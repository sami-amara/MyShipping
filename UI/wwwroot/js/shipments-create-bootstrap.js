/*
 * Shipment create bootstrap.
 * Rebuilds client texts from hidden JSON and initializes view-only create behaviors.
 */
(function () {
    function parseJson(id) {
        var element = document.getElementById(id);
        if (!element) {
            return null;
        }

        try {
            return JSON.parse(element.value || 'null');
        } catch {
            return null;
        }
    }

    var shipmentUiTexts = parseJson('shipment-ui-texts-json');
    if (shipmentUiTexts) {
        window.ShipmentUiTexts = shipmentUiTexts;
    }
})();
