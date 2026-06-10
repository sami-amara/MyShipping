/*
 * Shipment list alert bootstrap.
 * Shows created/updated/deleted success notifications based on URL parameters.
 */
(function () {
    function notifySuccess(title, message, cleanUrlDelay) {
        if (window.Swal) {
            Swal.fire({ title: title, text: message, icon: 'success', confirmButtonText: 'OK' }).then(function () {
                if (window.history && window.history.replaceState) {
                    window.history.replaceState({}, document.title, window.location.pathname);
                }
            });
            return;
        }
        if (window.showAlert && typeof showAlert.Success === 'function') {
            showAlert.Success(title, message);
        }
        else if (window.AppHelper && typeof AppHelper.showToast === 'function') {
            AppHelper.showToast(message, 'success');
        }
        setTimeout(function () {
            if (window.history && window.history.replaceState) {
                window.history.replaceState({}, document.title, window.location.pathname);
            }
        }, cleanUrlDelay || 1000);
    }
    function initShipmentListAlerts() {
        var urlParams = new URLSearchParams(window.location.search);
        if (urlParams.get('created') === '1') {
            setTimeout(function () {
                notifySuccess('Shipment Created', 'Your shipment has been created successfully!', 1000);
            }, 500);
        }
        if (urlParams.get('updated') === '1') {
            setTimeout(function () {
                notifySuccess('Shipment Updated', 'Shipment updated successfully ', 1000);
            }, 500);
        }
        if (urlParams.get('deleted') === '1') {
            setTimeout(function () {
                notifySuccess('Shipment Deleted', 'Shipment deleted successfully', 1000);
            }, 500);
        }
    }
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initShipmentListAlerts);
    }
    else {
        initShipmentListAlerts();
    }
})();
//# sourceMappingURL=shipments-list-alerts.js.map