var webSocket;
var netConfig;

var sendRequest = function (obj) {
    console.log('sendRequest:', obj)
    if (webSocket.readyState === WebSocket.OPEN) {
        var j = JSON.stringify(obj);
        webSocket.send(j);
    } else {
        $("#spanStatus").text("Connection is closed");
    }
};

var showError = function (err, action) {
    $("#spanText").append('<div class="alert alert-danger">' + (action ? '<b>' + action + ':</b> ' : '') + err + '</div>');
};

var readConfig = function () {
    sendRequest({ Action: 'getnetconfig' });
};

$().ready(function () {
    //var port = window.location.port;
    //port = (port == 80) ? '' : ':' + port;
    var host = window.location.host;
    webSocket = new WebSocket('ws://' + host + '/brain-torus');
    webSocket.onopen = function () {
        $("#spanStatus").text("connected");
        readConfig();
    };
    webSocket.onmessage = function (evt) {
        //$("#spanText").append('<hr />' + evt.data);
        var answer = JSON.parse(evt.data);
        console.log('onmessage:', evt.data, answer)
        if (typeof answer.Error !== 'undefined' && answer.Error) {
            showError(answer.Error, answer.Action);
        } else {
            switch (answer.Action) {
                case 'getnetconfig':
                    netConfig = answer;
                    //console.log('getnetconfig:', netConfig);
                    break;

            }
        }
    };
    webSocket.onerror = function (evt) {
        console.log(evt);
        //alert(evt.message);
        //$("#spanText").append('<div class="alert alert-danger">' + evt.message + '</div>');
        showError(evt.message);
    };
    webSocket.onclose = function () {
        console.log(arguments);
        var arg = arguments;
        var reason = '';
        if (typeof arguments !== undefined && arguments.length > 0 && typeof arguments[0].reason !== 'undefined') {
            reason = ' reason: ' + arguments[0].reason;
        }
        $("#spanStatus").text("disconnected." + reason);
    };
    /*$("#btnSend").click(function () {
        if (webSocket.readyState === WebSocket.OPEN) {
            webSocket.send($("#textInput").val());
        }
        else {
            $("#spanStatus").text("Connection is closed");
        }
    });*/

    
});