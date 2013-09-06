
namespace Common
{
    public static class Logger
    {
        public delegate void DelegateClassHandle(Enums.LogType type, string text);
        public static DelegateClassHandle MsgText;
        public static void Log(Enums.LogType type, string text)
        {
            if (MsgText != null)
                MsgText(type, text);
        }
    }
}
