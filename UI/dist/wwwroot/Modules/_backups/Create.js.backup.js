// Backup of UI/wwwroot/Modules/Create.js (current state)
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
            var _a;
            // Use centralized mapping helper when available
            const mapped = (window.ClientHelpers && typeof ClientHelpers.mapServerErrors === 'function')
                ? ClientHelpers.mapServerErrors(xhr, form)
                : false;
            if (mapped)
                return; // field errors handled by helper
            const msg = (window.ClientHelpers && typeof ClientHelpers.extractFirstMessage === 'function')
                ? ClientHelpers.extractFirstMessage(xhr)
                : (((_a = xhr === null || xhr === void 0 ? void 0 : xhr.responseJSON) === null || _a === void 0 ? void 0 : _a.message) || (xhr === null || xhr === void 0 ? void 0 : xhr.message) || 'Failed to create shipment.');
            if (window.AppHelper && typeof AppHelper.showToast === 'function')
                AppHelper.showToast(msg || 'Failed to create shipment.', 'error');
        });
    });
});
//# sourceMappingURL=Create.js.backup.js.map