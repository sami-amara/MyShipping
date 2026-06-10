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
        }
        catch (e) { /* ignore */ }
        try {
            if (window.FormValidator) {
                FormValidator.attach(formSelector, validationRules);
                FormValidator.enableStepValidation(formSelector, validationRules);
            }
        }
        catch (e) {
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
                    if (typeof (FormValidator === null || FormValidator === void 0 ? void 0 : FormValidator.showStep) === 'function') {
                        const first = form.querySelector('.field-error, .is-invalid, .input-validation-error, [aria-invalid="true"], :invalid');
                        if (first) {
                            const fs = first.closest('fieldset');
                            const idx = parseInt((fs === null || fs === void 0 ? void 0 : fs.getAttribute('data-step')) || '0', 10);
                            if (!isNaN(idx))
                                FormValidator.showStep(idx);
                            setTimeout(function () {
                                if (typeof first.focus === 'function')
                                    first.focus();
                            }, 0);
                        }
                    }
                    return;
                }
            }
            catch (ex) {
                console.error('Create: client validation failed', ex);
                return;
            }
            if (window.ShipmentService && typeof ShipmentService.submitShipment === 'function') {
                ShipmentService.submitShipment(null, function (xhr) {
                    var _a, _b, _c, _d, _e, _f, _g, _h;
                    const mapped = (window.ClientHelpers && typeof ClientHelpers.mapServerErrors === 'function')
                        ? ClientHelpers.mapServerErrors(xhr, form)
                        : false;
                    if (mapped) {
                        if (typeof (FormValidator === null || FormValidator === void 0 ? void 0 : FormValidator.showStep) === 'function') {
                            const first = form.querySelector('.field-error, .is-invalid, .input-validation-error, [aria-invalid="true"], :invalid');
                            if (first) {
                                const fs = first.closest('fieldset');
                                const idx = parseInt((fs === null || fs === void 0 ? void 0 : fs.getAttribute('data-step')) || '0', 10);
                                if (!isNaN(idx))
                                    FormValidator.showStep(idx);
                                setTimeout(function () {
                                    if (typeof first.focus === 'function')
                                        first.focus();
                                }, 0);
                            }
                        }
                        return;
                    }
                    const msg = (window.ClientHelpers && typeof ClientHelpers.extractFirstMessage === 'function')
                        ? ClientHelpers.extractFirstMessage(xhr)
                        : (((_a = xhr === null || xhr === void 0 ? void 0 : xhr.responseJSON) === null || _a === void 0 ? void 0 : _a.Message) ||
                            ((_b = xhr === null || xhr === void 0 ? void 0 : xhr.responseJSON) === null || _b === void 0 ? void 0 : _b.message) ||
                            ((_e = (_d = (_c = xhr === null || xhr === void 0 ? void 0 : xhr.responseJSON) === null || _c === void 0 ? void 0 : _c.Errors) === null || _d === void 0 ? void 0 : _d[0]) === null || _e === void 0 ? void 0 : _e.Description) ||
                            ((_h = (_g = (_f = xhr === null || xhr === void 0 ? void 0 : xhr.responseJSON) === null || _f === void 0 ? void 0 : _f.errors) === null || _g === void 0 ? void 0 : _g[0]) === null || _h === void 0 ? void 0 : _h.description) ||
                            (xhr === null || xhr === void 0 ? void 0 : xhr.message) ||
                            'Failed to create shipment.');
                    if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                        AppHelper.showToast(msg || 'Failed to create shipment.', 'error');
                    }
                });
                return;
            }
            if (window.AppHelper && typeof AppHelper.showToast === 'function') {
                AppHelper.showToast('Shipment service is not available.', 'error');
            }
        });
    });
})();
//# sourceMappingURL=Create.js.map