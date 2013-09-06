
namespace Common
{
    public class Enums
    {
        public enum ServerStatusLevel { Off, WaitingConnection, ConnectionEstablished };

        public enum LogType
        {
            Start=0,
            Login=1,
            Logout=2,
            Msg=3,
            Error=4

        }
    }
}
