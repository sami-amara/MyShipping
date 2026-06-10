/* eslint-disable no-undef */
// Shipped.js: Handles Mark Shipped functionality
(function () {
    'use strict';
    $(document).ready(function () {
        const btn = document.querySelector('#btnMarkShipped');
        if (!btn)
            return;
        btn.addEventListener('click', function () {
            const idInput = document.querySelector('input[name="Id"]');
            const id = idInput === null || idInput === void 0 ? void 0 : idInput.value;
            if (!id) {
                if (window.showAlert && typeof showAlert.Error === 'function') {
                    showAlert.Error('Error', 'Missing shipment ID');
                }
                else {
                    alert('Missing shipment ID');
                }
                return;
            }
            if (window.ShipmentService && typeof ShipmentService.shippedShipment === 'function') {
                ShipmentService.shippedShipment(id, {
                    button: btn,
                    redirect: true,
                    redirectUrl: '/admin/Shipments/List?shipped=1&shippedId=' + encodeURIComponent(id)
                }).catch(err => {
                    var _a;
                    if (err && err.mapped)
                        return;
                    const msg = (err && (err.message || ((_a = err.responseJSON) === null || _a === void 0 ? void 0 : _a.message))) || 'Failed to mark as shipped';
                    if (window.showAlert && typeof showAlert.Error === 'function') {
                        showAlert.Error('Error', msg);
                    }
                });
            }
            else {
                console.error('ShipmentService.shippedShipment not available');
                if (window.showAlert && typeof showAlert.Error === 'function') {
                    showAlert.Error('Error', 'Shipment service not available');
                }
            }
        });
    });
})();
//# sourceMappingURL=Shipped.js.map