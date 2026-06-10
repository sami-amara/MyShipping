/* eslint-disable no-undef */

// List.js: Handles shipment list interactions (delete, success notifications, row highlighting)
(function () {
    'use strict';

    $(document).ready(function () {
        const alerts = window.AppResourceAlerts || {};

        // ═══════════════════════════════════════════════════════════════
        // 1. SUCCESS NOTIFICATION (after approve/ready/shipped redirect)
        // ═══════════════════════════════════════════════════════════════
        const showSuccessNotification = () => {
            try {
                const params = new URLSearchParams(window.location.search);

                // Check for various success flags
                const approved = params.get('approved') === '1';
                const ready = params.get('ready') === '1';
                const shipped = params.get('shipped') === '1';
                const delivered = params.get('delivered') === '1';
                const cancelled = params.get('cancelled') === '1';
                const returned = params.get('returned') === '1';
                const deleted = params.get('deleted') === '1';
                const updated = params.get('updated') === '1';

                if (!approved && !ready && !shipped && !delivered && !cancelled && !returned && !deleted && !updated) return;

                // Get the ID for row highlighting
                const itemId = params.get('approvedId')
                    || params.get('readyId')
                    || params.get('shippedId')
                    || params.get('deliveredId')
                    || params.get('cancelledId')
                    || params.get('returnedId')
                    || params.get('deletedId')
                    || params.get('updatedId');

                // Determine message
                let title = alerts.updatedTitle || 'Success';
                let message = alerts.updatedSuccess || 'Operation completed successfully';
                if (approved) message = alerts.approvedSuccess || 'Shipment approved successfully';
                else if (ready) message = alerts.readySuccess || 'Shipment marked as ready for shipping successfully';
                else if (shipped) message = alerts.shippedSuccess || 'Shipment marked as shipped successfully';
                else if (delivered) message = alerts.deliveredSuccess || 'Shipment marked as delivered successfully';
                else if (cancelled) message = alerts.cancelledSuccess || 'Shipment cancelled successfully';
                else if (returned) message = alerts.returnedSuccess || 'Shipment marked as returned successfully';
                else if (deleted) {
                    title = alerts.deletedTitle || 'Deleted';
                    message = alerts.deletedSuccess || 'Shipment deleted successfully';
                }

                // Show notification
                let notified = false;
                if (window.showAlert && typeof showAlert.Success === 'function') {
                    try {
                        showAlert.Success(title, message);
                        notified = true;
                    } catch (e) {
                        console.warn('showAlert.Success failed', e);
                    }
                }

                if (!notified && window.AppHelper && typeof AppHelper.showToast === 'function') {
                    try {
                        AppHelper.showToast(message, 'success');
                        notified = true;
                    } catch (e) {
                        console.warn('AppHelper.showToast failed', e);
                    }
                }

                // Fallback: inline alert
                if (!notified) {
                    try {
                        const container = document.querySelector('.card-body');
                        if (container) {
                            const alertDiv = document.createElement('div');
                            alertDiv.className = 'alert alert-success alert-dismissible fade show';
                            alertDiv.role = 'alert';
                            alertDiv.innerHTML = `
                                ${message}
                                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
                            `;
                            container.insertBefore(alertDiv, container.firstChild);
                        } else {
                            alert(message);
                        }
                    } catch (e) {
                        try { alert(message); } catch { }
                    }
                }

                // Highlight row if ID provided
                if (itemId) {
                    try {
                        const row = document.querySelector(`tr[data-id="${itemId}"]`);
                        if (row) {
                          row.scrollIntoView({ behavior: 'smooth', block: 'center' });
                          row.classList.add('flash-approved');
                          setTimeout(() => {
                            try { row.classList.remove('flash-approved'); } catch { }
                          }, 3000);
                        }
                    } catch (e) {
                        console.warn('Row highlighting failed', e);
                    }
                }

                // Clean URL
                try {
                    const url = new URL(window.location.href);
                    ['approved', 'approvedId', 'ready', 'readyId', 'shipped', 'shippedId',
                        'delivered', 'deliveredId', 'cancelled', 'cancelledId',
                        'returned', 'returnedId', 'deleted', 'deletedId', 'updated', 'updatedId'].forEach(k => url.searchParams.delete(k));
                    window.history.replaceState({}, document.title, url.pathname + url.search);
                } catch (e) { }

            } catch (e) {
                console.error('Success notification failed', e);
            }
        };

        showSuccessNotification();
        // Replace the DELETE SHIPMENT section with this:

        // ═══════════════════════════════════════════════════════════════
        // 2. DELETE SHIPMENT (Admin only - use adminActionsMinimal with state 0)
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
                            redirectUrl: `/admin/Shipments/List?deleted=1&deletedId=${encodeURIComponent(id)}`
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
                        redirectUrl: `/admin/Shipments/List?deleted=1&deletedId=${encodeURIComponent(id)}`
                    })
                    .catch(function (err) {
                        console.error('Delete failed:', err);
                        btn.prop('disabled', false);
                    });
                }
            }

        });

        // ═══════════════════════════════════════════════════════════════
        // 3. CANCEL SHIPMENT FROM SHOW VIEW (same confirmation style as delete)
        // ═══════════════════════════════════════════════════════════════
        function handleCancelShipmentClick(evt, element) {
            evt.preventDefault();

            const cancelUrl = element.getAttribute('href');
            if (!cancelUrl) {
                if (window.showAlert && typeof showAlert.Error === 'function') {
                    showAlert.Error((alerts.deleteFailedTitle || 'Error'), (alerts.missingShipmentId || 'Missing shipment id'));
                } else {
                    alert(alerts.missingShipmentId || 'Missing shipment id');
                }
                return;
            }

            if (window.showAlert && typeof showAlert.ConfirmDelete === 'function') {
                showAlert.ConfirmDelete(function () {
                    window.location.href = cancelUrl;
                });
            } else if (confirm(alerts.confirmDelete || 'Are you sure?')) {
                window.location.href = cancelUrl;
            }
        }

        const btnCancelShipment = document.getElementById('btnCancelShipment');
        if (btnCancelShipment) {
            btnCancelShipment.addEventListener('click', function (evt) {
                handleCancelShipmentClick(evt, btnCancelShipment);
            });
        }

        $(document).on('click', '.btn-cancel-shipment', function (evt) {
            handleCancelShipmentClick(evt, this);
        });

        // ═══════════════════════════════════════════════════════════════
        // 4. DELETE SHIPMENT FROM SHOW VIEW (use adminActionsMinimal with state 0)
        // ═══════════════════════════════════════════════════════════════
        const btnDeleteShipment = document.getElementById('btnDeleteShipment');
        
        if (btnDeleteShipment) {
            btnDeleteShipment.addEventListener('click', function (evt) {
                evt.preventDefault();
                
                // ✅ Get ID from hidden input or from button data attribute
                const shipmentIdField = document.getElementById('shipmentId');
                const id = (shipmentIdField && shipmentIdField.value) || btnDeleteShipment.getAttribute('data-id') || null;
                
                if (!id) {
                    if (window.showAlert && typeof showAlert.Error === 'function') {
                        showAlert.Error((alerts.deleteFailedTitle || 'Error'), (alerts.missingShipmentId || 'Missing shipment id'));
                    } else {
                        alert(alerts.missingShipmentId || 'Missing shipment id');
                    }
                    console.error('Delete handler: Could not find shipment ID');
                    return;
                }

                console.log('Delete button clicked for shipment ID:', id);

                // ✅ Use showAlert.ConfirmDelete for confirmation
                if (window.showAlert && typeof showAlert.ConfirmDelete === 'function') {
                    showAlert.ConfirmDelete(function () {
                        console.log('Deletion confirmed for ID:', id);
                        // Use adminActionsMinimal with state 0 (Deleted)
                        if (window.ShipmentService && typeof ShipmentService.adminActionsMinimal === 'function') {
                            ShipmentService.adminActionsMinimal(id, {
                                button: btnDeleteShipment,
                                targetState: 0,  // Deleted state
                                redirect: true,
                                redirectUrl: '/admin/Shipments/List?deleted=1&deletedId=' + encodeURIComponent(id)
                            })
                            .catch(err => {
                                const message = err && (err.message || err.responseJSON?.message || err.responseJSON?.Message) || (alerts.deleteFailed || 'Failed to delete shipment.');
                                if (window.showAlert && typeof showAlert.Error === 'function') {
                                    showAlert.Error((alerts.deleteFailedTitle || 'Delete Failed'), message);
                                } else {
                                    alert(message);
                                }
                            });
                        } else {
                            console.error('ShipmentService.adminActionsMinimal not available');
                            if (window.showAlert && typeof showAlert.Error === 'function') {
                                showAlert.Error((alerts.deleteFailedTitle || 'Error'), (alerts.deleteUnavailable || 'Delete functionality not available'));
                            } else {
                                alert(alerts.deleteUnavailable || 'Delete functionality not available');
                            }
                        }
                    });
                } else {
                    console.error('ShipmentService.adminActionsMinimal not available');
                }
            });
        } else {
            console.debug('btnDeleteShipment not found on page (not on show view)');
        }

    });
})();

















































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































