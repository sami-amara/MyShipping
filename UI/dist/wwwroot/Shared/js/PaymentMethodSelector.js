function selectPaymentMethod(method, shipmentId) {
    console.log('Payment method selected:', method, 'for shipment:', shipmentId);
    if (method === 'paypal') {
        $('#paymentMethodModal').modal('hide');
        setTimeout(function () {
            window.location.href = '/Home/Payment?shipmentId=' + shipmentId + '&method=paypal';
        }, 300);
    }
    else if (method === 'stripe') {
        $('#paymentMethodModal').modal('hide');
        setTimeout(function () {
            openStripePaymentModal(shipmentId);
        }, 300);
    }
}
//# sourceMappingURL=PaymentMethodSelector.js.map