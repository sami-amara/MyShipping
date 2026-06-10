/* eslint-disable no-undef */
// MarkReady.js: Handles Mark Ready for Shipping functionality
// Uses ShipmentService.adminActions() which centralizes all logic
(function () {
    'use strict';
    $(document).ready(function () {
        // Try multiple form selectors for flexibility
        const formSelector = '#createShipmentForm, #makeReadyForm';
        const $form = $(formSelector).first();
        // ═══════════════════════════════════════════════════════════════
        // 1. POPULATE CARRIER DROPDOWN (if exists on page)
        // ═══════════════════════════════════════════════════════════════
        const select = document.querySelector('#deliveryManId');
        if (select) {
            if (window.ManagePageControlls && typeof window.ManagePageControlls.fillCarrierDropdown === 'function') {
                const currentCarrierId = select.getAttribute('data-current-carrier') || null;
                //console.log('MarkReady.js: Populating carriers, current:', currentCarrierId);
                window.ManagePageControlls.fillCarrierDropdown('#deliveryManId', currentCarrierId);
            }
            else {
                //console.error('ManagePageControlls.fillCarrierDropdown not available');
            }
        }
        // ═══════════════════════════════════════════════════════════════
        // 2. MARK READY BUTTON HANDLER
        // ═══════════════════════════════════════════════════════════════
        const bindButton = (btnSelector) => {
            const $btn = $(btnSelector);
            if (!$btn || !$btn.length)
                return;
            try {
                $btn.off('click.markready');
            }
            catch (_a) { }
            $btn.on('click.markready', function (e) {
                var _a, _b, _c, _d, _e, _f, _g;
                e.preventDefault();
                const formEl = $form[0];
                if (!formEl)
                    return;
                const id = (_a = formEl.querySelector('[name="Id"]')) === null || _a === void 0 ? void 0 : _a.value;
                if (!id) {
                    alert('Missing shipment id');
                    return;
                }
                const carrierEl = formEl.querySelector('[name="DeliveryManId"]') || document.querySelector('#deliveryManId');
                const carrierId = carrierEl ? (carrierEl.value || null) : null;
                if (!carrierId) {
                    if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                        AppHelper.showToast('Please select a carrier before marking ready', 'error');
                    }
                    else {
                        alert('Please select a carrier before marking ready');
                    }
                    return;
                }
                // Use centralized ShipmentService.adminActions()
                // This method handles: save form data, call ready API, redirect with success notification
                if (window.ShipmentService && typeof ShipmentService.adminActions === 'function') {
                    // Use 4 for ready, or your enum if available
                    const readyState = (typeof Business !== 'undefined' && ((_d = (_c = (_b = Business.Services) === null || _b === void 0 ? void 0 : _b.Shipment) === null || _c === void 0 ? void 0 : _c.ShipmentStatusEnum) === null || _d === void 0 ? void 0 : _d.ReadyForShipping))
                        ? Business.Services.Shipment.ShipmentStatusEnum.ReadyForShipping
                        : (typeof Business !== 'undefined' && ((_g = (_f = (_e = Business.Services) === null || _e === void 0 ? void 0 : _e.Shipment) === null || _f === void 0 ? void 0 : _f.ShipmentStatusEnum) === null || _g === void 0 ? void 0 : _g.Ready))
                            ? Business.Services.Shipment.ShipmentStatusEnum.Ready
                            : 4;
                    ShipmentService.adminActions(id, {
                        action: 'changeStatus',
                        targetState: readyState,
                        carrierId: carrierId,
                        button: $btn[0],
                        redirect: true,
                        redirectUrl: '/admin/Shipments/List?ready=1&readyId=' + encodeURIComponent(id)
                    })
                        .catch(err => {
                        //console.error('adminActions failed', err);
                        // Error already shown by ShipmentService
                    });
                }
                else {
                    //console.error('ShipmentService.adminActions not available');
                    alert('Unable to mark shipment ready - service not loaded');
                }
            });
        };
        // Bind to both button variants
        bindButton('#btnMarkReady');
        bindButton('#btnMakeReady');
    });
})();
/* eslint-disable no-undef */
//// MarkReady.js: Handles Mark Ready for Shipping functionality
//// Uses ShipmentService.markReadyShipment() which centralizes all logic
//(function () {
//    'use strict';
//    $(document).ready(function () {
//        // Try multiple form selectors for flexibility
//        const formSelector = '#createShipmentForm, #makeReadyForm';
//        const $form = $(formSelector).first();
//        // ═══════════════════════════════════════════════════════════════
//        // 1. POPULATE CARRIER DROPDOWN (if exists on page)
//        // ═══════════════════════════════════════════════════════════════
//        const select = document.querySelector('#deliveryManId');
//        if (select) {
//            if (window.ManagePageControlls && typeof window.ManagePageControlls.fillCarrierDropdown === 'function') {
//                const currentCarrierId = select.getAttribute('data-current-carrier') || null;
//                //console.log('MarkReady.js: Populating carriers, current:', currentCarrierId);
//                window.ManagePageControlls.fillCarrierDropdown('#deliveryManId', currentCarrierId);
//            } else {
//                //console.error('ManagePageControlls.fillCarrierDropdown not available');
//            }
//        }
//        // ═══════════════════════════════════════════════════════════════
//        // 2. MARK READY BUTTON HANDLER
//        // ═══════════════════════════════════════════════════════════════
//        const bindButton = (btnSelector) => {
//            const $btn = $(btnSelector);
//            if (!$btn || !$btn.length) return;
//            try { $btn.off('click.markready'); } catch { }
//            $btn.on('click.markready', function (e) {
//                e.preventDefault();
//                const formEl = $form[0];
//                if (!formEl) return;
//                const id = formEl.querySelector('[name="Id"]')?.value;
//                if (!id) {
//                    alert('Missing shipment id');
//                    return;
//                }
//                const carrierEl = formEl.querySelector('[name="DeliveryManId"]') || document.querySelector('#deliveryManId');
//                const carrierId = carrierEl ? (carrierEl.value || null) : null;
//                if (!carrierId) {
//                    if (window.AppHelper && typeof AppHelper.showToast === 'function') {
//                        AppHelper.showToast('Please select a carrier before marking ready', 'error');
//                    } else {
//                        alert('Please select a carrier before marking ready');
//                    }
//                    return;
//                }
//                // Use centralized ShipmentService.markReadyShipment()
//                // This method handles: save form data, call ready API, redirect with success notification
//                if (window.ShipmentService && typeof ShipmentService.markReadyShipment === 'function') {
//                    ShipmentService.markReadyShipment(id, {
//                        button: $btn[0],
//                        saveFirst: true,
//                        carrierId: carrierId,
//                        redirect: true,
//                        redirectUrl: '/admin/Shipments/List?ready=1&readyId=' + encodeURIComponent(id)
//                    })
//                        .catch(err => {
//                            //console.error('markReadyShipment failed', err);
//                            // Error already shown by ShipmentService
//                        });
//                } else {
//                    //console.error('ShipmentService.markReadyShipment not available');
//                    alert('Unable to mark shipment ready - service not loaded');
//                }
//            });
//        };
//        // Bind to both button variants
//        bindButton('#btnMarkReady');
//        bindButton('#btnMakeReady');
//    });
//})();
//# sourceMappingURL=MarkReady.adminActions.js.map