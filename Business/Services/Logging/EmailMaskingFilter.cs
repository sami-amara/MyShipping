using Serilog.Core;
using Serilog.Events;



namespace Business.Services.Logging
{
    public class EmailMaskingFilter : ILogEventFilter
    {
        public bool IsEnabled(LogEvent logEvent)
        {
            if (logEvent == null)
                return false;

            if (logEvent.Properties.TryGetValue("Email", out var emailValue))
            {
                var rawEmail = emailValue.ToString().Trim('"');
                var maskedEmail = MaskEmail(rawEmail);
                if (maskedEmail != rawEmail)
                {
                    // Replace property value with masked email
                    logEvent.AddOrUpdateProperty(
                        new LogEventProperty("Email", new ScalarValue(maskedEmail))
                    );
                }
            }

            return true; // Always allow event
        }

        /// <summary>
        /// Masks an email address, keeping only the first letter of the local part visible.
        /// e.g. "wasan@gmail.com" -> "w***@gmail.com"
        /// </summary>
        public static string MaskEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return email ?? string.Empty;

            var rawEmail = email.Trim('"');
            if (!rawEmail.Contains("@"))
                return rawEmail;

            var parts = rawEmail.Split('@');
            if (parts.Length != 2)
                return rawEmail;

            var local = parts[0];
            var domain = parts[1];

            // ✅ Keep only the first letter, mask the rest
            var maskedLocal = local.Length > 0 ? local.Substring(0, 1) + "***" : "***";
            return $"{maskedLocal}@{domain}";
        }
    }
}
