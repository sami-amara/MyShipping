/* eslint-disable no-undef */
// Edit.ChangeStatus.Alert.js: Handles Edit/Update via the generic ChangeStatus endpoint (full DTO) and shows success alert
(function () {
    'use strict';
    $(document).ready(function () {
        const formSelector = '#createShipmentForm';
        const validationRules = window.ShipmentValidationRules;
        // Remove any previously attached handlers
        try {
            if (window.jQuery) {
                $(formSelector).off('submit');
                $(formSelector + ' .next').off('click');
                $(formSelector + ' .previous').off('click');
                $(formSelector + ' input[name="btnPost"], button[name="btnPost"]').off('click');
            }
        }
        catch (e) { /* ignore */ }
        // Attach validation
        try {
            if (window.FormValidator) {
                FormValidator.attach(formSelector, validationRules);
                FormValidator.enableStepValidation(formSelector, validationRules);
            }
        }
        catch (e) {
            console.warn('Edit.ChangeStatus.Alert.js: FormValidator attach failed', e);
        }
        // Load shipment data into the form if available
        if (window.ShipmentService && typeof ShipmentService.loadShipmentData === 'function' && window.ShipmentData) {
            ShipmentService.loadShipmentData(window.ShipmentData);
        }
        // Submit handler: validate and call ShipmentService.adminActions (ChangeStatus)
        $(formSelector).on('submit', function (e) {
            var _a, _b, _c, _d;
            e.preventDefault();
            const form = this;
            // run client validation (prefer FormValidator)
            try {
                const ok = (window.FormValidator && typeof FormValidator.validate === 'function')
                    ? FormValidator.validate(form, validationRules)
                    : true;
                if (!ok) {
                    if (typeof (FormValidator === null || FormValidator === void 0 ? void 0 : FormValidator.showStep) === 'function') {
                        const first = form.querySelector('.field-error, .is-invalid');
                        if (first) {
                            const fs = first.closest('fieldset');
                            const idx = parseInt((fs === null || fs === void 0 ? void 0 : fs.getAttribute('data-step')) || '0');
                            if (!isNaN(idx))
                                FormValidator.showStep(idx);
                        }
                    }
                    return;
                }
            }
            catch (ex) {
                console.error('Edit.ChangeStatus.Alert: client validation failed', ex);
            }
            // Use adminActions with action: 'changeStatus' and targetState: 2 (Updated)
            if (window.ShipmentService && typeof ShipmentService.adminActions === 'function') {
                const id = (_a = form.querySelector('[name="Id"]')) === null || _a === void 0 ? void 0 : _a.value;
                const updatedState = (typeof Business !== 'undefined' && ((_d = (_c = (_b = Business.Services) === null || _b === void 0 ? void 0 : _b.Shipment) === null || _c === void 0 ? void 0 : _c.ShipmentStatusEnum) === null || _d === void 0 ? void 0 : _d.Updated))
                    ? Business.Services.Shipment.ShipmentStatusEnum.Updated : 2;
                ShipmentService.adminActions(id, {
                    action: 'changeStatus',
                    targetState: updatedState,
                    button: form.querySelector('input[name="btnPost"], button[name="btnPost"], .btn-submit-final'),
                    redirect: true,
                    redirectUrl: '/admin/Shipments/List?updated=1&updatedId=' + encodeURIComponent(id)
                }).then(function () {
                    // Show success alert after redirect
                    // (AdminListShipments.js will show the alert if ?updated=1 is in the URL)
                    // If not redirected, show here
                    if (window.showAlert && typeof showAlert.Success === 'function') {
                        showAlert.Success('Success', 'Shipment updated successfully');
                    }
                    else if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                        AppHelper.showToast('Shipment updated successfully', 'success');
                    }
                    else {
                        alert('Shipment updated successfully');
                    }
                }).catch(err => {
                    var _a, _b;
                    const message = err && (err.message || ((_a = err.responseJSON) === null || _a === void 0 ? void 0 : _a.message) || ((_b = err.responseJSON) === null || _b === void 0 ? void 0 : _b.Message)) || 'Failed to update shipment.';
                    if (window.showAlert && typeof showAlert.Error === 'function')
                        showAlert.Error('Error', message);
                    else
                        alert(message);
                });
                return;
            }
            // fallback: allow native POST
            form.submit();
        });
    });
})();
//# sourceMappingURL=Edit.ChangeStatus.Alert.js.map