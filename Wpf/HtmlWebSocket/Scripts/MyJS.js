var ws;
var SocketCreated = false;
var isUserloggedout = false;
//
$(function () {
   
    var webSocketsExist = true;
    try {
        var dummy = new WebSocket("ws://localhost:8989/test");
    } catch (ex) {
        try {
            var webSocket = new window.MozWebSocket("ws://localhost:8989/test");
        }
        catch (ex) {
            webSocketsExist = false;
        }
    }

    if (webSocketsExist) {
        Show("您的浏览器支持WebSocket. 您可以尝试连接到服务器!", "OK");
        $("#Connection").val("192.168.100.139");
    } else {
        Show("您的浏览器不支持WebSocket。请选择其他的浏览器再尝试连接服务器。", "ERROR");
        Show("不支持IE10以下内核浏览器", "ERROR");
        Show("需要支持HTML5的浏览器", "ERROR");
        Show("谷歌，火狐，苹果，欧鹏浏览器都可以", "ERROR");
        Disabled("ToggleConnection", true);
    }

    $("input[type=button]").button();
    $("#socketTop").draggable();
    $("#socketLeft").draggable();
    $("#socketMain").draggable();
    $("#chances").draggable();
    $("#host").draggable();
    $("#SendDataContainer").draggable();
    $("#socketMain").resizable();
    HideDiv();

});

function ToggleConnectionClicked() {
    if (SocketCreated && (ws.readyState == 0 || ws.readyState == 1)) {
        Show("已离开！");
        SocketCreated = false;
        isUserloggedout = true;
        ws.close();
        Disabled("Button2", false);
    } else {
        Show("正在连接...");
        try {
            if ("WebSocket" in window) {
                ws = new WebSocket("ws://" + $("#Connection").val() + ":8888/chat");
            }
            else if ("MozWebSocket" in window) {
                ws = new window.MozWebSocket("ws://" + $("#Connection").val() + ":8888/chat");
            }

            SocketCreated = true;
            isUserloggedout = false;
        } catch (ex) {
            Show(ex, "ERROR");
            return;
        }
        $("#ToggleConnection").val("断开");

        ws.onopen = WSonOpen;
        ws.onmessage = WSonMessage;
        ws.onclose = WSonClose;
        ws.onerror = WSonError;
    }
};

function WSonOpen() {
    Show("连接已经建立。", "OK");
    ShowDiv();
    $("txtName").attr("readonly", "readonly");
    var data = {};
    data.Action = "Login";
    data.UserName = $("#txtName").val();
    var jsonObject = $.toJSON(data);
    ws.send(jsonObject);
};

function WSonMessage(event) {
    var val = event.data.split('|');
    if (val[0] == "Login") {
        ReviceLogin(val);
    }
    else if (val[0] == "LoginOut") {
        ReviceLoginOut(val);
    }
    else if (val[0] == "ReviceMsg") {
        ReviceMsg(val);
    }
    else if (val[0] == "ReviceTitle") {
        ReviceTitle(val);
    }
    else if (val[0] == "ReviceStartGame") {
        ReviceStartGame(val);
    }
    else if (val[0] == "ReviceKillMe") {
        ReviceKillMe(val);
    }
    else if (val[0] == "ReviceGuessTitle") {
        ReviceGuessTitle(val);
    }
    else if (val[0] == "ReviceGameOver") {
        ReviceGameOver(val);
    }
    else if (val[0] == "ReviceVote") {
        ReviceVote(val);
    }
    else if (val[0] == "ReviceClearTitle") {
        ReviceClearTitle(val);
    }
    else if (val[0] == "RevicePlan") {
        RevicePlan(val);
    }
};

function WSonClose() {
    //$("#ToggleConnection").val("连接");
    //Show("远程连接中断。", "ERROR");
};

function WSonError() {
    $("#ToggleConnection").val("连接");
    HideDiv();
    Show("远程连接中断。", "ERROR");
};


//发送消息

function SendMsg() {
  
    if ($("#DataToSend").val().trim() == "") {
        alert("不能发送空消息！");
        return;
    }
    
    var data = {};
    data.Action = "SendMsg";
    data.UserName = $("#txtName").val();
    data.Msg = $("#DataToSend").val();
    var jsonObject = $.toJSON(data);
    ws.send(jsonObject);

    $("#DataToSend").val("");

    NoSpeak();
};

