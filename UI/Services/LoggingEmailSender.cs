using Business.Contracts;

namespace UI.Services
{
    /// <summary>
    /// Development-only IEmailSender: writes the email to the log instead of sending it.
    /// Lets you read reactivation codes / notifications without any SMTP or SendGrid account.
    /// Never register it outside Development: the log would contain security codes.
    /// </summary>
    public class LoggingEmailSender : IEmailSender
    {
        private readonly ILogger<LoggingEmailSender> _logger;

        public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

        public Task SendEmailAsync(string to, string subject, string body)
        {
            _logger.LogWarning("📧 [DEV EMAIL] To: {To} | Subject: {Subject} | Body: {Body}", to, subject, body);
            return Task.CompletedTask;
        }

        public Task SendAccountNameChangedNotificationAsync(string toEmail, string newFirstName, string newLastName) =>
            SendEmailAsync(toEmail, "Your name was changed", $"Your name is now {newFirstName} {newLastName}.");

        public Task SendAccountPhoneChangedNotificationAsync(string toEmail, string newPhone) =>
            SendEmailAsync(toEmail, "Your phone number was changed", "Your phone number was updated.");

        public Task SendAccountNameAndPhoneChangedNotificationAsync(string toEmail, string newFirstName, string newLastName, string newPhone) =>
            SendEmailAsync(toEmail, "Your name and phone were changed", $"Your name is now {newFirstName} {newLastName} and your phone was updated.");

        public Task SendAccountPasswordChangedNotificationAsync(string toEmail) =>
            SendEmailAsync(toEmail, "Your password was changed", "If this was not you, contact support immediately.");
    }
}
