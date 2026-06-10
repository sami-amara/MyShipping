(function () {
    if (window.FormValidator && window.Credentials) {
        FormValidator.attach('#loginForm', Credentials.loginForm);
    }
    var cfg = document.getElementById('login-rate-limit-config');
    if (!cfg || String(cfg.dataset.enabled).toLowerCase() !== 'true') {
        return;
    }
    if (window.RateLimitCountdown && typeof RateLimitCountdown.start === 'function') {
        RateLimitCountdown.start(parseInt(cfg.dataset.seconds || '0', 10), 'countdown', 'timeRemaining', 'rateLimitAlert', 'rateLimitProgress');
    }
})();
//# sourceMappingURL=login-page.js.map