namespace UI.Models
{
    public class AccountNotificationSettingsViewModel
    {
        public bool NotifyByEmail { get; set; } = true;

        public bool NotifyBySms { get; set; }

        public bool NotifyShipmentStatusUpdates { get; set; } = true;

        public bool NotifyMarketing { get; set; }
    }
}
