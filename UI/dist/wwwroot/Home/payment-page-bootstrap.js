(function () {
    var dataEl = document.getElementById('payment-alerts-json');
    if (!dataEl)
        return;
    try {
        window.AppResourceAlerts = JSON.parse(dataEl.value || '{}');
    }
    catch (_a) {
        window.AppResourceAlerts = {};
    }
})();
//# sourceMappingURL=payment-page-bootstrap.js.map