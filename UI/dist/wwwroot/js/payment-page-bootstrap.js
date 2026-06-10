/*
 * Payment page bootstrap.
 * Loads payment alert texts from hidden markup for the PayPal checkout page.
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
    var paymentAlerts = parseJson('payment-alerts-json');
    if (paymentAlerts) {
        window.AppResourceAlerts = Object.assign({}, window.AppResourceAlerts || {}, paymentAlerts);
    }
})();
//# sourceMappingURL=payment-page-bootstrap.js.map