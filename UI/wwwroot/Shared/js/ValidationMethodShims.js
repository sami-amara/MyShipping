/* eslint-disable no-var */
/* eslint-disable no-undef */
// Lightweight shim to add missing jquery.validate methods used by unobtrusive data-val attributes.
// Safe to include anywhere — it polls until jQuery + jquery.validate are available.
(function () {
    function install() {
        if (!window.jQuery) return false;
        const $ = window.jQuery;
        if (!$.validator) return false;

        // Add a 'regex' method if missing (maps to ASP.NET [RegularExpression] unobtrusive output).
        if (!$.validator.methods.regex) {
            $.validator.addMethod('regex', function (value, element, param) {
                if (this.optional(element)) return true;
                let pattern = param;
                // param may be an object or a string; handle both shapes
                if (param && typeof param === 'object') {
                    if (param.pattern) pattern = param.pattern;
                    else if (param.regex) pattern = param.regex;
                }
                if (!pattern) return false;
                try {
                    // Strip leading/trailing slashes if present, keep flags if provided as object
                    let flags = '';
                    if (typeof pattern === 'object' && pattern.pattern) {
                        pattern = pattern.pattern;
                        flags = pattern.flags || '';
                    }
                    pattern = ('' + pattern).replace(/^\/|\/$/g, '');
                    const re = new RegExp(pattern, flags);
                    return re.test(value);
                } catch (ex) {
                    if (window.console) console.warn('Validation regex: invalid pattern', pattern, ex);
                    return false;
                }
            }, 'Invalid format.');
        }

        // Provide a no-op for unexpected placeholder rules (e.g. "__dummy__") so validator doesn't throw.
        if (!$.validator.methods.__dummy__) {
            $.validator.addMethod('__dummy__', function () { return true; }, '');
        }

        return true;
    }

    // Try immediately, otherwise poll briefly until ready (max ~2s)
    if (!install()) {
        let tries = 0;
        var t = setInterval(function () {
            tries++;
            if (install() || tries > 40) clearInterval(t);
        }, 50);
    }
})();