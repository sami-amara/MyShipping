(function () {
    function parseBool(value) {
        return String(value).toLowerCase() === 'true';
    }
    var flagsEl = document.getElementById('shipment-show-flags');
    var modelEl = document.getElementById('shipment-show-model-json');
    if (!flagsEl || !modelEl)
        return;
    var isCreated = parseBool(flagsEl.dataset.isCreated);
    var updated = parseBool(flagsEl.dataset.updated);
    var isPaid = parseBool(flagsEl.dataset.isPaid);
    var serverDto = null;
    try {
        serverDto = JSON.parse(modelEl.value || 'null');
    }
    catch (e) {
        console.error('Failed to parse shipment model JSON', e);
        return;
    }
    if (window.ShipmentShow && typeof window.ShipmentShow.init === 'function') {
        ShipmentShow.init(serverDto, {
            redirectUrl: '/Shipments/List'
        });
    }
    function handlePayClick() {
        var shipmentId = $(this).data('id');
        if (!shipmentId) {
            if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                AppHelper.showToast('Shipment ID is missing.', 'warning');
            }
            return;
        }
        $('#paymentMethodModal').modal('show');
    }
    $(function () {
        $('#btnPay').off('click.shipmentPay').on('click.shipmentPay', handlePayClick);
        $(document).off('click.shipmentPay', '#btnPay').on('click.shipmentPay', '#btnPay', handlePayClick);
    });
    if ((isCreated || updated) && !isPaid) {
        console.log('Stripe scripts block rendered. Condition: isCreated=' + isCreated + ', updated=' + updated + ', IsPaid=' + isPaid);
        console.log('Stripe object available:', typeof Stripe !== 'undefined');
    }
    else {
        console.log('Stripe scripts NOT rendered. Condition: isCreated=' + isCreated + ', updated=' + updated + ', IsPaid=' + isPaid);
    }
})();
//# sourceMappingURL=ShipmentShowPage.js.map