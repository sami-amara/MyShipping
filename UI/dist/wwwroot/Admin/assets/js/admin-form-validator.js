/*
 * Admin form validation bootstrap.
 * Reads form selector/rule key from #admin-form-validator-config and attaches FormValidator.
 */
(function () {
    function attachAdminFormValidation() {
        var configElement = document.getElementById('admin-form-validator-config');
        if (!configElement || typeof FormValidator === 'undefined' || !FormValidator.attach) {
            return;
        }
        var formSelector = configElement.dataset.formSelector;
        var ruleKey = configElement.dataset.ruleKey;
        if (!formSelector || !ruleKey || typeof AdminValidationRules === 'undefined') {
            return;
        }
        var rules = AdminValidationRules[ruleKey];
        if (!rules) {
            return;
        }
        FormValidator.attach(formSelector, rules);
    }
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', attachAdminFormValidation);
    }
    else {
        attachAdminFormValidation();
    }
})();
//# sourceMappingURL=admin-form-validator.js.map