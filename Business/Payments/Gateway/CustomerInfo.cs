using System;

namespace Business.Payments.Gateway
{
    /// <summary>
    /// Customer information for payment processing
    /// </summary>
    public class CustomerInfo
    {
        /// <summary>
        /// Customer ID (internal system user ID)
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Customer email address
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Customer name
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Customer phone number
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// Billing address
        /// </summary>
        public AddressInfo? BillingAddress { get; set; }
    }

    /// <summary>
    /// Address information
    /// </summary>
    public class AddressInfo
    {
        public string? Line1 { get; set; }
        public string? Line2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
    }
}
