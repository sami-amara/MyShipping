/*
 * Account auth forms bootstrap.
 * Attaches shared validation rules and optional rate-limit countdown for login/register pages.
 */
(function () {
    function initAuthForm() {
        var config = document.getElementById('auth-form-config');
        if (!config) {
            return;
        }
        var formSelector = config.dataset.formSelector;
        var ruleKey = config.dataset.ruleKey;
        if (formSelector && ruleKey && typeof FormValidator !== 'undefined' && typeof Credentials !== 'undefined') {
            var rules = Credentials[ruleKey];
            if (rules) {
                FormValidator.attach(formSelector, rules);
            }
        }
        if (config.dataset.rateLimitExceeded === 'true' && typeof RateLimitCountdown !== 'undefined') {
            RateLimitCountdown.start(parseInt(config.dataset.retryAfterSeconds || '0', 10), 'countdown', 'timeRemaining', 'rateLimitAlert', 'rateLimitProgress');
        }
    }
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAuthForm);
    }
    else {
        initAuthForm();
    }
})();
//# sourceMappingURL=account-auth-forms.js.map