using Business.Contracts;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace UI.Services
{
    public class EmailSenderService : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSenderService> _logger;

        public EmailSenderService(IConfiguration configuration, ILogger<EmailSenderService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var apiKey = _configuration["SendGrid:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("SendGrid API key is missing. Email to {Email} was not sent.", to);
                return;
            }

            var fromEmail = _configuration["SendGrid:FromEmail"] ?? "noreply@myshipping.com";
            var fromName = _configuration["SendGrid:FromName"] ?? "MyShipping Support";

            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(fromEmail, fromName);
            var toAddress = new EmailAddress(to);
            var message = MailHelper.CreateSingleEmail(from, toAddress, subject, null, body);

            var response = await client.SendEmailAsync(message);
            var statusCode = (int)response.StatusCode;
            var messageId = response.Headers.TryGetValues("X-Message-Id", out var messageIds)
                ? messageIds.FirstOrDefault()
                : null;

            if (statusCode >= 400)
            {
                var responseBody = await response.Body.ReadAsStringAsync();
                _logger.LogWarning("SendGrid send failed. To={Email}, Subject={Subject}, Status={StatusCode}, MessageId={MessageId}, Response={Response}",
                    to, subject, statusCode, messageId ?? "N/A", responseBody);
            }
            else
            {
                _logger.LogInformation("SendGrid accepted email. To={Email}, Subject={Subject}, Status={StatusCode}, MessageId={MessageId}",
                    to, subject, statusCode, messageId ?? "N/A");
            }
        }

        public async Task SendAccountNameChangedNotificationAsync(string toEmail, string newFirstName, string newLastName)
        {
            var localDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var subject = "Account Security Alert - Name Updated";
            var htmlContent = $@"
                <html>
                <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #FF9800; padding: 20px; text-align: center;'>
                        <h1 style='color: white; margin: 0;'>🔒 Account Change Notification</h1>
                    </div>
                    <div style='padding: 30px; background-color: #f9f9f9;'>
                        <h2>Your Account Name has been Updated</h2>
                        <p>We're notifying you that your account name has been changed.</p>

                        <div style='background-color: white; padding: 20px; border-left: 4px solid #FF9800; margin: 20px 0;'>
                            <p><strong>New Name:</strong> {newFirstName} {newLastName}</p>
                            <p><strong>Change Time:</strong> {localDateTime}</p>
                        </div>

                        <p><strong>What to do:</strong></p>
                        <ul>
                            <li>If you made this change, no action is required.</li>
                            <li>If you didn't make this change, please <a href='https://localhost:7065/Account/ResetPassword'>reset your password immediately</a></li>
                            <li>Contact our support team if you need assistance</li>
                        </ul>

                        <p style='margin-top: 30px; color: #999; font-size: 12px;'>
                            For your security, we monitor all account changes. Never share your password or account details with anyone.
                        </p>
                    </div>
                    <div style='background-color: #333; padding: 15px; text-align: center; color: #999; font-size: 12px;'>
                        © {DateTime.Now.Year} MyShipping. All rights reserved.
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, htmlContent);
        }

        public async Task SendAccountPhoneChangedNotificationAsync(string toEmail, string newPhone)
        {
            var localDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var subject = "Account Security Alert - Phone Number Updated";
            var htmlContent = $@"
                <html>
                <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #FF9800; padding: 20px; text-align: center;'>
                        <h1 style='color: white; margin: 0;'>🔒 Account Change Notification</h1>
                    </div>
                    <div style='padding: 30px; background-color: #f9f9f9;'>
                        <h2>Your Phone Number has been Updated</h2>
                        <p>We're notifying you that your account phone number has been changed.</p>

                        <div style='background-color: white; padding: 20px; border-left: 4px solid #FF9800; margin: 20px 0;'>
                            <p><strong>New Phone:</strong> {newPhone}</p>
                            <p><strong>Change Time:</strong> {localDateTime}</p>
                        </div>

                        <p><strong>What to do:</strong></p>
                        <ul>
                            <li>If you made this change, no action is required.</li>
                            <li>If you didn't make this change, please <a href='https://localhost:7065/Account/ResetPassword'>reset your password immediately</a></li>
                            <li>Contact our support team if you need assistance</li>
                        </ul>

                        <p style='margin-top: 30px; color: #999; font-size: 12px;'>
                            For your security, we monitor all account changes. Never share your password or account details with anyone.
                        </p>
                    </div>
                    <div style='background-color: #333; padding: 15px; text-align: center; color: #999; font-size: 12px;'>
                        © {DateTime.Now.Year} MyShipping. All rights reserved.
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, htmlContent);
        }

        public async Task SendAccountNameAndPhoneChangedNotificationAsync(string toEmail, string newFirstName, string newLastName, string newPhone)
        {
            var localDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var subject = "Account Security Alert - Account Information Updated";
            var htmlContent = $@"
                <html>
                <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #FF9800; padding: 20px; text-align: center;'>
                        <h1 style='color: white; margin: 0;'>🔒 Account Change Notification</h1>
                    </div>
                    <div style='padding: 30px; background-color: #f9f9f9;'>
                        <h2>Your Account Information has been Updated</h2>
                        <p>We're notifying you that your account name and phone number have been changed.</p>

                        <div style='background-color: white; padding: 20px; border-left: 4px solid #FF9800; margin: 20px 0;'>
                            <p><strong>New Name:</strong> {newFirstName} {newLastName}</p>
                            <p><strong>New Phone:</strong> {newPhone}</p>
                            <p><strong>Change Time:</strong> {localDateTime}</p>
                        </div>

                        <p><strong>What to do:</strong></p>
                        <ul>
                            <li>If you made these changes, no action is required.</li>
                            <li>If you didn't make these changes, please <a href='https://localhost:7065/Account/ResetPassword'>reset your password immediately</a></li>
                            <li>Contact our support team if you need assistance</li>
                        </ul>

                        <p style='margin-top: 30px; color: #999; font-size: 12px;'>
                            For your security, we monitor all account changes. Never share your password or account details with anyone.
                        </p>
                    </div>
                    <div style='background-color: #333; padding: 15px; text-align: center; color: #999; font-size: 12px;'>
                        © {DateTime.Now.Year} MyShipping. All rights reserved.
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, htmlContent);
        }

        public async Task SendAccountPasswordChangedNotificationAsync(string toEmail)
        {
            var localDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var subject = "🔴 CRITICAL: Your Password Has Been Changed";
            var htmlContent = $@"
                <html>
                <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #F44336; padding: 20px; text-align: center;'>
                        <h1 style='color: white; margin: 0;'>⚠️ PASSWORD CHANGED</h1>
                    </div>
                    <div style='padding: 30px; background-color: #ffebee;'>
                        <h2 style='color: #F44336;'>Your Account Password Has Been Changed</h2>
                        <p><strong>This is a critical security alert.</strong></p>

                        <div style='background-color: white; padding: 20px; border-left: 6px solid #F44336; margin: 20px 0;'>
                            <p>Your account password was successfully changed on:</p>
                            <p style='font-weight: bold; font-size: 16px; color: #F44336;'>{localDateTime}</p>
                        </div>

                        <p style='color: #D32F2F; font-weight: bold;'>⚠️ ACTION REQUIRED - IF YOU DID NOT MAKE THIS CHANGE:</p>
                        <div style='background-color: #FFCDD2; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                            <p style='margin: 0; color: #B71C1C;'>
                                <strong>Your account may be compromised!</strong> Click the button below to secure your account immediately:
                            </p>
                            <div style='text-align: center; margin-top: 15px;'>
                                <a href='https://localhost:7065/Account/ResetPassword' 
                                   style='background-color: #F44336; color: white; padding: 12px 30px; 
                                          text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold;'>
                                    🔒 SECURE YOUR ACCOUNT NOW
                                </a>
                            </div>
                        </div>

                        <h3>What Happened:</h3>
                        <ul>
                            <li>✅ If you changed this password, your account is secure</li>
                            <li>⚠️ If you did NOT change this password, an unauthorized person has access to your account</li>
                        </ul>

                        <h3>What You Should Do:</h3>
                        <ul>
                            <li>✅ If this was you: No action needed. Your account is secure.</li>
                            <li>⚠️ If this was NOT you:
                                <ul>
                                    <li>Click the 'SECURE YOUR ACCOUNT NOW' button above to reset your password</li>
                                    <li>Review your account for unauthorized activity</li>
                                    <li>Contact our support team immediately (support@myshipping.com)</li>
                                </ul>
                            </li>
                        </ul>

                        <p style='margin-top: 30px; color: #999; font-size: 12px; border-top: 1px solid #ccc; padding-top: 15px;'>
                            <strong>Note:</strong> This is a security alert email. We sent this because your password was changed. 
                            If you have any questions about your account security, please contact support@myshipping.com
                        </p>
                    </div>
                    <div style='background-color: #333; padding: 15px; text-align: center; color: #999; font-size: 12px;'>
                        © {DateTime.Now.Year} MyShipping. All rights reserved.
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, htmlContent);
        }
    }
}
