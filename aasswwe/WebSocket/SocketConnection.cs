using System;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Common;

namespace WebSocket
{
    public class SocketConnection
    {
        public string Name { get; set; }
        public string RemoteEndPoint { get; set; }

        private Boolean isDataMasked;
        public Boolean IsDataMasked
        {
            get { return isDataMasked; }
            set { isDataMasked = value; }
        }

        public Socket ConnectionSocket;

        private readonly int maxBufferSize;
        private string handshake;
        private string newHandshake;

        public byte[] ReceivedDataBuffer;
        private readonly byte[] firstByte;
        private readonly byte[] lastByte;
        private byte[] serverKey1;
        private byte[] serverKey2;


        public event NewConnectionEventHandler NewConnection;
        public event DataReceivedEventHandler DataReceived;
        public event DisconnectedEventHandler Disconnected;

        public SocketConnection()
        {
            maxBufferSize = 1024 * 100;
            ReceivedDataBuffer = new byte[maxBufferSize];
            firstByte = new byte[maxBufferSize];
            lastByte = new byte[maxBufferSize];
            firstByte[0] = 0x00;
            lastByte[0] = 0xFF;

            handshake = "HTTP/1.1 101 Web Socket Protocol Handshake" + Environment.NewLine;
            handshake += "Upgrade: WebSocket" + Environment.NewLine;
            handshake += "Connection: Upgrade" + Environment.NewLine;
            handshake += "Sec-WebSocket-Origin: " + "{0}" + Environment.NewLine;
            handshake += string.Format("Sec-WebSocket-Location: " + "ws://{0}:8888/chat" + Environment.NewLine, WebSocketServer.GetLocalmachineIpAddress());
            handshake += Environment.NewLine;

            newHandshake = "HTTP/1.1 101 Switching Protocols" + Environment.NewLine;
            newHandshake += "Upgrade: WebSocket" + Environment.NewLine;
            newHandshake += "Connection: Upgrade" + Environment.NewLine;
            newHandshake += "Sec-WebSocket-Accept: {0}" + Environment.NewLine;
            newHandshake += Environment.NewLine;
        }

        private void Read(IAsyncResult status)
        {
            
            if (!ConnectionSocket.Connected) return;
            var dr = new DataFrame(ReceivedDataBuffer);
            if (dr.OpCode == 10 || dr.OpCode == 9)//心跳检查
            {
                ConnectionSocket.BeginReceive(ReceivedDataBuffer, 0, ReceivedDataBuffer.Length, 0, Read, null);
                return;
            }
            try
            {
                string messageReceived;
                if (!isDataMasked)
                {
                    // Web Socket protocol: messages are sent with 0x00 and 0xFF as padding bytes
                    var decoder = new UTF8Encoding();
                    var startIndex = 0;

                    // Search for the start byte
                    while (ReceivedDataBuffer[startIndex] == firstByte[0]) startIndex++;
                    // Search for the end byte
                    var endIndex = startIndex + 1;
                    while (ReceivedDataBuffer[endIndex] != lastByte[0] && endIndex != maxBufferSize - 1) endIndex++;
                    if (endIndex == maxBufferSize - 1) endIndex = maxBufferSize;

                    // Get the message
                    messageReceived = decoder.GetString(ReceivedDataBuffer, startIndex, endIndex - startIndex);
                }
                else
                {
                    messageReceived = dr.Text;
                }

                if ((messageReceived.Length == maxBufferSize && messageReceived[0] == Convert.ToChar(65533)) ||
                    messageReceived.Length == 0)
                {
                    //Logger.Log("接受到的信息 [\"" + string.Format("logout:{0}",this.name) + "\"]");
                    if (Disconnected != null)
                        Disconnected(this, EventArgs.Empty);

                    return;
                }
                else
                {
                    if (DataReceived != null)
                    {
                        //Logger.Log("接受到的信息 [\"" + messageReceived + "\"]");
                        DataReceived(this, messageReceived, EventArgs.Empty);
                    }
                    Array.Clear(ReceivedDataBuffer, 0, ReceivedDataBuffer.Length);
                    if (!ConnectionSocket.Connected) return;
                }
                ConnectionSocket.BeginReceive(ReceivedDataBuffer, 0, ReceivedDataBuffer.Length, 0, Read, null);

            }
            catch (Exception ex)
            {
                var log = "----------------------------------------------------------------------------------" + "\r\n";
                log += "错误信息：" + ex.Message+"\r\n";
                log += "结果：该Socket连接将会被终止。" + "\r\n";
                log += "----------------------------------------------------------------------------------" + "\r\n";

                Logger.Log(Enums.LogType.Error, log);
                if (Disconnected != null)
                    Disconnected(this, EventArgs.Empty);
            }
        }

