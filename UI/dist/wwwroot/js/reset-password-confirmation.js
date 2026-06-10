/*
 * Reset password confirmation bootstrap.
 * Exposes reset-password success alert texts through hidden JSON markup.
 */
(function () {
    function parseJson(id) {
        var element = document.getElementById(id);
        if (!element) {
            return null;
        }
        try {
            return JSON.parse(element.value || 'null');
        }
        catch (_a) {
            return null;
        }
    }
    var texts = parseJson('reset-password-alerts-json');
    if (texts) {
        window.ResetPasswordAlertTexts = texts;
    }
})();
//# sourceMappingURL=reset-password-confirmation.js.map