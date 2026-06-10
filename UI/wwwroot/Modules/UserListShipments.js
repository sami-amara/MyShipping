/* eslint-disable no-undef */

// UserListShipments.js: Handles user-facing shipment list (delete functionality using adminActionsMinimal)

(function () {
    'use strict';

    $(document).ready(function () {
        console.log('🟡 UserListShipments jQuery ready fired');

        const alerts = window.AppResourceAlerts || {};
        const params = new URLSearchParams(window.location.search);

        function showSuccessAlert(title, message) {
            if (window.showAlert && typeof showAlert.Success === 'function') {
                showAlert.Success(title, message);
            } else if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                AppHelper.showToast(message, 'success');
            }
        }

        function cleanUrl() {
            if (window.history && window.history.replaceState) {
                window.history.replaceState({}, document.title, window.location.pathname);
            }
        }

        if (params.get('created') === '1') {
            showSuccessAlert(
                alerts.createdTitle || 'Shipment Created',
                alerts.createdSuccess || 'Shipment created successfully'
            );
            cleanUrl();
        }

        if (params.get('updated') === '1') {
            showSuccessAlert(
                alerts.updatedTitle || 'Updated',
                alerts.updatedSuccess || 'Shipment updated successfully'
            );
            cleanUrl();
        }

        if (params.get('deleted') === '1') {
            showSuccessAlert(
                alerts.deletedTitle || 'Deleted',
                alerts.deletedSuccess || 'Shipment deleted'
            );
            cleanUrl();
        }

        // ═══════════════════════════════════════════════════════════════
        // DELETE SHIPMENT - Uses ShipmentService.adminActionsMinimal
        // ═══════════════════════════════════════════════════════════════

        $(document).on('click', '.btn-delete', function (e) {
            e.preventDefault();

            const btn = $(this);
            const id = btn.data('id');

            if (!id) {
                if (window.showAlert && typeof showAlert.Error === 'function') {
                    showAlert.Error((alerts.deleteFailedTitle || 'Error'), (alerts.missingShipmentId || 'Missing shipment ID'));
                } else {
                    alert(alerts.missingShipmentId || 'Missing shipment ID');
                }
                return;
            }

            // ✅ Use showAlert.ConfirmDelete for confirmation
            if (window.showAlert && typeof showAlert.ConfirmDelete === 'function') {
                showAlert.ConfirmDelete(function () {
                    btn.prop('disabled', true);

                    // ✅ Use ShipmentService.adminActionsMinimal with targetState=0 (Deleted)
                    if (window.ShipmentService && typeof ShipmentService.adminActionsMinimal === 'function') {
                        ShipmentService.adminActionsMinimal(id, {
                            button: btn[0],
                            targetState: 0,  // Deleted state
                            redirect: true,
                            redirectUrl: `/Shipments/List?deleted=1&deletedId=${encodeURIComponent(id)}`
                        })
                        .then(function (result) {
                            console.log('✅ Shipment deleted successfully via adminActionsMinimal');
                        })
                        .catch(function (err) {
                            console.error('❌ Delete failed:', err);
                            btn.prop('disabled', false);
                            const errMsg = err && (err.message || err.responseJSON?.message || err.responseJSON?.Message) || (alerts.deleteFailed || 'Unknown error');
                            if (window.showAlert && typeof showAlert.Error === 'function') {
                                showAlert.Error((alerts.deleteFailedTitle || 'Delete Failed'), errMsg);
                            } else {
                                alert((alerts.deleteFailed || 'Failed to delete shipment') + ': ' + errMsg);
                            }
                        });
                    } else {
                        console.error('ShipmentService.adminActionsMinimal not available');
                        btn.prop('disabled', false);
                        if (window.showAlert && typeof showAlert.Error === 'function') {
                            showAlert.Error((alerts.deleteFailedTitle || 'Error'), (alerts.deleteUnavailable || 'Delete functionality not available'));
                        } else {
                            alert(alerts.deleteUnavailable || 'Delete functionality not available');
                        }
                    }
                });
            } else {
                // Fallback to native confirm if showAlert not available
                if (!confirm(alerts.confirmDelete || 'Are you sure you want to delete this shipment?')) {
                    return;
                }

                btn.prop('disabled', true);

                if (window.ShipmentService && typeof ShipmentService.adminActionsMinimal === 'function') {
                    ShipmentService.adminActionsMinimal(id, {
                        button: btn[0],
                        targetState: 0,
                        redirect: true,
                        redirectUrl: `/Shipments/List?deleted=1&deletedId=${encodeURIComponent(id)}`
                    })
                    .catch(function (err) {
                        console.error('Delete failed:', err);
                        btn.prop('disabled', false);
                    });
                }
            }
        });
    });
})();
