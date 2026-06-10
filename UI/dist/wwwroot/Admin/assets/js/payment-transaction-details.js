/*
 * Payment transaction details bootstrap.
 * Handles refund modal interactions and TempData alert display for the details page.
 */
(function () {
    function parseJson(id) {
        var element = document.getElementById(id);
        if (!element) {
            return null;
        }
        try {
            return JSON.parse(element.value || 'null');
        }
        catch (_a) {
            return null;
        }
    }
    function initPaymentTransactionDetails() {
        var openRefundModalButton = document.getElementById('openRefundModal');
        if (openRefundModalButton) {
            openRefundModalButton.addEventListener('click', function () {
                $('#refundModal').modal('show');
            });
        }
        $('#refundModal').on('hidden.bs.modal', function () {
            $('#reason').val('');
        });
        var alertConfig = document.getElementById('payment-transaction-alert-config');
        if (!alertConfig || typeof showAlert === 'undefined') {
            return;
        }
        var messageType = parseInt(alertConfig.dataset.messageType || '0', 10);
        var message = parseJson('payment-transaction-alert-message');
        if (!message) {
            return;
        }
        if (messageType === 1) {
            showAlert.Success('Success', message);
        }
        else if (messageType === 2) {
            showAlert.Error('Error', message);
        }
    }
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initPaymentTransactionDetails);
    }
    else {
        initPaymentTransactionDetails();
    }
})();
//# sourceMappingURL=payment-transaction-details.js.map