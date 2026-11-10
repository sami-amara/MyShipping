// UI/wwwroot/Modules/UpdateStatus.js
/* eslint-disable no-undef */

// UpdateStatus.js: Handles Delivered=6, Cancelled=7, Returned=8, Deleted=0, Shipped=5
// Reads targetState from data-target-state attribute on the button
// ✅ CAPTURES REASON from textarea with id matching: cancelReason, returnReason, refundReason
// Calls ShipmentService.adminActionsMinimal() → ShipmentApiClient.updateStatus()
// → POST /api/Shipments/{id}/UpdateStatus?newState=N&reason=...
(function () {
    'use strict';

    $(document).ready(function () {
        const alerts = window.AppResourceAlerts || {};

        // ══════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════
        // BUTTON HANDLER — binds any button with data-target-state attr
        // ══════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════
        const bindButton = function (btnSelector) {
            const btn = document.querySelector(btnSelector);
            if (!btn) return;

            btn.addEventListener('click', function (e) {
                e.preventDefault();

                const id = (document.querySelector('[name="Id"]') || document.querySelector('input[name="Id"]'))?.value;
                if (!id) {
                    showAlert('error', alerts.missingShipmentId || 'Missing shipment ID');
                    return;
                }

                const targetState = parseInt(btn.getAttribute('data-target-state'), 10);
                if (isNaN(targetState)) {
                    showAlert('error', alerts.missingTargetState || 'Button is missing data-target-state attribute');
                    return;
                }

                // ✅ NEW: Extract reason from textarea based on targetState
                let reason = null;
                const reasonField = extractReasonField(targetState);

                if (reasonField) {
                    reason = reasonField.value?.trim();

                    // ✅ Validate reason is required for certain states
                    if (!reason && [7, 8, 6].includes(targetState)) {  // Cancel=7, Return=8, maybe Refund
                        showAlert('error', getReasonRequiredMessage(targetState));
                        return;
                    }
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
                        redirectUrl: redirectUrl,
                        reason: reason  // ✅ NEW: Pass reason to adminActionsMinimal
                    }).catch(function (err) {
                        var msg = (err && (err.message || (err.responseJSON && err.responseJSON.title))) || (alerts.operationFailed || 'Operation failed');
                        showAlert('error', msg);
                    });
                } else {
                    console.error('ShipmentService.adminActionsMinimal not available');
                    showAlert('error', alerts.serviceNotLoaded || 'Service not loaded');
                }
            });
        };

        // ✅ Helper: Extract reason field based on targetState
        function extractReasonField(targetState) {
            if (targetState === 7) {
                // Cancelled
                return document.querySelector('#cancelReason') || document.querySelector('[name="CancellationReason"]');
            }
            if (targetState === 8) {
                // Returned
                return document.querySelector('#returnReason') || document.querySelector('[name="ReturnReason"]');
            }
            if (targetState === 6) {
                // Refunded (if applicable)
                return document.querySelector('#refundReason') || document.querySelector('[name="RefundReason"]');
            }
            return null;
        }

        // ✅ Helper: Get localized reason required message
        function getReasonRequiredMessage(targetState) {
            if (targetState === 7) {
                return alerts.cancelReasonRequired || 'Cancellation reason is required.';
            }
            if (targetState === 8) {
                return alerts.returnReasonRequired || 'Return reason is required.';
            }
            if (targetState === 6) {
                return alerts.refundReasonRequired || 'Refund reason is required.';
            }
            return alerts.reasonRequired || 'Reason is required.';
        }

        // ✅ Helper: Show alert using AppHelper or fallback
        function showAlert(type, message) {
            if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                AppHelper.showToast(message, type);
            } else {
                alert((type === 'error' ? '❌ ' : '✅ ') + message);
            }
        }

        // Bind each status button by its ID
        bindButton('#btnMarkShipped'); // Shipped    = 5
        bindButton('#btnDeliver');     // Delivered  = 6
        bindButton('#btnCancel');      // Cancelled  = 7
        bindButton('#btnReturn');      // Returned   = 8
        bindButton('#btnDelete');      // Deleted    = 0
    });
})();










// /* eslint-disable no-undef */

// // UpdateStatus.js: Handles Delivered=6, Cancelled=7, Returned=8, Deleted=0, Shipped=5
// // Reads targetState from data-target-state attribute on the button
// // Calls ShipmentService.adminActionsMinimal() → ShipmentApiClient.updateStatus()
// // → POST /api/Shipments/{id}/UpdateStatus?newState=N
// (function () {
//     'use strict';

//     $(document).ready(function () {
//         const alerts = window.AppResourceAlerts || {};

//         // ═══════════════════════════════════════════════════════════════
//         // BUTTON HANDLER — binds any button with data-target-state attr
//         // ═══════════════════════════════════════════════════════════════
//         const bindButton = function (btnSelector) {
//             const btn = document.querySelector(btnSelector);
//             if (!btn) return;

//             btn.addEventListener('click', function (e) {
//                 e.preventDefault();

//                 const id = (document.querySelector('[name="Id"]') || document.querySelector('input[name="Id"]'))?.value;
//                 if (!id) {
//                     if (window.AppHelper && typeof AppHelper.showToast === 'function') {
//                         AppHelper.showToast((alerts.missingShipmentId || 'Missing shipment ID'), 'error');
//                     } else {
//                         alert(alerts.missingShipmentId || 'Missing shipment ID');
//                     }
//                     return;
//                 }

//                 const targetState = parseInt(btn.getAttribute('data-target-state'), 10);
//                 if (isNaN(targetState)) {
//                     if (window.AppHelper && typeof AppHelper.showToast === 'function') {
//                         AppHelper.showToast((alerts.missingTargetState || 'Button is missing data-target-state attribute'), 'error');
//                     } else {
//                         alert(alerts.missingTargetState || 'Button is missing data-target-state attribute');
//                     }
//                     return;
//                 }

//                 // Map state to redirect query param
//                 const redirectMap = { 0: 'deleted', 5: 'shipped', 6: 'delivered', 7: 'cancelled', 8: 'returned' };
//                 const paramName = redirectMap[targetState] || 'updated';
//                 const redirectUrl = '/admin/Shipments/List?' + paramName + '=1&' + paramName + 'Id=' + encodeURIComponent(id);

//                 if (window.ShipmentService && typeof ShipmentService.adminActionsMinimal === 'function') {
//                     ShipmentService.adminActionsMinimal(id, {
//                         targetState: targetState,
//                         button: btn,
//                         redirect: true,
//                         redirectUrl: redirectUrl
                      
//                     }).catch(function (err) {
//                         // console.error('UpdateStatus.js: adminActionsMinimal failed', err.status, err.responseText);
//                         var msg = (err && (err.message || (err.responseJSON && err.responseJSON.title))) || (alerts.operationFailed || 'Operation failed');
//                         if (window.AppHelper && typeof AppHelper.showToast === 'function') {
//                             AppHelper.showToast(msg, 'error');
//                         } else {
//                             alert(msg);
//                         }
//                     });
//                 } else {
//                     console.error('ShipmentService.adminActionsMinimal not available');
//                     alert(alerts.serviceNotLoaded || 'Service not loaded');
//                 }
//             });
//         };

//         // Bind each status button by its ID
//         bindButton('#btnMarkShipped'); // Shipped    = 5  (generic UpdateStatus endpoint)
//         bindButton('#btnDeliver');     // Delivered  = 6
//         bindButton('#btnCancel');      // Cancelled  = 7
//         bindButton('#btnReturn');      // Returned   = 8
//         bindButton('#btnDelete');      // Deleted    = 0
//     });
// })();
