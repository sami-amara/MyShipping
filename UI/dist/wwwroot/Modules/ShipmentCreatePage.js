(function () {
    var uiTextsEl = document.getElementById('shipment-ui-texts-json');
    if (!uiTextsEl)
        return;
    try {
        window.ShipmentUiTexts = JSON.parse(uiTextsEl.value || '{}');
    }
    catch (_a) {
        window.ShipmentUiTexts = {};
    }
})();
//# sourceMappingURL=ShipmentCreatePage.js.map