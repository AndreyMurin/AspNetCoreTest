( function ( $ ) {
    "use strict"; // jshint ;_;

    // задаем пространство имен
    if ( !$.bt ) $.bt = {};

    var DATA_KEY = 'bt-controls';

    $.bt.controls = function ( elem, options ) {

        var defaultSettings = {
            draw: '.js-bt-draw',
            //url: ''
        },
        settings = $.extend( {}, defaultSettings, options ),
        base = this,
        element = $( elem ),
        webSocket,
        isConnected = false,
        subscribeRanges = undefined,
        netConfig = undefined,
        socketStatusCont,
        netStatusCont,
        errorCont,
        textCont,
        drawCont,
        threadsCont,
        subscribeCont = {},
        clearError = function () {
            errorCont.html('');
        },
        connect = function () {
            
            console.log('connect webSocket.readyState=', webSocket ? webSocket.readyState : '-');

            // мы в процессе конекта. тупо ждем
            if (webSocket && webSocket.readyState == WebSocket.CONNECTING || webSocket && webSocket.readyState == WebSocket.OPEN) return;

            clearError();

            var url = element.data('url');
            if (!url) {
                var host = window.location.host;
                url = 'ws://' + host + '/brain-torus';
            }
            webSocket = new WebSocket(url);
            //console.log('connect webSocket=', webSocket);
            webSocket.onopen = function () {
                //console.log('connect webSocket.onopen');
                isConnected = true;
                socketStatusCont.text("connected");
                readConfig();
            };
            webSocket.onmessage = function (evt) {
                //$("#spanText").append('<hr />' + evt.data);
                var answer = JSON.parse(evt.data);
                //console.log('onmessage:', evt.data, answer)
                if (typeof answer.IsStarted !== 'undefined') {
                    netStatusCont.text(answer.IsStarted ? 'work' : 'stop');
                }
                if (typeof answer.Error !== 'undefined' && answer.Error) {
                    showError(answer.Error, answer.Action);
                } else {
                    console.log('webSocket.onmessage', answer.Action)
                    switch (answer.Action) {
                        case 'getnetconfig':
                            netConfig = answer;
                            //console.log('getnetconfig:', netConfig);
                            drawCont.btDraw('setConfig', netConfig);
                            break;
                        case 'subscribe':
                            drawCont.btDraw('setNeurons', answer);
                            break;
                        case 'activities':
                            drawCont.btDraw('setActivities', answer);
                            threadsCont.html(answer.Threads);
                            break;
                    }
                }
            };
            webSocket.onerror = function (evt) {
                console.error('webSocket.onerror', evt);
                //alert(evt.message);
                //$("#spanText").append('<div class="alert alert-danger">' + evt.message + '</div>');
                //showError('Нет связи');
            };
            webSocket.onclose = function () {
                console.log('connect webSocket.onclose');
                reconnect();
                isConnected = false;
                //console.log(arguments);
                var arg = arguments;
                var reason = '';
                if (typeof arguments !== undefined && arguments.length > 0 && typeof arguments[0].reason !== 'undefined') {
                    reason = ' reason: ' + arguments[0].reason;
                }
                socketStatusCont.text("disconnected. " + reason);
            };

        },

        reconnectDelay = 500,
        reconnectHander = undefined,
        reconnect = function () {
            //console.log('reconnect ...', typeof reconnectHander);

            // пределаем алгоритм. если хандлер существует то тупо ждем его выполнения новый не создаем
            if (typeof reconnectHander === 'number') {
                //console.log('reconnectHander already exists return.');
                return;
            }
            //clearTimeout(reconnectHander);

            var func = function () {
                reconnectHander = undefined;
                //console.log('reconnect after timeout ...');
                if (isConnected) return; //clearTimeout(reconnectHander); // уже законектились

                connect();

                // зацикливаем пока не законектимся
                reconnectHander = setTimeout(func, reconnectDelay);
            };
            reconnectHander = setTimeout(func, reconnectDelay);
        },

        sendRequest = function (obj) {
            // не делать reconnect(); в этой функции! получается рекурсия при получении конфига из конекта 
            if (!isConnected) {
                //reconnect();
                console.error('disconected');
                
                return;
            }
            
            console.log( 'sendRequest:', obj )
            if (webSocket.readyState === WebSocket.OPEN) {
                clearError();
                var j = JSON.stringify(obj);
                webSocket.send(j);
            } else {
                console.log('sendRequest webSocket.readyState !== WebSocket.OPEN');
                isConnected = false;
                //reconnect();
                socketStatusCont.text("Connection is closed");
            }
        },
        showError = function ( err, action ) {
            //textCont.append( '<div class="alert alert-danger">' + ( action ? '<b>' + action + ':</b> ' : '' ) + err + '</div>' );
            errorCont.html('<div class="alert alert-danger">' + (action ? '<b>' + action + ':</b> ' : '') + err + '</div>');
        },
        readConfig = function () {
            //console.log('readConfig...');
            sendRequest( { Action: 'getnetconfig' } );
        },
        subscribe = function ( ranges ) {
            // сохранять или нет выбранные области? пока нет смысла
            // теперь смысл етсь при реконекте пробуем переподписаться
            subscribeRanges = ranges;
            console.log( 'subscribe' );

            sendRequest( { Action: 'subscribe', ArgsInt: ranges } );
        },
        setInput = function ( ranges ) {
            console.log( 'setInput' );

            sendRequest( { Action: 'setinput', ArgsInt: ranges } );
        },
        start = function ( ) {
            sendRequest( { Action: 'start' } );
        },
        stop = function () {
            sendRequest( { Action: 'stop' } );
        },
        clearSubscribeBlock = function () {
            subscribeCont.MinX.val( '' );
            subscribeCont.MinY.val( '' );
            subscribeCont.MinZ.val( '' );
            subscribeCont.MaxX.val( '' );
            subscribeCont.MaxY.val( '' );
            subscribeCont.MaxZ.val( '' );
            subscribeCont.Button.prop( 'disabled', true );
        },
        fillSubscribeBlock = function ( args ) {
            //console.log('fillSubscribeBlock', args);
            clearSubscribeBlock();
            if ( args.firstN ) {
                subscribeCont.MinX.val( args.firstN.x );
                subscribeCont.MinY.val( args.firstN.y );
                subscribeCont.MinZ.val( args.firstN.z );
            }
            if ( args.secondN ) {
                subscribeCont.MaxX.val( args.secondN.x );
                subscribeCont.MaxY.val( args.secondN.y );
                subscribeCont.MaxZ.val( args.secondN.z );
            }
            if ( args.firstN && args.secondN ) {
                subscribeCont.Button.prop( 'disabled', false );
            }
        },
        drawControls = function ( value ) {
            threadsCont = $( '<div></div>' ).appendTo( element );
            // ----------------------- подписка
            subscribeCont.Form = $( '<form class="subscribe"></form>' )
                .on( 'submit', function () {
                    return false;
                } )
                .appendTo( element );

            subscribeCont.MinX = $( '<input name="ArgsInt" />' ).appendTo( subscribeCont.Form );
            subscribeCont.MinY = $( '<input name="ArgsInt" />' ).appendTo( subscribeCont.Form );
            subscribeCont.MinZ = $( '<input name="ArgsInt" />' ).appendTo( subscribeCont.Form );
            subscribeCont.MaxX = $( '<input name="ArgsInt" />' ).appendTo( subscribeCont.Form );
            subscribeCont.MaxY = $( '<input name="ArgsInt" />' ).appendTo( subscribeCont.Form );
            subscribeCont.MaxZ = $( '<input name="ArgsInt" />' ).appendTo( subscribeCont.Form );

            subscribeCont.Button = $( '<button class="btn btn-primary">Subscribe</button>' )
                .on( 'click', function () {
                    var args = [
                        subscribeCont.MinX.val(), subscribeCont.MinY.val(), subscribeCont.MinZ.val(),
                        subscribeCont.MaxX.val(), subscribeCont.MaxY.val(), subscribeCont.MaxZ.val(),
                    ];

                    subscribe( args );
                    return false;
                } )
                .appendTo( subscribeCont.Form );

            clearSubscribeBlock();
            // ----------------------- конец подписки

            var setInputVal = $( '<input />' ).appendTo( element );
            var setInputButton = $( '<button class="btn btn-warning">Set Input</button>' )
                .on( 'click', function () {
                    var args = [
                        subscribeCont.MinX.val(), subscribeCont.MinY.val(), subscribeCont.MinZ.val(),
                        subscribeCont.MaxX.val(), subscribeCont.MaxY.val(), subscribeCont.MaxZ.val(),
                        setInputVal.val()
                    ];

                    setInput( args );
                    return false;
                } )
                .appendTo( element );

            var startButton = $( '<button class="btn btn-success">Start</button>' )
                .on( 'click', function () {
                    start();
                    return false;
                } )
                .appendTo( element );
            var stopButton = $( '<button class="btn btn-danger">Stop</button>' )
                .on( 'click', function () {
                    stop();
                    return false;
                } )
                .appendTo( element );

            netStatusCont = $('<div class="bt-controls-netstatus">unknown</div>').appendTo(element);
            socketStatusCont = $('<div class="bt-controls-status"></div>').appendTo(element);
            textCont = $('<div class="bt-controls-text"></div>').appendTo(element);
            errorCont = $('<div class="bt-controls-error"></div>').appendTo(element);

            //element.html();
        },

        create = function () {
            drawControls();
            drawCont = $( settings.draw );

            var LongTest = 100123123123;
            console.log('LongTest:', LongTest, LongTest + 1);

            reconnect();
            
            return base;
        };
        /*base.Subscribe = function (ranges) {
            console.log('Subscribe', ranges);
            return subscribe(ranges);
        };*/
        base.fillSubscribeBlock = function ( args ) {
            fillSubscribeBlock( args );
        };
        return create.call( base );
    };


    // обертка для jquery
    $.fn.btControls = function ( options, attrs ) {
        var instance;
        if ( typeof ( options ) == 'string' ) {
            var retValue = null;
            this.each( function () {
                instance = $.data( this, DATA_KEY );
                if ( instance && typeof ( instance[options] ) == 'function' ) {
                    retValue = instance[options].call( instance, attrs );
                }
                return retValue;
            } );
            return retValue;
        }
        var elements = this.each( function () {
            instance = new $.bt.controls( this, options );;
            $.data( this, DATA_KEY, instance );
            return instance;
        } );
        return elements;

    };
})(jQuery);