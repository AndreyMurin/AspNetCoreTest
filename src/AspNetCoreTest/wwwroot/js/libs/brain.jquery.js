(function ($) {
    // задаем пространство имен
    if (!$.bt) $.bt = {};

    var DATA_KEY = 'bt-controls';

    $.bt.controls = function (elem, options) {

        var defaultSettings = {
            url: "",
            id: "",
        },
        settings = $.extend({}, defaultSettings, options),
        base = this,
        element = $(elem),
        webSocket,
        netConfig,
        statusCont,
        textCont,
        sendRequest = function (obj) {
            console.log('sendRequest:', obj)
            if (webSocket.readyState === WebSocket.OPEN) {
                var j = JSON.stringify(obj);
                webSocket.send(j);
            } else {
                statusCont.text("Connection is closed");
            }
        },
        showError = function (err, action) {
            textCont.append('<div class="alert alert-danger">' + (action ? '<b>' + action + ':</b> ' : '') + err + '</div>');
        },
        readConfig = function () {
            sendRequest({ Action: 'getnetconfig' });
        },
        drawControls = function (value) {
            statusCont = $('<div class="bt-controls-status"></div>').appendTo(element);
            textCont = $('<div class="bt-controls-text"></div>').appendTo(element);
            //element.html();
        },
        create = function () {
            drawControls();

            var url = element.data('url');
            if (!url) {
                var host = window.location.host;
                url = 'ws://' + host + '/brain-torus';
            }
            webSocket = new WebSocket(url);
            webSocket.onopen = function () {
                statusCont.text("connected");
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
                statusCont.text("disconnected." + reason);
            };

            return base;
        };
        /*this.getValues = function () {
            
        };*/

        return create.call(this);
    };


    // обертка для jquery
    $.fn.btControls = function (options, attrs) {
        var instance;
        if (typeof (options) == 'string') {
            var retValue = null;
            this.each(function () {
                instance = $.data(this, DATA_KEY);
                if (instance && typeof (instance[options]) == 'function') {
                    retValue = instance[options].call(instance, attrs);
                }
                return retValue;
            });
            return retValue;
        }
        else {
            var elements = this.each(function () {
                instance = new $.bt.controls(this, options);;
                $.data(this, DATA_KEY, instance);
                return instance;
            });
            return elements;
        }
    };
})(jQuery);