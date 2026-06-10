/*
 * Shipment show bootstrap.
 * Reads shipment DTO/config from markup and initializes ShipmentShow module.
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

    function initShipmentShow() {
        if (typeof ShipmentShow === 'undefined' || typeof ShipmentShow.init !== 'function') {
            return;
        }

        var serverDto = parseJson('shipment-show-server-dto');
        var configElement = document.getElementById('shipment-show-config');

        if (!configElement) {
            return;
        }

        ShipmentShow.init(serverDto, {
            redirectUrl: configElement.dataset.redirectUrl,
            enableDelete: configElement.dataset.enableDelete === 'true',
            enableEdit: configElement.dataset.enableEdit === 'true'
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initShipmentShow);
    } else {
        initShipmentShow();
    }
})();
