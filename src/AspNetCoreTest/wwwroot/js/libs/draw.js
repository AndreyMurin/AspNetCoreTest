(function ($) {
    // задаем пространство имен
    if (!$.bt) $.bt = {};

    var DATA_KEY = 'bt-draw';

    $.bt.draw = function (elem, options) {
        var defaultSettings = {
            controls: ".js-bt-controls",
        },
        settings = $.extend({}, defaultSettings, options),
        base = this,
        element = $(elem),
        netConfig,
        controlsCont,
        drawNet = function () {
            console.log('drawNet', netConfig);
        },
        create = function () {
            controlsCont = $(settings.controls);


            return base;
        };
        this.setConfig = function (config) {
            netConfig = config;
            drawNet();
        };

        return create.call(this);
    };


    // обертка для jquery
    $.fn.btDraw = function (options, attrs) {
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
                instance = new $.bt.draw(this, options);;
                $.data(this, DATA_KEY, instance);
                return instance;
            });
            return elements;
        }
    };
})(jQuery);