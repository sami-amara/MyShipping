/* eslint-disable no-undef */
(function () {
    'use strict';

    const refundButton = document.getElementById('btnRefundPayment');
    const shipmentIdInput = document.getElementById('shipmentId');
    const reasonInput = document.getElementById('reason');
    const listUrlInput = document.getElementById('listUrl');

    if (!refundButton || !shipmentIdInput || !reasonInput || !listUrlInput) {
        return;
    }

    // Read localized strings from AppResourceAlerts injected by _ClientResourceScripts.cshtml
    const alerts = window.AppResourceAlerts || {};
    const t = {
        errorTitle:      alerts.cancelErrorTitle      || 'Cancellation Error',
        successTitle:    alerts.cancelSuccessTitle    || 'Cancel Shipment',
        successMessage:  alerts.cancelSuccessMessage  || 'Shipment cancelled and payment refunded successfully.',
        missingId:       alerts.cancelMissingId       || 'Shipment ID is missing.',
        reasonRequired:  alerts.cancelReasonRequired  || 'Cancellation reason is required.'
    };

    async function refundShipmentPayment() {
        const shipmentId = shipmentIdInput.value;
        const reason = reasonInput.value?.trim();
        const listUrl = listUrlInput.value;

        if (!shipmentId) {
            if (window.showAlert) {
                window.showAlert.Error(t.errorTitle, t.missingId);
            }
            return;
        }

        if (!reason) {
            if (window.showAlert) {
                window.showAlert.Error(t.errorTitle, t.reasonRequired);
            }
            return;
        }

        refundButton.disabled = true;

        try {
            const data = await ApiClient.postJson('api/Payment/Refund', {
                shipmentId,
                reason
            }, true);

            if (window.showAlert) {
                window.showAlert.Success(t.successTitle, data.message || t.successMessage);
            }

            setTimeout(() => {
                window.location.href = listUrl;
            }, 1200);
        } catch (error) {
            const errorMessage = error?.responseJSON?.error || error?.statusText || error?.message || t.errorTitle;
            if (window.showAlert) {
                window.showAlert.Error(t.errorTitle, errorMessage);
            }
        } finally {
            refundButton.disabled = false;
        }
    }

    refundButton.addEventListener('click', refundShipmentPayment);
}());