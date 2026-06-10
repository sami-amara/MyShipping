var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
(function () {
    function getAntiForgeryToken() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : null;
    }
    document.addEventListener('click', function (e) {
        const btn = e.target.closest && e.target.closest('.btn-delete');
        if (!btn)
            return;
        e.preventDefault();
        const id = btn.getAttribute('data-id');
        if (!id)
            return;
        const table = document.getElementById('shipments-table');
        const deleteUrl = (table && table.dataset && table.dataset.deleteUrl) ? table.dataset.deleteUrl : '';
        if (!deleteUrl)
            return;
        const confirmAndDelete = function () {
            const token = getAntiForgeryToken();
            btn.disabled = true;
            btn.setAttribute('aria-disabled', 'true');
            const headers = new Headers();
            headers.append('X-Requested-With', 'XMLHttpRequest');
            if (token)
                headers.append('RequestVerificationToken', token);
            headers.append('Content-Type', 'application/x-www-form-urlencoded');
            const body = new URLSearchParams();
            body.append('id', id);
            fetch(deleteUrl, {
                method: 'POST',
                credentials: 'same-origin',
                headers: headers,
                body: body.toString()
            })
                .then(function (resp) {
                return __awaiter(this, void 0, void 0, function* () {
                    const json = yield resp.json().catch(function () { return null; });
                    if (!resp.ok)
                        throw json || new Error('Delete failed');
                    return json;
                });
            })
                .then(function (json) {
                const ok = json && (json.success === true || json.success === 'true');
                const message = (json && (json.message || json.Message)) || 'Shipment deleted';
                if (ok) {
                    try {
                        if (window.showAlert && typeof showAlert.Success === 'function') {
                            if (showAlert.Success.length >= 3) {
                                showAlert.Success('Deleted', message, function () {
                                    const row = btn.closest('tr');
                                    if (row)
                                        row.remove();
                                });
                            }
                            else {
                                showAlert.Success('Deleted', message);
                                const row = btn.closest('tr');
                                if (row)
                                    row.remove();
                            }
                        }
                        else if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                            AppHelper.showToast(message, 'success');
                            const row = btn.closest('tr');
                            if (row)
                                row.remove();
                        }
                        else {
                            alert(message);
                            const row = btn.closest('tr');
                            if (row)
                                row.remove();
                        }
                    }
                    catch (ex) {
                        console.warn('Delete success handler failed', ex);
                        const row = btn.closest('tr');
                        if (row)
                            row.remove();
                    }
                }
                else {
                    const errMsg = message || 'Failed to delete shipment';
                    if (window.showAlert && typeof showAlert.Error === 'function')
                        showAlert.Error('Error', errMsg);
                    else if (window.AppHelper && typeof AppHelper.showToast === 'function')
                        AppHelper.showToast(errMsg, 'error');
                    else
                        alert(errMsg);
                }
            })
                .catch(function (err) {
                console.error('Delete request failed', err);
                const msg = (err && (err.message || err.body)) ? (err.message || err.body) : 'Failed to delete shipment';
                if (window.showAlert && typeof showAlert.Error === 'function')
                    showAlert.Error('Error', msg);
                else if (window.AppHelper && typeof AppHelper.showToast === 'function')
                    AppHelper.showToast(msg, 'error');
                else
                    alert(msg);
            })
                .finally(function () {
                try {
                    btn.disabled = false;
                    btn.removeAttribute('aria-disabled');
                }
                catch (_a) { }
            });
        };
        try {
            if (window.showAlert && typeof showAlert.ConfirmDelete === 'function') {
                try {
                    showAlert.ConfirmDelete(function (ok) {
                        if (ok)
                            confirmAndDelete();
                    });
                }
                catch (ex) {
                    try {
                        showAlert.ConfirmDelete(function () { confirmAndDelete(); }, function () { });
                    }
                    catch (e) {
                        if (confirm('Are you sure you want to delete this shipment?'))
                            confirmAndDelete();
                    }
                }
            }
            else {
                if (confirm('Are you sure you want to delete this shipment?'))
                    confirmAndDelete();
            }
        }
        catch (ex) {
            if (confirm('Are you sure you want to delete this shipment?'))
                confirmAndDelete();
        }
    });
})();
//# sourceMappingURL=ShipmentsIndexPage.js.map