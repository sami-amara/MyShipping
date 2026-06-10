/* eslint-disable no-undef */
// UpdateStatus.js: Handles Delivered=6, Cancelled=7, Returned=8, Deleted=0, Shipped=5
// Reads targetState from data-target-state attribute on the button
// Calls ShipmentService.adminActionsMinimal() → ShipmentApiClient.updateStatus()
// → POST /api/Shipments/{id}/UpdateStatus?newState=N
(function () {
    'use strict';
    $(document).ready(function () {
        const alerts = window.AppResourceAlerts || {};
        // ═══════════════════════════════════════════════════════════════
        // BUTTON HANDLER — binds any button with data-target-state attr
        // ═══════════════════════════════════════════════════════════════
        const bindButton = function (btnSelector) {
            const btn = document.querySelector(btnSelector);
            if (!btn)
                return;
            btn.addEventListener('click', function (e) {
                var _a;
                e.preventDefault();
                const id = (_a = (document.querySelector('[name="Id"]') || document.querySelector('input[name="Id"]'))) === null || _a === void 0 ? void 0 : _a.value;
                if (!id) {
                    if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                        AppHelper.showToast((alerts.missingShipmentId || 'Missing shipment ID'), 'error');
                    }
                    else {
                        alert(alerts.missingShipmentId || 'Missing shipment ID');
                    }
                    return;
                }
                const targetState = parseInt(btn.getAttribute('data-target-state'), 10);
                if (isNaN(targetState)) {
                    if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                        AppHelper.showToast((alerts.missingTargetState || 'Button is missing data-target-state attribute'), 'error');
                    }
                    else {
                        alert(alerts.missingTargetState || 'Button is missing data-target-state attribute');
                    }
                    return;
                }
                // Map state to redirect query param
                const redirectMap = { 0: 'deleted', 5: 'shipped', 6: 'delivered', 7: 'cancelled', 8: 'returned' };
                const paramName = redirectMap[targetState] || 'updated';
                const redirectUrl = '/admin/Shipments/List?' + paramName + '=1&' + paramName + 'Id=' + encodeURIComponent(id);
                if (window.ShipmentService && typeof ShipmentService.adminActionsMinimal === 'function') {
                    ShipmentService.adminActionsMinimal(id, {
                        targetState: targetState,
                        button: btn,
                        redirect: true,
                        redirectUrl: redirectUrl
                    }).catch(function (err) {
                        // console.error('UpdateStatus.js: adminActionsMinimal failed', err.status, err.responseText);
                        var msg = (err && (err.message || (err.responseJSON && err.responseJSON.title))) || (alerts.operationFailed || 'Operation failed');
                        if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                            AppHelper.showToast(msg, 'error');
                        }
                        else {
                            alert(msg);
                        }
                    });
                }
                else {
                    console.error('ShipmentService.adminActionsMinimal not available');
                    alert(alerts.serviceNotLoaded || 'Service not loaded');
                }
            });
        };
        // Bind each status button by its ID
        bindButton('#btnMarkShipped'); // Shipped    = 5  (generic UpdateStatus endpoint)
        bindButton('#btnDeliver'); // Delivered  = 6
        bindButton('#btnCancel'); // Cancelled  = 7
        bindButton('#btnReturn'); // Returned   = 8
        bindButton('#btnDelete'); // Deleted    = 0
    });
})();
//# sourceMappingURL=UpdateStatus.js.map