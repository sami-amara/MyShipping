$(document).ready(function () {
    $('#openRefundModal').click(function () {
        $('#refundModal').modal('show');
    });
    $('#refundModal').on('hidden.bs.modal', function () {
        $('#reason').val('');
    });
    var typeEl = document.getElementById('payment-transaction-message-type');
    var msgEl = document.getElementById('payment-transaction-message');
    if (!typeEl || !msgEl)
        return;
    var messageType = parseInt(typeEl.value || '0', 10);
    var message = msgEl.value || '';
    if (messageType === 1 && message) {
        showAlert.Success('Success', message);
    }
    else if (messageType === 2 && message) {
        showAlert.Error('Error', message);
    }
});
//# sourceMappingURL=payment-transactions-details.js.map