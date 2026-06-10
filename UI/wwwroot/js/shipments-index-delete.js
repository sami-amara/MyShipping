/*
 * Shipment index delete handler.
 * Handles AJAX deletion with antiforgery support and row removal.
 */
(function () {
    function getAntiForgeryToken() {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : null;
    }

    function initShipmentIndexDelete() {
        document.addEventListener('click', function (e) {
            var btn = e.target.closest && e.target.closest('.btn-delete');
            if (!btn) {
                return;
            }

            e.preventDefault();
            var id = btn.getAttribute('data-id');
            if (!id) {
                return;
            }

            var confirmAndDelete = function () {
                var token = getAntiForgeryToken();
                var deleteUrl = btn.dataset.deleteUrl;

                btn.disabled = true;
                btn.setAttribute('aria-disabled', 'true');

                var headers = new Headers();
                headers.append('X-Requested-With', 'XMLHttpRequest');
                if (token) {
                    headers.append('RequestVerificationToken', token);
                }
                headers.append('Content-Type', 'application/x-www-form-urlencoded');

                var body = new URLSearchParams();
                body.append('id', id);

                fetch(deleteUrl, {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: headers,
                    body: body.toString()
                })
                    .then(async function (resp) {
                        var json = await resp.json().catch(function () { return null; });
                        if (!resp.ok) {
                            throw json || new Error('Delete failed');
                        }
                        return json;
                    })
                    .then(function (json) {
                        var ok = json && (json.success === true || json.success === 'true');
                        var message = (json && (json.message || json.Message)) || 'Shipment deleted';
                        if (!ok) {
                            throw new Error(message);
                        }

                        if (window.showAlert && typeof showAlert.Success === 'function') {
                            showAlert.Success('Deleted', message);
                        } else if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                            AppHelper.showToast(message, 'success');
                        }

                        var row = btn.closest('tr');
                        if (row) {
                            row.remove();
                        }
                    })
                    .catch(function (err) {
                        console.error('Delete request failed', err);
                        var msg = (err && (err.message || err.body)) ? (err.message || err.body) : 'Failed to delete shipment';
                        if (window.showAlert && typeof showAlert.Error === 'function') {
                            showAlert.Error('Error', msg);
                        } else if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                            AppHelper.showToast(msg, 'error');
                        } else {
                            alert(msg);
                        }
                    })
                    .finally(function () {
                        try {
                            btn.disabled = false;
                            btn.removeAttribute('aria-disabled');
                        } catch {
                        }
                    });
            };

            try {
                if (window.showAlert && typeof showAlert.ConfirmDelete === 'function') {
                    showAlert.ConfirmDelete(function (ok) {
                        if (ok) {
                            confirmAndDelete();
                        }
                    });
                } else if (confirm('Are you sure you want to delete this shipment?')) {
                    confirmAndDelete();
                }
            } catch {
                if (confirm('Are you sure you want to delete this shipment?')) {
                    confirmAndDelete();
                }
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initShipmentIndexDelete);
    } else {
        initShipmentIndexDelete();
    }
})();
