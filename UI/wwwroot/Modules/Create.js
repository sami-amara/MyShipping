/* eslint-disable no-undef */
(function () {
    'use strict';

    $(document).ready(function () {
        const formSelector = '#createShipmentForm';
        const validationRules = window.ShipmentValidationRules || {};
        const localizedValidation = window.ShipmentValidationTexts || {};

        Object.keys(localizedValidation).forEach(function (fieldName) {
            if (validationRules[fieldName]) {
                validationRules[fieldName].requiredMessage = localizedValidation[fieldName];
            }
        });

        try {
            if (window.jQuery) {
                $(formSelector).off('submit');
                $(formSelector + ' .next').off('click');
                $(formSelector + ' .previous').off('click');
                $(formSelector + ' input[name="btnPost"], button[name="btnPost"]').off('click');
            }
        } catch (e) { /* ignore */ }

        try {
            if (window.FormValidator) {
                FormValidator.attach(formSelector, validationRules);
                FormValidator.enableStepValidation(formSelector, validationRules);
            }
        } catch (e) {
            console.warn('Create.js: FormValidator attach failed', e);
        }

        $(formSelector).on('submit', function (e) {
            e.preventDefault();
            const form = this;

            try {
                const ok = (window.FormValidator && typeof FormValidator.validate === 'function')
                    ? FormValidator.validate(form, validationRules)
                    : true;

                if (!ok) {
                    if (typeof FormValidator?.showStep === 'function') {
                        const first = form.querySelector('.field-error, .is-invalid, .input-validation-error, [aria-invalid="true"], :invalid');
                        if (first) {
                            const fs = first.closest('fieldset');
                            const idx = parseInt(fs?.getAttribute('data-step') || '0', 10);
                            if (!isNaN(idx)) FormValidator.showStep(idx);
                            setTimeout(function () {
                                if (typeof first.focus === 'function') first.focus();
                            }, 0);
                        }
                    }
                    return;
                }
            } catch (ex) {
                console.error('Create: client validation failed', ex);
                return;
            }

            if (window.ShipmentService && typeof ShipmentService.submitShipment === 'function') {
                ShipmentService.submitShipment(
                    null,
                    function (xhr) {
                        const mapped = (window.ClientHelpers && typeof ClientHelpers.mapServerErrors === 'function')
                            ? ClientHelpers.mapServerErrors(xhr, form)
                            : false;

                        if (mapped) {
                            if (typeof FormValidator?.showStep === 'function') {
                                const first = form.querySelector('.field-error, .is-invalid, .input-validation-error, [aria-invalid="true"], :invalid');
                                if (first) {
                                    const fs = first.closest('fieldset');
                                    const idx = parseInt(fs?.getAttribute('data-step') || '0', 10);
                                    if (!isNaN(idx)) FormValidator.showStep(idx);
                                    setTimeout(function () {
                                        if (typeof first.focus === 'function') first.focus();
                                    }, 0);
                                }
                            }
                            return;
                        }

                        const msg = (window.ClientHelpers && typeof ClientHelpers.extractFirstMessage === 'function')
                            ? ClientHelpers.extractFirstMessage(xhr)
                            : (
                                xhr?.responseJSON?.Message ||
                                xhr?.responseJSON?.message ||
                                xhr?.responseJSON?.Errors?.[0]?.Description ||
                                xhr?.responseJSON?.errors?.[0]?.description ||
                                xhr?.message ||
                                'Failed to create shipment.'
                            );

                        if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                            AppHelper.showToast(msg || 'Failed to create shipment.', 'error');
                        }
                    }
                );
                return;
            }

            if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                AppHelper.showToast('Shipment service is not available.', 'error');
            }
        });
    });
})();
