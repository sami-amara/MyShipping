/* eslint-disable no-undef */
(function () {
    'use strict';

    const AppHelper = {
        getCookie: function (name) {
            const value = `; ${document.cookie}`;
            const parts = value.split(`; ${name}=`);
            if (parts.length === 2) return parts.pop().split(';').shift();
            return null;
        },

        
        showToast: function (message, type = 'info') {
            if (window.toastr) {
                window.toastr[type](message);
            } else {
                // fallback: simple alert for dev
                alert(message);
            }
        },
       

        // Return ISO string or readable format if requested
        formatDate: function (data, format = 'iso') {
            if (!data) return '';
            const d = new Date(data);
            if (isNaN(d.getTime())) return String(data);
            if (format === 'iso') return d.toISOString();
            // simple yyyy-MM-dd
            const y = d.getFullYear();
            const m = ('0' + (d.getMonth() + 1)).slice(-2);
            const day = ('0' + d.getDate()).slice(-2);
            return `${y}-${m}-${day}`;
        },

        // Format currency according to provided locale/currency
        formatCurrency: function (value, currency = 'USD', locale) {
            if (value === null || value === undefined || value === '') return '-';
            locale = locale || (navigator.language || 'en-US');
            try {
                return new Intl.NumberFormat(locale, { style: 'currency', currency: currency }).format(Number(value));
            } catch (e) {
                return String(value);
            }
        },

        setCookie: function (name, value, days) {
            let expires = '';
            if (days) {
                const date = new Date();
                date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
                expires = '; expires=' + date.toUTCString();
            }
            document.cookie = name + '=' + (value || '') + expires + '; path=/';
        },

        getQueryParam: function (name) {
            const urlParams = new URLSearchParams(window.location.search);
            return urlParams.get(name);
        },

        getIdFromPath: function () {
            const segments = window.location.pathname.split('/');
            return segments[segments.length - 1];
        },

        removeCookie: function (name) {
            document.cookie = name + '=; Max-Age=-99999999; path=/';
        }
    };

    window.AppHelper = AppHelper;
})();
