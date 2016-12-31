//var webSocket;
//var netConfig;

$().ready(function () {
    //var port = window.location.port;
    //port = (port == 80) ? '' : ':' + port;
    
    /*$("#btnSend").click(function () {
        if (webSocket.readyState === WebSocket.OPEN) {
            webSocket.send($("#textInput").val());
        }
        else {
            $("#spanStatus").text("Connection is closed");
        }
    });*/
    $('.js-bt-draw').btDraw();
    $('.js-bt-controls').btControls();
    
});