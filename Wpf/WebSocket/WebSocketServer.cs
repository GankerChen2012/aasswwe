using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Common;

namespace WebSocket
{
    public delegate void NewConnectionEventHandler(string loginName, EventArgs e);
    public delegate void DataReceivedEventHandler(Object sender, string message, EventArgs e);
    public delegate void DisconnectedEventHandler(Object sender, EventArgs e);
    public delegate void BroadcastEventHandler(string message, EventArgs e);

    public partial class WebSocketServer : IDisposable
    {
        private bool alreadyDisposed;
        private Socket listener;
        private int connectionsQueueLength;
        private int maxBufferSize;
        private byte[] firstByte;
        private byte[] lastByte;
        
        public Enums.ServerStatusLevel Status { get; private set; }
        public int ServerPort { get; set; }
        public string ServerIp { get; set; }
        public string ServerLocation { get; set; }
        public string ConnectionOrigin { get; set; }

        readonly List<SocketConnection> connectionSocketList = new List<SocketConnection>();
      
        private void Initialize()
        {
            alreadyDisposed = false;

            Status = Enums.ServerStatusLevel.Off;
            connectionsQueueLength = 500;
            maxBufferSize = 1024 * 100;
            firstByte = new byte[maxBufferSize];
            lastByte = new byte[maxBufferSize];
            firstByte[0] = 0x00;
            lastByte[0] = 0xFF;
        }

        public static IPAddress GetLocalmachineIpAddress()
        {
            var strHostName = Dns.GetHostName();
            var ipEntry = Dns.GetHostEntry(strHostName);

            foreach (var ip in ipEntry.AddressList)
            {
                //IPV4
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip;
            }

            return ipEntry.AddressList[0];

            //IPAddress address = IPAddress.Parse("192.168.1.102");
            //return address;
        }

        public WebSocketServer()
        {
            ServerPort = 8888;
            //ServerIp = serverIp;
            //ConnectionOrigin = connectionOrigin;
            //ServerLocation = serverLocation;
            ServerLocation = string.Format("ws://{0}:{1}/chat", GetLocalmachineIpAddress(), ServerPort);
            Initialize();
        }

        ~WebSocketServer()
        {
            Close();
        }

        public void Dispose()
        {
            Close();
        }

        private void Close()
        {
            if (alreadyDisposed) return;

            alreadyDisposed = true;
            if (listener != null) listener.Close();

            foreach (var item in connectionSocketList)
            {
                item.ConnectionSocket.Close();
            }
            connectionSocketList.Clear();

            GC.SuppressFinalize(this);
        }

        public void StartServer()
        {
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            //var address = IPAddress.Parse(ServerIp);
            listener.Bind(new IPEndPoint(GetLocalmachineIpAddress(), ServerPort));
            listener.Listen(connectionsQueueLength);

            Logger.Log(Enums.LogType.Start, string.Format("聊天服务器启动。监听地址：{0}, 端口：{1}", GetLocalmachineIpAddress(), ServerPort));
            Logger.Log(Enums.LogType.Start, string.Format("WebSocket服务器地址: ws://{0}:{1}/chat", GetLocalmachineIpAddress(), ServerPort));

            while (true)
            {
                var sc = listener.Accept();
                System.Threading.Thread.Sleep(100);

                var socketConn = new SocketConnection { ConnectionSocket = sc };
                socketConn.DataReceived += socketConn_DataReceived;
                socketConn.Disconnected += socketConn_Disconnected;

                socketConn.ConnectionSocket.BeginReceive(socketConn.ReceivedDataBuffer,
                                                         0, socketConn.ReceivedDataBuffer.Length,
                                                         0, socketConn.ManageHandshake,
                                                         socketConn.ConnectionSocket.Available);
                connectionSocketList.Add(socketConn);
            }
// ReSharper disable FunctionNeverReturns
        }
// ReSharper restore FunctionNeverReturns

        public void SendUser(string message, SocketConnection item)
        {
            if (!item.ConnectionSocket.Connected) return;
            Logger.Log(Enums.LogType.Msg, message);
            try
            {
                if (item.IsDataMasked)
                {
                    var dr = new DataFrame(message);
                    item.ConnectionSocket.Send(dr.GetBytes());
                }
                else
                {
                    item.ConnectionSocket.Send(firstByte);
                    item.ConnectionSocket.Send(Encoding.UTF8.GetBytes(message));
                    item.ConnectionSocket.Send(lastByte);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(Enums.LogType.Error, ex.Message);
            }
        }
    }
}
