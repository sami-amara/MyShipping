using SendGrid;
using SendGrid.Helpers.Mail;

namespace WebApi.Services
{
    /// <summary>
    /// Email service using SendGrid for sending transactional emails
    /// </summary>
    public interface IEmailService
    {
        Task SendEmailConfirmationAsync(string toEmail, string confirmationLink);
        Task SendPasswordResetAsync(string toEmail, string resetLink);
        Task SendAccountLockedNotificationAsync(string toEmail, DateTimeOffset? lockoutEnd);
        Task SendWelcomeEmailAsync(string toEmail, string userName);

        // New methods for account change notifications (Phase 6 implementation)
        /// <summary>
        /// Notify user when their name changes (security alert)
        /// </summary>
        Task SendAccountNameChangedNotificationAsync(string toEmail, string newFirstName, string newLastName);

        /// <summary>
        /// Notify user when their phone number changes (security alert)
        /// </summary>
        Task SendAccountPhoneChangedNotificationAsync(string toEmail, string newPhone);

        /// <summary>
        /// Notify user when both name and phone change (security alert)
        /// </summary>
        Task SendAccountNameAndPhoneChangedNotificationAsync(string toEmail, string newFirstName, string newLastName, string newPhone);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            _apiKey = _configuration["SendGrid:ApiKey"] ?? throw new ArgumentNullException("SendGrid:ApiKey");
            _fromEmail = _configuration["SendGrid:FromEmail"] ?? "noreply@myshipping.com";
            _fromName = _configuration["SendGrid:FromName"] ?? "MyShipping Support";
        }

