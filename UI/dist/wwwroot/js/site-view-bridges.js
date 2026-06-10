/*
 * Public view bridges.
 * Loads localized resources and validation texts from hidden JSON elements into window globals.
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
    var paymentAlerts = parseJsonElementValue('payment-alerts-json');
    var resetPasswordAlerts = parseJsonElementValue('reset-password-alerts-json');
    if (labels) {
        window.AppResourceLabels = labels;
    }
    if (alerts) {
        window.AppResourceAlerts = alerts;
    }
    if (shipmentValidationTexts) {
        window.ShipmentValidationTexts = shipmentValidationTexts;
    }
    if (paymentAlerts) {
        window.AppResourceAlerts = Object.assign({}, window.AppResourceAlerts || {}, paymentAlerts);
    }
    if (resetPasswordAlerts) {
        window.ResetPasswordAlertTexts = resetPasswordAlerts;
    }
})();
//# sourceMappingURL=site-view-bridges.js.map