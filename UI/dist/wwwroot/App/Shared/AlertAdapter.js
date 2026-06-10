(function () {
    // idempotent adapter: only install once
    if (window.__AlertAdapterInstalled)
        return;
    window.__AlertAdapterInstalled = true;
    window.AppHelper = window.AppHelper || {};
    // preserve original
    const _origShowToast = typeof AppHelper.showToast === 'function' ? AppHelper.showToast : null;
    // normalize inputs: message (string), type ('success'|'error'|'info'), options (object)
    AppHelper.showToast = function (message, type = 'info', options = {}) {
        try {
            // Prefer showAlert API when available
            if (window.showAlert) {
                // try common variants in your project
                if (type === 'success' && typeof showAlert.Success === 'function') {
                    return showAlert.Success(options.title || 'Success', message, options);
                }
                if (type === 'error' && typeof showAlert.Error === 'function') {
                    return showAlert.Error(options.title || 'Error', message, options);
                }
                if (typeof showAlert.Info === 'function') {
                    return showAlert.Info(options.title || 'Info', message, options);
                }
                // Generic send if showAlert exposes a single method
                if (typeof showAlert === 'function') {
                    return showAlert(message, type, options);
                }
                // If showAlert object has notify
                if (typeof showAlert.notify === 'function') {
                    return showAlert.notify(message, type, options);
                }
            }
        }
        catch (ex) {
            // fall back to original if adapter fails
            console.warn('AlertAdapter: showAlert call failed', ex);
        }
        // fallback to original AppHelper.showToast if present
        if (_origShowToast) {
            try {
                return _origShowToast(message, type, options);
            }
            catch (ex) { /* ignore */ }
        }
        // final fallback: browser console
        if (type === 'error')
            console.error(message);
        else
            console.log(type + ': ' + message);
    };
    // also preserve a showAlert-aware convenience for callers that expect a Promise
    AppHelper.showToastAsync = function (message, type = 'info', options = {}) {
        try {
            const result = AppHelper.showToast(message, type, options);
            // if showAlert returns a promise-like, return it
            if (result && typeof result.then === 'function')
                return result;
        }
        catch (ex) { /* ignore */ }
        return Promise.resolve();
    };
})();
//(function () {
//    // Ensure AppHelper exists
//    window.AppHelper = window.AppHelper || {};
//    // Preserve any existing implementation
//    const _origShowToast = typeof AppHelper.showToast === 'function' ? AppHelper.showToast : null;
//    // Adapter: prefer showAlert, fallback to original showToast, fallback to console
//    AppHelper.showToast = function (message, type = 'info', options = {}) {
//        try {
//            if (window.showAlert && typeof showAlert === 'object') {
//                // map types to showAlert methods; adjust to your showAlert API
//                if (type === 'success' && typeof showAlert.Success === 'function') {
//                    showAlert.Success(options.title || 'Success', message, options);
//                    return;
//                }
//                if (type === 'error' && typeof showAlert.Error === 'function') {
//                    showAlert.Error(options.title || 'Error', message, options);
//                    return;
//                }
//                if (typeof showAlert.Info === 'function') {
//                    showAlert.Info(options.title || 'Info', message, options);
//                    return;
//                }
//            }
//        } catch (ex) {
//            // fall through to original
//            console.warn('AlertAdapter: showAlert failed', ex);
//        }
//        if (_origShowToast) {
//            try { _origShowToast(message, type, options); return; } catch (ex) { /* ignore */ }
//        }
//        // final fallback
//        if (type === 'error') console.error(message); else console.log(message);
//    };
//})();
//# sourceMappingURL=AlertAdapter.js.map