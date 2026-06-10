/*
 * Shipment show bootstrap.
 * Rebuilds shipment DTO/config data and wires pay button behavior.
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
    function initShipmentShowPage() {
        var serverDto = parseJson('shipment-show-server-dto');
        var configElement = document.getElementById('shipment-show-config');
        if (typeof ShipmentShow === 'undefined' || typeof ShipmentShow.init !== 'function' || !configElement) {
            return;
        }
        ShipmentShow.init(serverDto, {
            redirectUrl: configElement.dataset.redirectUrl
        });
        var payButton = document.getElementById('btnPay');
        if (!payButton) {
            return;
        }
        function handlePayClick() {
            var shipmentId = payButton.dataset.id;
            if (!shipmentId) {
                if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                    AppHelper.showToast('Shipment ID is missing.', 'warning');
                }
                return;
            }
            $('#paymentMethodModal').modal('show');
        }
        payButton.removeEventListener('click', handlePayClick);
        payButton.addEventListener('click', handlePayClick);
    }
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initShipmentShowPage);
    }
    else {
        initShipmentShowPage();
    }
})();
//# sourceMappingURL=shipments-show-bootstrap.js.map