        private void BuildServerPartialKey(int keyNum, string clientKey)
        {
            string partialServerKey = "";
            int spacesNum = 0;
            char[] keyChars = clientKey.ToCharArray();
            foreach (char currentChar in keyChars)
            {
                if (char.IsDigit(currentChar)) partialServerKey += currentChar;
                if (char.IsWhiteSpace(currentChar)) spacesNum++;
            }
            try
            {
                var currentKey = BitConverter.GetBytes((int)(Int64.Parse(partialServerKey) / spacesNum));
                if (BitConverter.IsLittleEndian) Array.Reverse(currentKey);

                if (keyNum == 1) serverKey1 = currentKey;
                else serverKey2 = currentKey;
            }
            catch
            {
                if (serverKey1 != null) Array.Clear(serverKey1, 0, serverKey1.Length);
                if (serverKey2 != null) Array.Clear(serverKey2, 0, serverKey2.Length);
            }
        }

        private byte[] BuildServerFullKey(byte[] last8Bytes)
        {
            var concatenatedKeys = new byte[16];
            Array.Copy(serverKey1, 0, concatenatedKeys, 0, 4);
            Array.Copy(serverKey2, 0, concatenatedKeys, 4, 4);
            Array.Copy(last8Bytes, 0, concatenatedKeys, 8, 8);

            // MD5 Hash
            var md5Service = MD5.Create();
            return md5Service.ComputeHash(concatenatedKeys);
        }

        public void ManageHandshake(IAsyncResult status)
        {
            const string header = "Sec-WebSocket-Version:";
            var handshakeLength = (int)status.AsyncState;
            var last8Bytes = new byte[8];

            var decoder = new UTF8Encoding();
            var rawClientHandshake = decoder.GetString(ReceivedDataBuffer, 0, handshakeLength);

            Array.Copy(ReceivedDataBuffer, handshakeLength - 8, last8Bytes, 0, 8);

            //现在使用的是比较新的Websocket协议
            if (rawClientHandshake.IndexOf(header, StringComparison.Ordinal) != -1)
            {
                isDataMasked = true;
                var rawClientHandshakeLines = rawClientHandshake.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                var acceptKey = "";
                foreach (string line in rawClientHandshakeLines)
                {
                    Console.WriteLine(line);
                    if (line.Contains("Sec-WebSocket-Key:"))
                    {
                        acceptKey = ComputeWebSocketHandshakeSecurityHash09(line.Substring(line.IndexOf(":", StringComparison.Ordinal) + 2));
                        break;
                    }
                }

                newHandshake = string.Format(newHandshake, acceptKey);
                byte[] newHandshakeText = Encoding.UTF8.GetBytes(newHandshake);
                ConnectionSocket.BeginSend(newHandshakeText, 0, newHandshakeText.Length, 0, HandshakeFinished, null);
                Logger.Log(Enums.LogType.Msg, "新的连接请求来自" + ConnectionSocket.RemoteEndPoint + ",正在准备连接 ...");
                return;
            }

            var clientHandshake = decoder.GetString(ReceivedDataBuffer, 0, handshakeLength - 8);
            var clientHandshakeLines = clientHandshake.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);


            // Welcome the new client
            foreach (string line in clientHandshakeLines)
            {
                if (line.Contains("Sec-WebSocket-Key1:"))
                    BuildServerPartialKey(1, line.Substring(line.IndexOf(":", StringComparison.Ordinal) + 2));
                if (line.Contains("Sec-WebSocket-Key2:"))
                    BuildServerPartialKey(2, line.Substring(line.IndexOf(":", StringComparison.Ordinal) + 2));
                if (line.Contains("Origin:"))
                    try
                    {
                        handshake = string.Format(handshake, line.Substring(line.IndexOf(":", StringComparison.Ordinal) + 2));
                    }
                    catch
                    {
                        handshake = string.Format(handshake, "null");
                    }
            }
            // Build the response for the client
            var handshakeText = Encoding.UTF8.GetBytes(handshake);
            var serverHandshakeResponse = new byte[handshakeText.Length + 16];
            var serverKey = BuildServerFullKey(last8Bytes);
            Array.Copy(handshakeText, serverHandshakeResponse, handshakeText.Length);
            Array.Copy(serverKey, 0, serverHandshakeResponse, handshakeText.Length, 16);

            ConnectionSocket.BeginSend(serverHandshakeResponse, 0, handshakeText.Length + 16, 0, HandshakeFinished, null);
            Logger.Log(Enums.LogType.Msg, "新的连接请求来自" + ConnectionSocket.RemoteEndPoint + ",正在准备连接 ...");
            
        }

        public static String ComputeWebSocketHandshakeSecurityHash09(String secWebSocketKey)
        {
            const String magicKey = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            // 1. Combine the request Sec-WebSocket-Key with magic key.
            var ret = secWebSocketKey + magicKey;
            // 2. Compute the SHA1 hash
            SHA1 sha = new SHA1CryptoServiceProvider();
            var sha1Hash = sha.ComputeHash(Encoding.UTF8.GetBytes(ret));
            // 3. Base64 encode the hash
            var secWebSocketAccept = Convert.ToBase64String(sha1Hash);
            return secWebSocketAccept;
        }

        private void HandshakeFinished(IAsyncResult status)
        {
            ConnectionSocket.EndSend(status);
            ConnectionSocket.BeginReceive(ReceivedDataBuffer, 0, ReceivedDataBuffer.Length, 0, Read, null);
            if (NewConnection != null) NewConnection("", EventArgs.Empty);
        }
    }
}
