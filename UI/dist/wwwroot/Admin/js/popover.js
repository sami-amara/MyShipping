(function ($) {
    'use strict';
    $(function () {
        /* Code for attribute data-custom-class for adding custom class to tooltip */
        if (typeof $.fn.popover.Constructor === 'undefined') {
            throw new Error('Bootstrap Popover must be included first!');
        }
        const Popover = $.fn.popover.Constructor;
        // add customClass option to Bootstrap Tooltip
        $.extend(Popover.Default, {
            customClass: ''
        });
        const _show = Popover.prototype.show;
        Popover.prototype.show = function () {
            // invoke parent method
            _show.apply(this, Array.prototype.slice.apply(arguments));
            if (this.config.customClass) {
                const tip = this.getTipElement();
                $(tip).addClass(this.config.customClass);
            }
        };
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        const tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
        const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
        const popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
            return new bootstrap.Popover(popoverTriggerEl);
        });
    });
})(jQuery);
//# sourceMappingURL=popover.js.map