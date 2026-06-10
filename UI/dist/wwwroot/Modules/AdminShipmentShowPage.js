(function () {
    var modelEl = document.getElementById('admin-shipment-show-model-json');
    var flagsEl = document.getElementById('admin-shipment-show-flags');
    if (!modelEl || !flagsEl)
        return;
    var serverDto = null;
    try {
        serverDto = JSON.parse(modelEl.value || 'null');
    }
    catch (_a) {
        return;
    }
    ShipmentShow.init(serverDto, {
        redirectUrl: '/admin/Shipments/List',
        enableDelete: String(flagsEl.dataset.enableDelete).toLowerCase() === 'true',
        enableEdit: String(flagsEl.dataset.enableEdit).toLowerCase() === 'true'
    });
})();
//# sourceMappingURL=AdminShipmentShowPage.js.map