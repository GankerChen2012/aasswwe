using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebSocket
{
    public partial class WebSocketServer
    {
        readonly List<string> userList = new List<string>();
        readonly List<string> titleList = new List<string>();
        string titleUserName;
        string people;
        string ghost;
        string speak;
        int peopleNum;
        int ghostNum;
        readonly Dictionary<string, string> voteList = new Dictionary<string, string>();
        readonly Dictionary<string, string> userTitle = new Dictionary<string, string>();
        string win;
        bool waiver;
        private string type = "_over";
        string judge;

        //有人断线
        void socketConn_Disconnected(Object sender, EventArgs e)
        {
            var sConn = sender as SocketConnection;
            if (sConn == null) return;
            if (!connectionSocketList.Contains(sConn)) return;

            string msg;
            if (type == "_game")
            {
                string isOver;
                if (sConn.Name == judge)
                {
                    isOver = "_over";
                    msg = string.Format("LoginOut|法官【{0}】离开游戏了，本局提前结束！|{0}|{1}", sConn.Name, isOver);
                }
                else
                {
                    speak = SpeakUser();
                    isOver = IsOver(sConn.Name, userTitle[sConn.Name]);
                    msg = string.Format("LoginOut|【{0}】回家吃饭去了！|{0}|{1}|{2}|{3}", sConn.Name, isOver, speak, userList[0]);
                }
                Send(msg);

                if (isOver == "_over")
                    GameOver();
            }
            else
            {
                var isUser = sConn.Name == titleUserName;
                if (isUser)
                {
                    titleList.Clear();
                    userList.Clear();
                }
                msg = string.Format("LoginOut|【{0}】回家吃饭去了|{0}|{1}", sConn.Name, isUser);
                Send(msg);
            }

            Logger.Log(Enums.LogType.Logout, sConn.Name);
            Logger.Log(Enums.LogType.Msg, "【" + sConn.Name + "】掉线了！");

            sConn.ConnectionSocket.Close();
            userList.Remove(sConn.Name);
            connectionSocketList.Remove(sConn);
            //Close();
        }
        
        //有请求消息
        void socketConn_DataReceived(Object sender, string message, EventArgs e)
        {
            var jo = (JObject)JsonConvert.DeserializeObject(message);
            var requestType = jo["Action"].ToString();
            var sConn = sender as SocketConnection;
            if (sConn == null) return;
           
            Logger.Log(Enums.LogType.Msg, "【"+sConn.Name+"】：" + message);

                if (type == "_game")
                {
                    string msg;
                    if (!userList.Contains(sConn.Name))
                    {
                        msg = string.Format("现在正在游戏，等游戏结束后，你才能发言！");
                        SendUser(msg, sConn);
                        return;
                    }

                    if (speak != null && speak != sConn.Name)
                    {
                        msg = string.Format("检查到玩家【{0}】企图破坏游戏公平性！", sConn.Name);
                        Send(msg);
                        return;
                    }
                }

                switch (requestType)
                {
                    case "Login": Login(jo, sConn); break;
                    case "SendMsg": SendMsg(jo); break;
                    case "SendTitle": SendTitle(jo, sConn); break;
                    case "StartGame": StartGame(jo, sConn); break;
                    case "KillMe": KillMe(jo); break;
                    case "GuessTitle": GuessTitle(jo); break;
                    case "VoteUser": VoteUser(jo, sConn); break;
                        //case "GameOver": GameOver(); break;
                    case "ClearTitle": ClearTitle(sConn); break;
                    case "Plan": Plan(jo, sConn); break;
                }
        }

        public void Plan(JObject jo, SocketConnection sConn)
        {
            var user = jo["UserName"].ToString();
            string msg;
            if (!userList.Contains(user))
            {
                userList.Add(user);
                msg = string.Format("RevicePlan|True|{0}", user);
                Send(msg);
            }
            else
            {
                msg = string.Format("RevicePlan|False|你要爪子，准备了还点。。");
                var dr = new DataFrame(msg);
                sConn.ConnectionSocket.Send(dr.GetBytes());
            }
        }

        public void ClearTitle(SocketConnection sConn)
        {
            type = "_over";
            titleList.Clear();
            userList.Clear();
            Send("ReviceClearTitle|" + sConn.Name);
        }

        public void GameOver()
        {
            var usertitle = "";
            usertitle = userTitle.Aggregate(usertitle, (current, title) => current + (":" + title.Key + ":" + title.Value));

            var msg = string.Format("ReviceGameOver|【{0}】胜利|{1}", win, usertitle);
            Send(msg);

            userList.Clear();
            titleList.Clear();
            userTitle.Clear();
            voteList.Clear();

            titleUserName = null;
            people = null;
            ghost = null;
            speak = null;
            peopleNum = 0;
            ghostNum = 0;
            win = null;
            judge = null;

            type = "_over";
        }

        public void Login(JObject jo, SocketConnection sConn)
        {
            string msg;
            var user = jo["UserName"].ToString();
            
            var name = connectionSocketList.Where(b => b.Name == user);
            if (name.Count() != 0)
            {
                msg = "Login|False|用户名已被使用！";
                SendUser(msg, sConn);
                Logger.Log(Enums.LogType.Error, "用户名【" + user + "】已经被使用");

                sConn.ConnectionSocket.Close();
                connectionSocketList.Remove(sConn);
            }
            else
            {
                sConn.Name = user;
                msg = "Login|True|" + user + "|";
                msg += connectionSocketList.Aggregate("", (current, list) => current + ("," + list.Name));
                msg += "|";
                msg += userList.Aggregate("", (current, list) => current + ("," + list));
                msg += "|" + type;
                msg += "|" + titleUserName;
                Send(msg);

                Logger.Log(Enums.LogType.Login, user);
            }
        }

        public void SendMsg(JObject jo)
        {
            var txt = jo["Msg"].ToString();
            var user = jo["UserName"].ToString();
            string msg;
            if (type == "_game")
            {
                speak = SpeakUser();
                msg = string.Format("ReviceMsg|{0}说:{1}|{2}|{3}|{4}", user, txt, speak, waiver, userList[0]);
            }
            else
            {
                msg = string.Format("ReviceMsg|{0}说:{1}", user, txt);
            }
            Send(msg);

            if (speak == "")
            {
                waiver = false;
            }
        }

        public void SendTitle(JObject jo, SocketConnection sConn)
        {
            var userName = jo["UserName"].ToString();
            string msg;
            if (titleList.Count == 0)
            {
                var requestPeople = jo["People"].ToString();
                var requestGhost = jo["Ghost"].ToString();
                var requestPeopleNum = Convert.ToInt32(jo["PeopleNum"].ToString());
                var requestGhostNum = Convert.ToInt32(jo["GhostNum"].ToString());
                var requestXiaoBai = Convert.ToInt32(jo["XiaoBai"].ToString());

                titleUserName = userName;
                people = requestPeople;
                ghost = requestGhost;
                peopleNum = requestPeopleNum;
                ghostNum = requestGhostNum;

                for (var i = 0; i < requestPeopleNum; i++)
                {
                    titleList.Add(requestPeople);
                }
                for (var i = 0; i < requestGhostNum; i++)
                {
                    titleList.Add(requestGhost);
                }
                for (var i = 0; i < requestXiaoBai; i++)
                {
                    titleList.Add("你是小白，伤不起");
                }
                type = "_plan";
                msg = string.Format("ReviceTitle|{0}已出题，请要参加的人员，赶快准备！|{0}", userName);
                Send(msg);
            }
            else
            {
                msg = string.Format("ReviceTitle|出题失败！{0}已出题，请先完成当前题目！！", titleUserName);
                var dr = new DataFrame(msg);
                sConn.ConnectionSocket.Send(dr.GetBytes());
            }
        }

        public void StartGame(JObject jo, SocketConnection sConn)
        {
            userList.Remove(sConn.Name);
            judge = sConn.Name;

            string msg;
            if (titleList.Count != userList.Count)
            {
                msg = string.Format("ReviceStartGame|False|抱歉，出题要求{0}人参加，但现在只有{1}人，所以游戏启动失败！", titleList.Count, userList.Count);
                Send(msg);
            }
            else
            {
                var num = Rank();
                speak = userList[0];

                var i = 0;
                foreach (var item in connectionSocketList)
                {
                    if (item.Name != sConn.Name)
                    {
                        var title = titleList[num[i] - 1];
                        msg = string.Format("ReviceStartGame|True|游戏开始，{1}开始发言！|{0}|{1}", title, speak);

                        if (!userTitle.ContainsKey(item.Name))
                            userTitle.Add(item.Name, title);
                        i++;
                    }
                    else
                    {
                        type = "_game";
                        msg = string.Format("ReviceStartGame|True|游戏开始，{0}开始发言！|你是法官|{0}", speak);
                    }
                    var dr = new DataFrame(msg);
                    item.ConnectionSocket.Send(dr.GetBytes());
                }
            }
        }

        public void KillMe(JObject jo)
        {
            var userName = jo["UserName"].ToString();
            var title = jo["Title"].ToString();

            speak = SpeakUser();
            string isOver = IsOver(userName, title);
            waiver = true;
            var msg = string.Format("ReviceKillMe|{0}自杀|{0}|{1}|{2}|{3}", userName, isOver, speak, userList[0]);
            Send(msg);


            if (isOver == "_over")
            {
                GameOver();
            }
        }

        private string IsOver(string user, string title)
        {
            userList.Remove(user);

            if (people == title)
                peopleNum--;
            else if (ghost == title)
                ghostNum--;

            if (ghostNum == 0)
            {
                win = "平民" + people;
                return "_over";
            }

            if (peopleNum < ghostNum || peopleNum == 0)
            {
                win = "卧底" + ghost;
                return "_over";
            }
            return "continue";
        }

        public void GuessTitle(JObject jo)
        {
            var title = jo["Title"].ToString();
            var userName = jo["UserName"].ToString();
            var guessTitle = jo["GuessTitle"].ToString();
            speak = SpeakUser();
            waiver = true;
            string isOver;
            if (title == ghost && guessTitle == people)
            {
                isOver = "_over";
            }
            else
            {
                isOver = IsOver(userName, title);
            }

            var msg = string.Format("ReviceGuessTitle|{0}猜题为{1}|{0}|{2}|{3}|{4}", userName, guessTitle, isOver, speak, userList[0]);
            Send(msg);

            if (isOver == "_over")
            {
                GameOver();
            }
        }

        public void VoteUser(JObject jo, SocketConnection sConn)
        {
            var userName = jo["UserName"].ToString();
            var voteUser = jo["VoteUser"].ToString();

            if (!voteList.ContainsKey(userName))
            {
                voteList.Add(userName, voteUser);
            }
            else
            {
                const string message = "ReviceVote|Flase|你已经投过票了，请不要重复投票！";
                var dr = new DataFrame(message);
                sConn.ConnectionSocket.Send(dr.GetBytes());
                return;
            }

            if (userList.Count != voteList.Count) return;


            var result = voteList.GroupBy(d => d.Value).Select(
                n => new
                {
                    Count = n.Count(),
                    value = n.FirstOrDefault().Value
                }).OrderByDescending(n => n.Count).ToList();

            var voteTxt = voteList.Aggregate("", (current, vote) => current + (":" + vote.Key + ":" + vote.Value));

            string resultVote = null;
            for (var i = 0; i < result.Count - 1; i++)
            {
                if (result[i].Count == result[i + 1].Count)
                {
                    resultVote += "," + result[i + 1].value;
                }
                else
                {
                    break;
                }
            }

            string style;
            string isOver;
            if (resultVote == null)
            {
                userList.Remove(result[0].value);
                resultVote = string.Format("{0}获得了{1}票，所以{0}出局！", result[0].value, result[0].Count);
                isOver = IsOver(result[0].value, userTitle[result[0].value]);
                style = "Kill";
            }
            else
            {
                resultVote = string.Format("{0}等人票数相同，都是{1}票，所以进行PK!", result[0].value + resultVote, result[0].Count);
                style = "Pk";
                isOver = "continue";
            }

            voteList.Clear();

            var msg = string.Format("ReviceVote|True|{0}|{1}|{2}|{3}|{4}|{5}", voteTxt, style, resultVote,
                                    result[0].value, userList[0], isOver);
            Send(msg);

            speak = userList[0];

            if (isOver == "_over")
            {
                GameOver();
            }

        }

        public void Send(string message)
        {
            foreach (var item in connectionSocketList)
            {
                SendUser(message, item);
            }
        }

        //发言
        private string SpeakUser()
        {
            for (var i = 0; i < userList.Count; i++)
            {
                if (speak == null)
                {
                    if (userList.Count != 0)
                        return userList[0];
                }

                if (userList[i] == speak && i != userList.Count - 1)
                {
                    return userList[i + 1];
                }
            }
            return null;
        }
        //随机 平民和卧底
        private int[] Rank()
        {
            var count = userList.Count;
            var num = new int[count];
            for (var i = 0; i < count; i++)
            {
                var ra = new Random();
                var shu = ra.Next(0, count + 1);
                var b = false;
                for (var j = 0; j < count; j++)
                {
                    if (num[j] == shu)
                    {
                        b = true;
                    }
                }
                if (b)
                {
                    i--;
                    continue;
                }
                num[i] = shu;
            }
            return num;
        }

    }
}
