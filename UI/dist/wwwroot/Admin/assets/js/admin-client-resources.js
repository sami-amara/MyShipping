/*
 * Admin client resources bootstrap.
 * Loads localized labels/alerts/validation texts from hidden JSON elements into window globals.
 */
(function () {
    function parseJsonElementValue(elementId) {
        var element = document.getElementById(elementId);
        if (!element) {
            return null;
        }
        try {
            return JSON.parse(element.value || '{}');
        }
        catch (_a) {
            return null;
        }
    }
    var labels = parseJsonElementValue('app-resource-labels-json');
    var alerts = parseJsonElementValue('app-resource-alerts-json');
    var shipmentValidationTexts = parseJsonElementValue('shipment-validation-texts-json');
    var adminValidationTexts = parseJsonElementValue('admin-validation-texts-json');
    if (labels) {
        window.AppResourceLabels = labels;
    }
    if (alerts) {
        window.AppResourceAlerts = alerts;
    }
    if (shipmentValidationTexts) {
        window.ShipmentValidationTexts = shipmentValidationTexts;
    }
    if (adminValidationTexts) {
        window.AdminValidationTexts = adminValidationTexts;
    }
})();
//# sourceMappingURL=admin-client-resources.js.map