        public async Task SendEmailConfirmationAsync(string toEmail, string confirmationLink)
        {
            var subject = "Confirm Your Email Address";
            var htmlContent = $@"
                <html>
                <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #4CAF50; padding: 20px; text-align: center;'>
                        <h1 style='color: white; margin: 0;'>Welcome to MyShipping!</h1>
                    </div>
                    <div style='padding: 30px; background-color: #f9f9f9;'>
                        <h2>Confirm Your Email Address</h2>
                        <p>Thank you for registering with MyShipping. Please confirm your email address by clicking the button below:</p>
                        
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{confirmationLink}' 
                               style='background-color: #4CAF50; color: white; padding: 12px 30px; 
                                      text-decoration: none; border-radius: 5px; display: inline-block;'>
                                Confirm Email Address
                            </a>
                        </div>
                        
                        <p>Or copy and paste this link into your browser:</p>
                        <p style='word-break: break-all; color: #666;'>{confirmationLink}</p>
                        
                        <p style='margin-top: 30px; color: #999; font-size: 12px;'>
                            If you didn't create an account with MyShipping, you can safely ignore this email.
                        </p>
                    </div>
                    <div style='background-color: #333; padding: 15px; text-align: center; color: #999; font-size: 12px;'>
                        © {DateTime.Now.Year} MyShipping. All rights reserved.
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, htmlContent);
        }

        public async Task SendPasswordResetAsync(string toEmail, string resetLink)
        {
            var subject = "Reset Your Password";
            var htmlContent = $@"
                <html>
                <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #FF9800; padding: 20px; text-align: center;'>
                        <h1 style='color: white; margin: 0;'>Password Reset Request</h1>
                    </div>
                    <div style='padding: 30px; background-color: #f9f9f9;'>
                        <h2>Reset Your Password</h2>
                        <p>We received a request to reset your password. Click the button below to create a new password:</p>
                        
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{resetLink}' 
                               style='background-color: #FF9800; color: white; padding: 12px 30px; 
                                      text-decoration: none; border-radius: 5px; display: inline-block;'>
                                Reset Password
                            </a>
                        </div>
                        
                        <p>Or copy and paste this link into your browser:</p>
                        <p style='word-break: break-all; color: #666;'>{resetLink}</p>
                        
                        <p style='margin-top: 30px;'><strong>⚠️ This link will expire in 24 hours.</strong></p>
                        
                        <p style='margin-top: 30px; color: #999; font-size: 12px;'>
                            If you didn't request a password reset, please ignore this email or contact support if you have concerns.
                        </p>
                    </div>
                    <div style='background-color: #333; padding: 15px; text-align: center; color: #999; font-size: 12px;'>
                        © {DateTime.Now.Year} MyShipping. All rights reserved.
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, htmlContent);
        }

        public async Task SendAccountLockedNotificationAsync(string toEmail, DateTimeOffset? lockoutEnd)
        {
            var lockoutMessage = lockoutEnd.HasValue
                ? $"Your account has been locked until {lockoutEnd.Value.LocalDateTime:yyyy-MM-dd HH:mm:ss}"
                : "Your account has been locked due to multiple failed login attempts.";

            var subject = "Account Security Alert - Account Locked";
            var htmlContent = $@"
                <html>
                <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #F44336; padding: 20px; text-align: center;'>
                        <h1 style='color: white; margin: 0;'>🔒 Security Alert</h1>
                    </div>
                    <div style='padding: 30px; background-color: #f9f9f9;'>
                        <h2>Account Locked</h2>
                        <p>{lockoutMessage}</p>
                        
                        <p>This is a security measure to protect your account from unauthorized access.</p>
                        
                        <p><strong>What to do:</strong></p>
                        <ul>
                            <li>Wait for the lockout period to expire</li>
                            <li>If you don't recognize this activity, reset your password immediately</li>
                            <li>Contact support if you need immediate assistance</li>
                        </ul>
                        
                        <p style='margin-top: 30px; color: #999; font-size: 12px;'>
                            If you didn't attempt to log in, your account may be at risk. Please contact our support team.
                        </p>
                    </div>
                    <div style='background-color: #333; padding: 15px; text-align: center; color: #999; font-size: 12px;'>
                        © {DateTime.Now.Year} MyShipping. All rights reserved.
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, htmlContent);
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string userName)
        {
            var subject = "Welcome to MyShipping!";
            var htmlContent = $@"
                <html>
                <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #2196F3; padding: 20px; text-align: center;'>
                        <h1 style='color: white; margin: 0;'>Welcome to MyShipping!</h1>
                    </div>
                    <div style='padding: 30px; background-color: #f9f9f9;'>
                        <h2>Hi {userName},</h2>
                        <p>Your email has been confirmed and your account is now active!</p>
                        
                        <p><strong>Getting Started:</strong></p>
                        <ul>
                            <li>Create your first shipment</li>
                            <li>Track packages in real-time</li>
                            <li>Manage multiple shipping addresses</li>
                            <li>View shipping history and invoices</li>
                        </ul>
                        
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='https://localhost:7065' 
                               style='background-color: #2196F3; color: white; padding: 12px 30px; 
                                      text-decoration: none; border-radius: 5px; display: inline-block;'>
                                Go to Dashboard
                            </a>
                        </div>
                        
                        <p style='margin-top: 30px; color: #999; font-size: 12px;'>
                            Need help? Contact our support team at support@myshipping.com
                        </p>
                    </div>
                    <div style='background-color: #333; padding: 15px; text-align: center; color: #999; font-size: 12px;'>
                        © {DateTime.Now.Year} MyShipping. All rights reserved.
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, htmlContent);
        }

        public async Task SendAccountNameChangedNotificationAsync(string toEmail, string newFirstName, string newLastName)
        {
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
                            <p><strong>Change Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
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
                            <p><strong>Change Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
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
                            <p><strong>Change Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
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

        private async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            try
            {
                var client = new SendGridClient(_apiKey);
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(toEmail);
                var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlContent);

                var response = await client.SendEmailAsync(msg);

                if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    _logger.LogInformation("📧 Email sent successfully to {Email} | Subject: {Subject}", toEmail, subject);
                }
                else
                {
                    _logger.LogWarning("⚠️ Email send failed | Status: {StatusCode} | To: {Email}", 
                        response.StatusCode, toEmail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Email send exception | To: {Email} | Subject: {Subject}", 
                    toEmail, subject);
                throw;
            }
        }
    }
}
