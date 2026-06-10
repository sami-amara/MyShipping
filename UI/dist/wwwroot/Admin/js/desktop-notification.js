(function ($) {
    'use strict';
    $.fn.easyNotify = function (options) {
        const settings = $.extend({
            title: 'Notification',
            options: {
                body: '',
                icon: '',
                lang: 'pt-BR',
                onClose: '',
                onClick: '',
                onError: ''
            }
        }, options);
        this.init = function () {
            const notify = this;
            if (!('Notification' in window)) {
                alert('This browser does not support desktop notification');
            }
            else if (Notification.permission === 'granted') {
                const notification = new Notification(settings.title, settings.options);
                notification.onclose = function () {
                    if (typeof settings.options.onClose === 'function') {
                        settings.options.onClose();
                    }
                };
                notification.onclick = function () {
                    if (typeof settings.options.onClick === 'function') {
                        settings.options.onClick();
                    }
                };
                notification.onerror = function () {
                    if (typeof settings.options.onError === 'function') {
                        settings.options.onError();
                    }
                };
            }
            else if (Notification.permission !== 'denied') {
                Notification.requestPermission(function (permission) {
                    if (permission === 'granted') {
                        notify.init();
                    }
                });
            }
        };
        this.init();
        return this;
    };
    //Initialise notification
    const myFunction = function () {
        alert('Click function');
    };
    const myImg = 'https://unsplash.it/600/600?image=777';
    $('form').submit(function (event) {
        event.preventDefault();
        const options = {
            title: $('#title').val(),
            options: {
                body: $('#message').val(),
                icon: myImg,
                lang: 'en-US',
                onClick: myFunction
            }
        };
        console.log(options);
        $('#easyNotify').easyNotify(options);
    });
}(jQuery));
//# sourceMappingURL=desktop-notification.js.map