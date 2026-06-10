/*
 * Payment method selector.
 * Handles selector card clicks and routes to PayPal or Stripe flows.
 */
(function () {
    function closePaymentSelectorModal() {
        $('#paymentMethodModal').modal('hide');
    }
    function selectPaymentMethod(method, shipmentId) {
        if (method === 'paypal') {
            closePaymentSelectorModal();
            setTimeout(function () {
                window.location.href = '/Home/Payment?shipmentId=' + shipmentId + '&method=paypal';
            }, 300);
            return;
        }
        if (method === 'stripe') {
            closePaymentSelectorModal();
            setTimeout(function () {
                if (typeof openStripePaymentModal === 'function') {
                    openStripePaymentModal(shipmentId);
                }
            }, 300);
        }
    }
    function bindPaymentMethodCards() {
        var cards = document.querySelectorAll('#paymentMethodModal .payment-method-card[data-payment-method][data-shipment-id]');
        cards.forEach(function (card) {
            card.addEventListener('click', function () {
                selectPaymentMethod(card.dataset.paymentMethod, card.dataset.shipmentId);
            });
        });
    }
    window.selectPaymentMethod = selectPaymentMethod;
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', bindPaymentMethodCards);
    }
    else {
        bindPaymentMethodCards();
    }
})();
//# sourceMappingURL=payment-method-selector.js.map