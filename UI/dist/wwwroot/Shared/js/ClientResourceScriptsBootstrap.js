(function () {
    function readJson(id) {
        var el = document.getElementById(id);
        if (!el)
            return null;
        try {
            return JSON.parse(el.value || '{}');
        }
        catch (_a) {
            return null;
        }
    }
    var labels = readJson('app-resource-labels-json');
    if (labels) {
        window.AppResourceLabels = labels;
    }
    var alerts = readJson('app-resource-alerts-json');
    if (alerts) {
        window.AppResourceAlerts = alerts;
    }
    var shipmentValidationTexts = readJson('shipment-validation-texts-json');
    if (shipmentValidationTexts) {
        window.ShipmentValidationTexts = shipmentValidationTexts;
    }
    var adminValidationTexts = readJson('admin-validation-texts-json');
    if (adminValidationTexts) {
        window.AdminValidationTexts = adminValidationTexts;
    }
})();
//# sourceMappingURL=ClientResourceScriptsBootstrap.js.map