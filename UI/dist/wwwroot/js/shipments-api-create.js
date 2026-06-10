/*
 * Shipment API create bootstrap.
 * Initializes the shipment list rendering for the API-backed sample view.
 */
(function () {
    function initApiCreateShipments() {
        if (typeof ShipmentService === 'undefined' || typeof ShipmentService.initList !== 'function') {
            return;
        }
        ShipmentService.initList({
            tableSelector: '#shipments-table',
            tableBodySelector: '#shipments-table-body'
        });
    }
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initApiCreateShipments);
    }
    else {
        initApiCreateShipments();
    }
})();
//# sourceMappingURL=shipments-api-create.js.map