function SendTitle() {

    if ($("#gameType").val() != "_over") {
        alert("已经有题目了，请先完成现有题目！");
        return;
    }
    
    var txtName = $("#txtName").val();
    var people = $("#people").val();
    var ghost = $("#ghost").val();
    var peopleNum = $("#peopleNum").val();
    var ghostNum = $("#ghostNum").val();
    var xiaoBai = $("#XiaoBai").val();

    if (people == "" || ghost == "" || peopleNum == "" || ghostNum == "" || xiaoBai == "") {
        alert("输入的题目或人数不能为空！");
        return;
    }

    if (isNaN(peopleNum) || isNaN(ghostNum) || isNaN(xiaoBai)) {
        alert("人数请输入正整数！");
        return;
    }

    var data = {};
    data.Action = "SendTitle";
    data.UserName = txtName;
    data.People = people;
    data.Ghost = ghost;
    data.PeopleNum = peopleNum;
    data.GhostNum = ghostNum;
    data.XiaoBai = xiaoBai;
    var jsonObject = $.toJSON(data);
    ws.send(jsonObject);
}

function SendStartGame() {

    if ($("#gameType").val() != "_plan") {
        alert("已经在开始游戏或还没有出题！");
        return;
    }
    
    var data = {};
    data.Action = "StartGame";
    data.UserName = $("#txtName").val();
    var jsonObject = $.toJSON(data);
    ws.send(jsonObject);
}

function SendKillMe() {

    if ($("#gameType").val() != "_game") {
        alert("现在还没有开始游戏！");
        return;
    }

    var data = {};
    data.Action = "KillMe";
    data.Title = $("#title").val();
    data.UserName = $("#txtName").val();
    var jsonObject = $.toJSON(data);
    ws.send(jsonObject);

    NoSpeak();
}

function SendGuessTitle() {

    if ($("#gameType").val() != "_game") {
        alert("现在还没有开始游戏！");
        return;
    }

    if ($("#guessTxt").val() == "" || $("#title").val() == "" || $("#txtName").val() == "") {
        alert("猜的题目不能为空！");
        return;
    }
    var data = {};
    data.Action = "GuessTitle";
    data.UserName = $("#txtName").val();
    data.Title = $("#title").val();
    data.GuessTitle = $("#guessTxt").val();
    var jsonObject = $.toJSON(data);
    ws.send(jsonObject);

    NoSpeak();
}

function SendVoteUser() {
    
    if ($("#gameType").val() != "_game") {
        alert("现在还没有开始游戏！");
        return;
    }
   
    var user = ChangeList();
    if (user == null) {
        alert("请选择玩家，再点击投票");
        return;
    }

    var data = {};
    data.Action = "VoteUser";
    data.VoteUser = user;
    data.UserName = $("#txtName").val();
    var jsonObject = $.toJSON(data);
    ws.send(jsonObject);

    NoSpeak();
}

function SendClearTitle() {

    if ($("#gameType").val() == "_game") {
        alert("正在游戏，清除无效！");
        return;
    }


    var data = {};
    data.Action = "ClearTitle";
    var jsonObject = $.toJSON(data);
    ws.send(jsonObject);

    Disabled("sendTitle", false);
    Disabled("startGame", false);
    
    $("#people").val("");
    $("#peopleNum").val("");
    $("#ghost").val("");
    $("#ghostNum").val("");
    $("#XiaoBai").val("");
}

function SendPlan() {

    if ($("#gameType").val() == "_game") {
        alert("正在游戏，准备无效！");
        return;
    }

    var data = {};
    data.Action = "Plan";
    data.UserName = $("#txtName").val();
    var jsonObject = $.toJSON(data);
    ws.send(jsonObject);
    $("#Button2").val("已准备");
    Disabled("Button2", true);
}



//接受消息
function ReviceLogin(val) {
    if (val[1] == "True") {

        if (val[2] == $("#txtName").val()) {
            $("#UserList").html("");
            $("#Select1").html("<option >请选择</option>");
            Disabled("startGame", true);

            if (val[5] == "_game") {
                Disabled("Button2", true);
                Disabled("sendTitle", true);
                Disabled("ClearTitle", true);
                
                $("#gameType").val("_game");
                NoSpeak();
                Show("请等待这盘游戏结束，再加入！", "ERROR");
            } else {
                $("#SpeakUserName").html("自由发言时间...");
                $("#gameType").val(val[5]);
                if (val[6] != "") {
                    Disabled("sendTitle", true);
                    Show(val[6] + "已出题，请要参加的人员，赶快准备！", "Plan");
                } 
            }
            var userList = val[3].split(',');
            var txt;
            var i;
            for (i = 1; i < userList.length; i++) {
                txt = userList[i];
                $("#UserList").append("<p id='p-" + txt + "'>" + txt + "</p>");
            }

            var planList = val[4].split(',');
            for (i = 1; i < planList.length; i++) {
                txt = planList[i];
                $("#p-" + txt).html($("#p-" + txt).html() + "(已准备)");
                $("#Select1").append("<option id='op-" + txt + "'>" + txt + "</option>");
            }
        } else {
            $("#UserList").append("<p id='p-" + val[2] + "'>" + val[2] + "</p>");
            $("#Select1").append("<option id='op-" + val[2] + "'>" + val[2] + "</option>");
        }
        var msg = val[2] + "登录成功！";
        Show(msg, "OK");
    }
    else {
        Show(val[2], "ERROR");
        $("#ToggleConnection").val("连接");
    }

}

