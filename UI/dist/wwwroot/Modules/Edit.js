/* eslint-disable no-undef */
// Edit.ChangeStatus.js: Handles Edit/Update via the generic ChangeStatus endpoint (full DTO)
(function () {
    'use strict';
    $(document).ready(function () {
        const formSelector = '#createShipmentForm';
        const validationRules = window.ShipmentValidationRules;
        const localizedValidation = window.ShipmentValidationTexts || {};
        Object.keys(localizedValidation).forEach(function (fieldName) {
            if (validationRules && validationRules[fieldName]) {
                validationRules[fieldName].requiredMessage = localizedValidation[fieldName];
            }
        });
        // Remove any previously attached handlers
        try {
            if (window.jQuery) {
                $(formSelector).off('submit');
                $(formSelector + ' .next').off('click');
                $(formSelector + ' .previous').off('click');
                // $(formSelector + ' input[name="btnPost"], button[name="btnPost"]').off('click');
                $(formSelector + ' #btnSave, ' + formSelector + ' button[name="command"], ' + formSelector + ' input[name="btnPost"], ' + formSelector + ' button[name="btnPost"]').off('click');
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
            console.warn('Edit.ChangeStatus.js: FormValidator attach failed', e);
        }
        // Load shipment data into the form if available
        if (window.ShipmentService && typeof ShipmentService.loadShipmentData === 'function' && window.ShipmentData) {
            ShipmentService.loadShipmentData(window.ShipmentData);
        }
        // Submit handler kept for reference during testing (disabled by request)
        // $(formSelector).on('submit', function (e) {
        //     e.preventDefault();
        //     const form = this;
        //
        //     try {
        //         const ok = (window.FormValidator && typeof FormValidator.validate === 'function')
        //             ? FormValidator.validate(form, validationRules)
        //             : true;
        //         if (!ok) {
        //             if (typeof FormValidator?.showStep === 'function') {
        //                 const first = form.querySelector('.field-error, .is-invalid');
        //                 if (first) {
        //                     const fs = first.closest('fieldset');
        //                     const idx = parseInt(fs?.getAttribute('data-step') || '0');
        //                     if (!isNaN(idx)) FormValidator.showStep(idx);
        //                 }
        //             }
        //             return;
        //         }
        //     } catch (ex) {
        //         console.error('Edit.ChangeStatus: client validation failed', ex);
        //     }
        //
        //     if (window.ShipmentService && typeof ShipmentService.adminActions === 'function') {
        //         const id = form.querySelector('[name="Id"]')?.value;
        //         const updatedState = (typeof Business !== 'undefined' && Business.Services?.Shipment?.ShipmentStatusEnum?.Updated)
        //             ? Business.Services.Shipment.ShipmentStatusEnum.Updated : 2;
        //         ShipmentService.adminActions(id, {
        //             action: 'changeStatus',
        //             targetState: updatedState,
        //             button: form.querySelector('#btnSave, button[name="command"], input[name="btnPost"], button[name="btnPost"], .btn-submit-final'),
        //             redirect: true,
        //             redirectUrl: '/Shipments/List?updated=1&updatedId=' + encodeURIComponent(id)
        //         }).catch(err => {
        //             const message = err && (err.message || err.responseJSON?.message || err.responseJSON?.Message) || 'Failed to update shipment.';
        //             if (window.showAlert && typeof showAlert.Error === 'function') showAlert.Error('Error', message);
        //             else alert(message);
        //         });
        //         return;
        //     }
        //
        //     form.submit();
        // });
        // Active flow: bind Save click directly (similar to ChangeStatus.js pattern)
        $(formSelector + ' #btnSave, ' + formSelector + ' button[name="command"]').on('click', function (e) {
            var _a, _b, _c, _d;
            e.preventDefault();
            const form = document.querySelector(formSelector);
            if (!form)
                return;
            if (form._submitting)
                return;
            form._submitting = true;
            const btn = this;
            try {
                btn.disabled = true;
            }
            catch (x) { }
            const cleanup = function () {
                form._submitting = false;
                try {
                    btn.disabled = false;
                }
                catch (x) { }
            };
            const id = (_a = form.querySelector('[name="Id"]')) === null || _a === void 0 ? void 0 : _a.value;
            const updatedState = (typeof Business !== 'undefined' && ((_d = (_c = (_b = Business.Services) === null || _b === void 0 ? void 0 : _b.Shipment) === null || _c === void 0 ? void 0 : _c.ShipmentStatusEnum) === null || _d === void 0 ? void 0 : _d.Updated))
                ? Business.Services.Shipment.ShipmentStatusEnum.Updated : 2;
            if (window.ShipmentService && typeof ShipmentService.adminActions === 'function') {
                ShipmentService.adminActions(id, {
                    action: 'changeStatus',
                    targetState: updatedState,
                    button: btn,
                    redirect: true,
                    redirectUrl: '/Shipments/List?updated=1&updatedId=' + encodeURIComponent(id)
                }).then(function () {
                    cleanup();
                }).catch(function (err) {
                    var _a, _b;
                    cleanup();
                    const message = err && (err.message || ((_a = err.responseJSON) === null || _a === void 0 ? void 0 : _a.message) || ((_b = err.responseJSON) === null || _b === void 0 ? void 0 : _b.Message)) || 'Failed to update shipment.';
                    if (window.showAlert && typeof showAlert.Error === 'function')
                        showAlert.Error('Error', message);
                    else
                        alert(message);
                });
                return;
            }
            // Fallback: bypass other submit handlers and post to MVC action directly
            try {
                HTMLFormElement.prototype.submit.call(form);
            }
            finally {
                cleanup();
            }
        });
    });
})();
/* eslint-disable no-undef */
// (function () {
//    'use strict';
//    $(document).ready(function () {
//        const formSelector = '#createShipmentForm';
//        const validationRules = window.ShipmentValidationRules;
//        const localizedValidation = window.ShipmentValidationTexts || {};
//        Object.keys(localizedValidation).forEach(function (fieldName) {
//            if (validationRules && validationRules[fieldName]) {
//                validationRules[fieldName].requiredMessage = localizedValidation[fieldName];
//            }
//        });
//        // Remove any previously attached handlers (from old Edit/Create scripts)
//        try {
//            if (window.jQuery) {
//                $(formSelector).off('submit');
//                $(formSelector + ' .next').off('click');
//                $(formSelector + ' .previous').off('click');
//                $(formSelector + ' input[name="btnPost"], button[name="btnPost"]').off('click');
//            }
//            if (window.ShipmentService && typeof ShipmentService._boundSubmitHandler === 'function') {
//                try {
//                    const btnsel = (ShipmentService.settings && ShipmentService.settings.submitButtonSelector)
//                        || 'input[name="btnPost"], button[name="btnPost"], .btn-submit-final';
//                    document.querySelectorAll(btnsel).forEach(b => {
//                        try { b.removeEventListener('click', ShipmentService._boundSubmitHandler); } catch (e) { }
//                    });
//                    const form = document.querySelector(formSelector);
//                    if (form) try { form.removeEventListener('submit', ShipmentService._boundSubmitHandler); } catch (e) { }
//                    ShipmentService._boundSubmitHandler = null;
//                } catch (e) { /* ignore */ }
//            }
//        } catch (e) { /* ignore */ }
//        try {
//            if (window.FormValidator) {
//                FormValidator.attach(formSelector, validationRules);
//                FormValidator.enableStepValidation(formSelector, validationRules);
//            }
//        } catch (e) {
//            console.warn('Edit.js: FormValidator attach failed', e);
//        }
//        // Load shipment data into the form if available
//        if (window.ShipmentService && typeof ShipmentService.loadShipmentData === 'function' && window.ShipmentData) {
//            ShipmentService.loadShipmentData(window.ShipmentData);
//        }
//        // Submit handler: validate and call ShipmentService.update (or fallback to native POST)
//        $(formSelector).on('submit', function (e) {
//            e.preventDefault();
//            const form = this;
//            // run client validation (prefer FormValidator)
//            try {
//                const ok = (window.FormValidator && typeof FormValidator.validate === 'function')
//                    ? FormValidator.validate(form, validationRules)
//                    : true; // if no FormValidator, allow native client/server validation
//                if (!ok) {
//                    // show first invalid step if available
//                    if (typeof FormValidator?.showStep === 'function') {
//                        const first = form.querySelector('.field-error, .is-invalid');
//                        if (first) {
//                            const fs = first.closest('fieldset');
//                            const idx = parseInt(fs?.getAttribute('data-step') || '0');
//                            if (!isNaN(idx)) FormValidator.showStep(idx);
//                        }
//                    }
//                    return;
//                }
//            } catch (ex) {
//                console.error('Edit: client validation failed', ex);
//                // continue and let server validate
//            }
//            // call AJAX update if present
//            if (window.ShipmentService && typeof ShipmentService.update === 'function') {
//                ShipmentService.update();
//                return;
//            }
//            // no AJAX client available - allow native POST to controller Edit action
//            form.submit();
//        });
//    });
// })();
//# sourceMappingURL=Edit.js.map