using System;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using Common;
using WebSocket;


namespace WpfApplication1
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Logger.MsgText += ShowMsg;
        }

        private WebSocketServer webSocket;
        private Thread th;

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (button1.Content.ToString() == "启动")
            {
                th = new Thread(Start);
                th.Start();
                button1.Content = "停止";
            }
            else
            {
                button1.Content = "启动";
                Close();
                th.Abort();
            }
        }

        private void Start()
        {
            webSocket = new WebSocketServer();
            webSocket.StartServer();
            
        }

        private void Close()
        {
            webSocket.Dispose();
        }

        FlowDocument Doc = new FlowDocument();
        public void ShowMsg(Enums.LogType type, string txt)
        {
            switch (type)
            {
                case Enums.LogType.Login:
                    Dispatcher.Invoke(new Action(() => listBox1.Items.Add(txt)));
                    break;
                case Enums.LogType.Logout:
                    Dispatcher.Invoke(new Action(() => listBox1.Items.Remove(txt)));
                    break;
                default:

                    Brush brush = Brushes.Black;
                    if (type == Enums.LogType.Start)
                    {
                        brush = Brushes.Red;
                    }
                    else if (type == Enums.LogType.Error)
                    {
                        brush = Brushes.Blue;
                    }
                    Dispatcher.Invoke(new Action(() =>
                        {
                            var p = new Paragraph(); // Paragraph 类似于 html 的 P 标签
                            var r = new Run(txt); // Run 是一个 Inline 的标签
                            p.Inlines.Add(r);
                            p.Foreground = brush;//设置字体颜色
                            // 除了设置属性，事件也可以在这里设置
                            
                            Doc.Blocks.Add(p);
                            richTextBox1.Document = Doc;
                        }));
                    break;
            }
        }
     
        private void button2_Click(object sender, RoutedEventArgs e)
        {
            richTextBox1.Dispatcher.Invoke(new Action(() => richTextBox1.Document.Blocks.Clear()));
        }
    }
}
