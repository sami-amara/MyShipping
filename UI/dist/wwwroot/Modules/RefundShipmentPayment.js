var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
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
        errorTitle: alerts.cancelErrorTitle || 'Cancellation Error',
        successTitle: alerts.cancelSuccessTitle || 'Cancel Shipment',
        successMessage: alerts.cancelSuccessMessage || 'Shipment cancelled and payment refunded successfully.',
        missingId: alerts.cancelMissingId || 'Shipment ID is missing.',
        reasonRequired: alerts.cancelReasonRequired || 'Cancellation reason is required.'
    };
    function refundShipmentPayment() {
        return __awaiter(this, void 0, void 0, function* () {
            var _a, _b;
            const shipmentId = shipmentIdInput.value;
            const reason = (_a = reasonInput.value) === null || _a === void 0 ? void 0 : _a.trim();
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
                const data = yield ApiClient.postJson('api/Payment/Refund', {
                    shipmentId,
                    reason
                }, true);
                if (window.showAlert) {
                    window.showAlert.Success(t.successTitle, data.message || t.successMessage);
                }
                setTimeout(() => {
                    window.location.href = listUrl;
                }, 1200);
            }
            catch (error) {
                const errorMessage = ((_b = error === null || error === void 0 ? void 0 : error.responseJSON) === null || _b === void 0 ? void 0 : _b.error) || (error === null || error === void 0 ? void 0 : error.statusText) || (error === null || error === void 0 ? void 0 : error.message) || t.errorTitle;
                if (window.showAlert) {
                    window.showAlert.Error(t.errorTitle, errorMessage);
                }
            }
            finally {
                refundButton.disabled = false;
            }
        });
    }
    refundButton.addEventListener('click', refundShipmentPayment);
}());
//# sourceMappingURL=RefundShipmentPayment.js.map