// Simple shim that ensures AppHelper.showToast exists.
// If your app provides AppHelper.showToast or showAlert, this will not overwrite them.
// Fallback uses simple alert() or console when not available.
window.AppHelper = window.AppHelper || {};
if (typeof AppHelper.showToast !== 'function') {
    AppHelper.showToast = function (message, type) {
        try {
            // prefer a small DOM toast if bootstrap toasts exist
            if (window.bootstrap && typeof bootstrap.Toast === 'function') {
                // create ephemeral toast element
                const id = 'shim-toast-' + Date.now();
                const container = document.getElementById('shim-toast-container') || (function () {
                    const c = document.createElement('div');
                    c.id = 'shim-toast-container';
                    c.style.position = 'fixed';
                    c.style.top = '1rem';
                    c.style.right = '1rem';
                    c.style.zIndex = 2147483647;
                    document.body.appendChild(c);
                    return c;
                })();
                const toastEl = document.createElement('div');
                toastEl.id = id;
                toastEl.className = 'toast align-items-center text-white bg-' + (type === 'error' ? 'danger' : (type === 'success' ? 'success' : 'secondary')) + ' border-0';
                toastEl.setAttribute('role', 'alert');
                toastEl.setAttribute('aria-live', 'assertive');
                toastEl.setAttribute('aria-atomic', 'true');
                toastEl.style.minWidth = '200px';
                toastEl.innerHTML = `<div class="d-flex"><div class="toast-body">${message}</div><button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button></div>`;
                container.appendChild(toastEl);
                // eslint-disable-next-line no-undef
                const toast = new bootstrap.Toast(toastEl, { delay: 3000 });
                toast.show();
                // remove after hidden
                toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove(), { once: true });
                return;
            }
        }
        catch (e) { /* ignore */ }
        // final fallback
        try {
            console.log('[ToastShim]', type || 'info', message);
            alert(message);
        }
        catch (ignored) { /* no-op */ }
    };
}
//# sourceMappingURL=ToastShim.js.map