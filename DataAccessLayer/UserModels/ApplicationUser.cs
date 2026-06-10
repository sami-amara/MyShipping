using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using DataAccessLayer.Models;

namespace DataAccessLayer.UserModels
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Phone { get; set; }

        public bool NotifyByEmail { get; set; } = true;

        public bool NotifyBySms { get; set; } = false;

        public bool NotifyShipmentStatusUpdates { get; set; } = true;

        public bool NotifyMarketing { get; set; } = false;

        public Guid? DefaultCountryId { get; set; }

        public Guid? DefaultCityId { get; set; }

        public Guid? DefaultCarrierId { get; set; }

        public Guid? DefaultShippingPackageId { get; set; }

        public Guid? DefaultShippingTypeId { get; set; }


        /// <summary>
        /// Indicates if the user has requested account deactivation.
        /// When true, the account cannot be used for login but data is retained.
        /// </summary>
        public bool IsDeactivated { get; set; }

        /// <summary>
        /// Timestamp when the user requested deactivation (for audit/GDPR purposes).
        /// </summary>
        public DateTime? DeactivatedAt { get; set; }

        // ✅ NEW: Navigation property for audit logs
        public virtual ICollection<AccountAuditLog> AuditLogs { get; set; } = new List<AccountAuditLog>();


    }
}
