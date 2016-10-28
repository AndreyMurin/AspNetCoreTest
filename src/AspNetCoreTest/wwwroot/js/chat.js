var webSocket;
$().ready(function () {
    //var port = window.location.port;
    //port = (port == 80) ? '' : ':' + port;
    var host = window.location.host;
    webSocket = new WebSocket('ws://' + host);
    webSocket.onopen = function () {
        $("#spanStatus").text("connected");
    };
    webSocket.onmessage = function (evt) {
        $("#spanText").append('<hr />' + evt.data);
    };
    webSocket.onerror = function (evt) {
        console.log(evt);
        //alert(evt.message);
        $("#spanText").append('<div class="alert alert-danger">' + evt.message + '</div>');
    };
    webSocket.onclose = function () {
        $("#spanStatus").text("disconnected");
    };
    $("#btnSend").click(function () {
        if (webSocket.readyState == WebSocket.OPEN) {
            webSocket.send($("#textInput").val());
        }
        else {
            $("#spanStatus").text("Connection is closed");
        }
    });
});