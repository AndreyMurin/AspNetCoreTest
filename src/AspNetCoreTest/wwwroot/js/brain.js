(function ($) {
    "use strict"; // jshint ;_;

    $().ready(function () {

        $('.js-bt-draw').btDraw({ controls: '.js-bt-controls' });
        $('.js-bt-controls').btControls({ draw: '.js-bt-draw' });

    });
})(jQuery);