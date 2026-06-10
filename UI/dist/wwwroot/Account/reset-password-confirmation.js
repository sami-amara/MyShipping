(function () {
    var dataEl = document.getElementById('reset-password-alert-data');
    if (!dataEl)
        return;
    window.ResetPasswordAlertTexts = {
        title: dataEl.dataset.title || '',
        message: dataEl.dataset.message || ''
    };
})();
//# sourceMappingURL=reset-password-confirmation.js.map