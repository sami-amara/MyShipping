/* eslint-disable no-undef */
// Edit.ChangeStatus.js: Handles Edit/Update via the generic ChangeStatus endpoint (full DTO)
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
        } catch (e) { /* ignore */ }

        // Attach validation
        try {
            if (window.FormValidator) {
                FormValidator.attach(formSelector, validationRules);
                FormValidator.enableStepValidation(formSelector, validationRules);
            }
        } catch (e) {
            console.warn('Edit.ChangeStatus.js: FormValidator attach failed', e);
        }

        // Load shipment data into the form if available
        if (window.ShipmentService && typeof ShipmentService.loadShipmentData === 'function' && window.ShipmentData) {
            ShipmentService.loadShipmentData(window.ShipmentData);
        }

        // Submit handler: validate and call ShipmentService.adminActions (ChangeStatus)
        $(formSelector).on('submit', function (e) {
            e.preventDefault();
            const form = this;

            // run client validation (prefer FormValidator)
            try {
                const ok = (window.FormValidator && typeof FormValidator.validate === 'function')
                    ? FormValidator.validate(form, validationRules)
                    : true;
                if (!ok) {
                    if (typeof FormValidator?.showStep === 'function') {
                        const first = form.querySelector('.field-error, .is-invalid');
                        if (first) {
                            const fs = first.closest('fieldset');
                            const idx = parseInt(fs?.getAttribute('data-step') || '0');
                            if (!isNaN(idx)) FormValidator.showStep(idx);
                        }
                    }
                    return;
                }
            } catch (ex) {
                console.error('Edit.ChangeStatus: client validation failed', ex);
            }

            // Use adminActions with action: 'changeStatus' and targetState: 2 (Updated)
            if (window.ShipmentService && typeof ShipmentService.adminActions === 'function') {
                const id = form.querySelector('[name="Id"]')?.value;
                const updatedState = (typeof Business !== 'undefined' && Business.Services?.Shipment?.ShipmentStatusEnum?.Updated)
                    ? Business.Services.Shipment.ShipmentStatusEnum.Updated : 2;
                ShipmentService.adminActions(id, {
                    action: 'changeStatus',
                    targetState: updatedState,
                    button: form.querySelector('input[name="btnPost"], button[name="btnPost"], .btn-submit-final'),
                    redirect: true,
                    redirectUrl: '/admin/Shipments/List?updated=1&updatedId=' + encodeURIComponent(id)
                }).catch(err => {
                    const message = err && (err.message || err.responseJSON?.message || err.responseJSON?.Message) || 'Failed to update shipment.';
                    if (window.showAlert && typeof showAlert.Error === 'function') showAlert.Error('Error', message);
                    else alert(message);
                });
                return;
            }

            // fallback: allow native POST
            form.submit();
        });
    });
})();
