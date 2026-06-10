document.addEventListener('DOMContentLoaded', function () {
    var typeEl = document.getElementById('alert-message-type');
    var textsEl = document.getElementById('alert-message-texts-json');
    if (!typeEl || !textsEl)
        return;
    var messageType = parseInt(typeEl.value || '0', 10);
    var alertTexts = {};
    try {
        alertTexts = JSON.parse(textsEl.value || '{}');
    }
    catch (_a) {
        alertTexts = {};
    }
    var resetPasswordAlertTexts = window.ResetPasswordAlertTexts || null;
    switch (messageType) {
        case 1:
            showAlert.Success(alertTexts.saveSuccessTitle, alertTexts.saveSuccessMessage);
            break;
        case 2:
            showAlert.Error(alertTexts.saveFailedTitle, alertTexts.saveFailedMessage);
            break;
        case 3:
            showAlert.Success(alertTexts.deleteSuccessTitle, alertTexts.deleteSuccessMessage);
            break;
        case 4:
            showAlert.Error(alertTexts.deleteFailedTitle, alertTexts.deleteFailedMessage);
            break;
        case 5:
            showAlert.Success((resetPasswordAlertTexts && resetPasswordAlertTexts.title) || alertTexts.updateSuccessTitle, (resetPasswordAlertTexts && resetPasswordAlertTexts.message) || alertTexts.updateSuccessMessage);
            break;
        case 6:
            showAlert.Success(alertTexts.registrationSuccessTitle, alertTexts.registrationSuccessMessage);
            break;
        case 7:
            showAlert.Error(alertTexts.registrationFailedTitle, alertTexts.registrationFailedMessage);
            break;
        case 15:
            showAlert.Error(alertTexts.lockedFaildTitle, alertTexts.lockedFaildMessage);
            break;
        case 16:
            showAlert.Error(alertTexts.unlockedFaildTitle, alertTexts.unlockedFaildMessage);
            break;
        case 17:
            showAlert.Success(alertTexts.lockedSuccessTitle, alertTexts.lockedSuccessMessage);
            break;
        case 18:
            showAlert.Success(alertTexts.unlockedSuccessTitle, alertTexts.unlockedSuccessMessage);
            break;
        case 19:
            showAlert.Error(alertTexts.adminLockedTitle, alertTexts.adminLockedMessage);
            break;
        case 20:
            showAlert.Error(alertTexts.tempLockedTitle, alertTexts.tempLockedMessage);
            break;
        default:
            break;
    }
});
//# sourceMappingURL=AlertMessageHandler.js.map