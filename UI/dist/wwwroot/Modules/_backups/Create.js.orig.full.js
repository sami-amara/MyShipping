/* eslint-disable no-undef */
// Full backup of Create.js prior to refactor
$(document).ready(function () {
    const formSelector = '#createShipmentForm';
    const validationRules = window.ShipmentValidationRules;
    FormValidator.attach(formSelector, validationRules);
    FormValidator.enableStepValidation(formSelector, validationRules);
    // Explicitly enable ShipmentService submit wiring only on Create page
    try {
        if (window.ShipmentService && typeof ShipmentService.init === 'function') {
            ShipmentService.init({ autoWireSubmit: true });
        }
    }
    catch (e) {
        console.warn('Create.js: ShipmentService.init failed', e);
    }
    $(formSelector).on('submit', function (e) {
        e.preventDefault();
        const form = this;
        const isValid = FormValidator.validate(form, validationRules);
        ShipmentService.submitShipment(null, function (xhr) {
            var _a, _b, _c, _d;
            const errorMap = (_a = xhr.responseJSON) === null || _a === void 0 ? void 0 : _a.errors;
            let hasFieldErrors = false;
            let firstErrorInput = null;
            let firstErrorStep = null;
            if (errorMap && typeof errorMap === 'object') {
                Object.entries(errorMap).forEach(([field, messages]) => {
                    const flatField = field.split('.').pop();
                    const input = form.querySelector(`[name="${flatField}"]`);
                    if (input && Array.isArray(messages) && messages.length > 0) {
                        hasFieldErrors = true;
                        FormValidator.validateField(input, {
                            required: true,
                            requiredMessage: messages[0]
                        });
                        if (!firstErrorInput) {
                            firstErrorInput = input;
                            const fieldset = input.closest('fieldset');
                            const stepIndex = parseInt((fieldset === null || fieldset === void 0 ? void 0 : fieldset.getAttribute('data-step')) || '0');
                            firstErrorStep = stepIndex;
                        }
                    }
                });
                if (hasFieldErrors) {
                    if (typeof FormValidator.showStep === 'function' && firstErrorStep !== null) {
                        FormValidator.showStep(firstErrorStep);
                    }
                    if (firstErrorInput && ((_b = window.ScrollHelper) === null || _b === void 0 ? void 0 : _b.scrollToCenter)) {
                        ScrollHelper.scrollToCenter(firstErrorInput);
                    }
                    return; // Suppress toast if field-level errors are shown
                }
            }
            const fallbackMessage = ((_c = xhr.responseJSON) === null || _c === void 0 ? void 0 : _c.title) || ((_d = xhr.responseJSON) === null || _d === void 0 ? void 0 : _d.message) || 'Failed to create shipment.';
            AppHelper.showToast(fallbackMessage, 'error');
        });
    });
});
//# sourceMappingURL=Create.js.orig.full.js.map