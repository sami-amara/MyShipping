$(document).ready(function () {
    const urlParams = new URLSearchParams(window.location.search);
    function cleanupUrl() {
        if (window.history && window.history.replaceState) {
            window.history.replaceState({}, document.title, window.location.pathname);
        }
    }
    if (urlParams.get('created') === '1') {
        setTimeout(function () {
            const title = 'Shipment Created';
            const message = 'Your shipment has been created successfully From List View ! Payment has been processed.';
            if (window.Swal) {
                Swal.fire({
                    title: title,
                    text: message,
                    icon: 'success',
                    confirmButtonText: 'OK'
                }).then(function () {
                    cleanupUrl();
                });
            }
            else if (window.showAlert && typeof showAlert.Success === 'function') {
                showAlert.Success(title, message);
                setTimeout(cleanupUrl, 1000);
            }
            else if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                AppHelper.showToast(message, 'success');
                setTimeout(cleanupUrl, 3000);
            }
        }, 500);
    }
    if (urlParams.get('updated') === '1') {
        setTimeout(function () {
            const title = 'Shipment Updated';
            const message = 'Shipment updated successfully ';
            if (window.Swal) {
                Swal.fire({ title: title, text: message, icon: 'success' });
            }
            else if (window.showAlert && typeof showAlert.Success === 'function') {
                showAlert.Success(title, message);
            }
            else if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                AppHelper.showToast(message, 'success');
            }
        }, 500);
    }
    if (urlParams.get('deleted') === '1') {
        setTimeout(function () {
            const title = 'Shipment Deleted';
            const message = 'Shipment deleted successfully from List View';
            if (window.Swal) {
                Swal.fire({ title: title, text: message, icon: 'success' });
            }
            else if (window.showAlert && typeof showAlert.Success === 'function') {
                showAlert.Success(title, message);
            }
            else if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                AppHelper.showToast(message, 'success');
            }
        }, 500);
    }
});
//# sourceMappingURL=ShipmentsListPage.js.map