function ReviceLoginOut(val) {
    Show(val[1], "ERROR");
    var type = $("#gameType").val();
    if (type == "_game") {
        var msg = "由于有人退出，算自杀，所以本轮不进行投票！";
        Result(val, msg);
    }
    else {
        if (val[2]) {
            Disabled("sendTitle", false);
            Disabled("startGame", false);
            Disabled("ClearTitle", false);
            
            Show("玩家" + val[1] + "已离开，现在请重新设置题目！");
        }
        $("#op-" + val[2]).remove();
        $("#p-" + val[2]).remove();
    }

}

function ReviceMsg(val) {
    Show(val[1]);
    var gametype = $("#gameType").val();
    if (gametype == "_game") {
        if (val[2] == "") {
            if (val[3] == "True") {
                Show("发言结束！");
                Show("由于有人弃权(自杀/猜题失败/掉线)，所以本轮不再投票！");
                Show("新一轮开始！");
                SpeakUserName(val[4]);
                if (val[4] == $("#txtName").val()) {
                    Speak();
                }
            }
            else {
                Show("发言结束，开始投票！");
                Disabled("SendData", true);
                Disabled("sendNote", false);
            }
        }
        else {
            SpeakUserName(val[2]);
            if (val[2] == $("#txtName").val()) {
                Speak();
            }
        }
    }
}

function ReviceTitle(val) {
    Show(val[1]);
    Disabled("sendTitle", true);
    if (val[2] == $("#txtName").val()) {
        Disabled("startGame", false);
    }
    else {
        Disabled("startGame", true);
    }
    $("#gameType").val("_plan");
}

function ReviceStartGame(val) {
    if (val[1] == "True") {
        Disabled("sendNote", true);
        Disabled("sendTitle", true);
        Disabled("startGame", true);
        Disabled("ClearTitle", true);

        $("#chances").show("blind", {}, 800, "");
        $("#gameType").val("_game");
        Show(val[2], "OK");
        $("#title").val(val[3]);
        if (val[4] == $("#txtName").val()) {
            Speak();
        }
        else {
            NoSpeak();
        }
        SpeakUserName(val[4]);

        var userList = $("#UserList p");
        for (var i = 0; i < userList.length; i++) {
            var userName = userList[i].innerHTML;
            var userId = userList[i].id;

            var length = userName.indexOf("已准备");
            userName = userName.substring(0, length - 1, userName.length - length);
            if (length > 0) {
                $("#" + userId).html(userName + "(正在游戏)");
            }
        }
    }
    else {
        Show(val[2], "ERROR");
    }
}

function ReviceKillMe(val) {
    Show(val[1], "ERROR");
    var msg = "由于有人自杀，所以本轮不进行投票！";
    Result(val, msg);
}

function ReviceGameOver(val) {
    $("#chances").hide("blind", {}, 800, "");
    $("#Button2").val("准备");
    Show("游戏结束！", "OK");
    Show(val[1]);
    Show("题目清单：");
    var userTitle = val[2].split(':');
    for (var i = 1; i < userTitle.length; i++) {
        var msg = userTitle[i] + "=>" + userTitle[i + 1];
        $("#p-" + userTitle[i]).html(userTitle[i]);
        Show(msg);
        i++;
    }
    Speak();
    Disabled("startGame", false);
    Disabled("sendTitle", false);
    Disabled("Button2", false);
    
    $("#SpeakUserName").html("自由发言时间...");
    $("#gameType").val("_over");
}

function ReviceGuessTitle(val) {
    Show(val[1]);
    var msg = "由于有人猜题，但未能成功，只能算自杀，所以本轮不再投票！";
    Result(val, msg);
}

