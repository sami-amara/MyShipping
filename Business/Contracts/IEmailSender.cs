using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Business.Contracts
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string to, string subject, string body);

        // Account change notifications (Phase 6 - Security Alerts)
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

        /// <summary>
        /// Notify user when their password changes (CRITICAL security alert - always sent)
        /// </summary>
        Task SendAccountPasswordChangedNotificationAsync(string toEmail);
    }
}



