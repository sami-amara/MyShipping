namespace UI.Models
{
    public class AccountSettingsPageViewModel
    {
        public AccountSettingsViewModel Profile { get; set; } = new AccountSettingsViewModel();
        public AccountSecuritySettingsViewModel Security { get; set; } = new AccountSecuritySettingsViewModel();
        public AccountNotificationSettingsViewModel Notifications { get; set; } = new AccountNotificationSettingsViewModel();
        public AccountShippingPreferencesViewModel ShippingPreferences { get; set; } = new AccountShippingPreferencesViewModel();
        public AccountPrivacySettingsViewModel Privacy { get; set; } = new AccountPrivacySettingsViewModel();
    }
}