function ReviceVote(val) {
    if (val[1] == "Flase") {
        Show(val[2], "ERROR");
    }
    else {
        Show("投票结束！", "OK");
        var voteList = val[2].split(":");

        for (var i = 1; i < voteList.length; i++) {
            var msg = voteList[i] + "=>" + voteList[i + 1];
            Show(msg);
            i++;
        }

        Show(val[4]);

        if (val[3] == "Pk") {
            Show("友情提示：目前没有做PK人的筛选，所以投票的时候请投要进行PK的人，表乱投了！！！", "ERROR");
            Disabled("sendNote", false);
        }
        else {
            if (val[7] != "_over") {
                Disabled("sendNote", true);
                
                $("#op-" + val[5]).remove();
                $("#p-" + val[5]).html($("#p-" + val[5]).html() + "(被投死)");
                Show("新一轮开始！");
                SpeakUserName(val[6]);
                if (val[6] == $("#txtName").val()) {
                    Speak();
                }
            }
        }
    }
}

function ReviceClearTitle(val) {
    Disabled("sendTitle", false);
    Disabled("startGame", true);
    Disabled("ClearTitle", false);

    $("#gameType").val("_over");
    $("#Button2").val("准备");
    Show("玩家"+val[1]+"已清除题目，现在可以重新设置题目！");
}

function RevicePlan(val) {
    if (val[1] == "True") {
        var msg = val[2] + "已准备！";
        Show(msg, "Plan");
        $("#p-" + val[2]).html(val[2] + "(已准备)");
        $("#Select1").append("<option id='op-" + val[2] + "'>" + val[2] + "</option>");

        if (val[2] == $("#txtName").val()) {
            $("#Button2").val("已准备");
            Disabled("startGame", true);
        }
    }
    else {
        Show(val[2], "ERROR");
    }
}

function Result(val, msg) {
    if (val[3] != "_over") {
        $("#op-" + val[2]).remove();
        $("#p-" + val[2]).html($("#p-" + val[2]).html() + "(已洗白)");

        if (val[4] == "") {
            Show("发言结束！");
            Show(msg);
            Show("新一轮开始！");
            SpeakUserName(val[5]);
            if (val[5] == $("#txtName").val()) {
                Speak();
            }
            SpeakUserName(val[5]);
            return;
        }

        if (val[4] == $("#txtName").val()) {
            Speak();
        }
        SpeakUserName(val[4]);
    }
}

function Show(text, messageType) {
    if (messageType == "OK")
        text = "<span style='color: green;'>" + text + "</span>";
    else if (messageType == "ERROR")
        text = "<span style='color: red;'>" + text + "</span>";
    else if (messageType == "Plan")
        text = "<span style='color: blue;'>" + text + "</span>";

    $("#LogContainer").html($("#LogContainer").html() + text + "<br />");
    var logContainer = $("#LogContainer");
    logContainer.scrollTop = logContainer.scrollHeight;
};

function ChangeList() {
    var list = $("#Select1")[0];
    for (var i = 1; i < list.length; i++) {
        if (list[i].selected)
            return list[i].value;
    }
    return null;
}

function NoSpeak() {
    var gametype = $("#gameType").val();
    if (gametype == "_game") {
        Disabled("KillMe", true);
        Disabled("guessBtn", true);
        Disabled("sendNote", true);
        Disabled("SendData", true);
    }
}

function Speak() {
    Disabled("KillMe", false);
    Disabled("guessBtn", false);
    Disabled("SendData", false);
}

function ClearMsg() {
    $("#LogContainer").html("");
}

function SpeakUserName(user) {
    $("#SpeakUserName").html("<span style='color: Red;'>现在【" + user + "】发言</span>");
    user = "p-" + user;
    var userList = $("#UserList p");
    for (var i = 0; i < userList.length; i++) {
        var userName = userList[i].id;
        if (userName == user) {
            $("#" + userName).addClass("selected");
        } else {
            $("#" + userName).removeClass("selected");
        }
    }
}



function HideDiv() {
    $("#chances").hide();
    $("#SendDataContainer").hide();
    $("#host").hide();
}

function ShowDiv() {
    $("#chances").show("blind", {}, 800, "");
    $("#SendDataContainer").show("puff", {}, 800, "");
    $("#host").show("clip", {}, 800, "");
}

function Disabled(id, type) {
    if (type) {
        $("#" + id).css("background-color","white");
    } else {
        $("#" + id).css("background-color", "rgb(230, 230, 230)");
    }
    $("#" + id).attr("disabled", type);
}



