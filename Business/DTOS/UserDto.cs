


using AppResource;
using Business.DTOS;
using Business.Validations;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;


namespace Business.DTOS
{
    public class UserDto : BaseDto
    {
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "FristNameRequired", ErrorMessageResourceType = typeof(ValidationMessages))]
        [StringLength(200, MinimumLength = 2, ErrorMessageResourceName = "NameLength", ErrorMessageResourceType = typeof(ValidationMessages))]
        //[RegularExpression(@"^[\p{L}\p{Zs}\.\-'''`]+$", ErrorMessageResourceName = "OnlyValidNamesAllowed", ErrorMessageResourceType = typeof(ValidationMessages))]
        public string FirstName { get; set; } = string.Empty;



        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "LastNameRequired", ErrorMessageResourceType = typeof(ValidationMessages))]
        [StringLength(200, MinimumLength = 2, ErrorMessageResourceName = "NameLength", ErrorMessageResourceType = typeof(ValidationMessages))]
        //RegularExpression(@"^[\p{L}\p{Zs}\.\-'''`]+$", ErrorMessageResourceName = "OnlyValidNamesAllowed", ErrorMessageResourceType = typeof(ValidationMessages))]
        public string LastName { get; set; } = string.Empty;



        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "EmailRequired", ErrorMessageResourceType = typeof(ValidationMessages))]
        [EmailAddress(ErrorMessageResourceName = "InvalidEmail", ErrorMessageResourceType = typeof(ValidationMessages))]
       
        public string Email { get; set; } = string.Empty;



        //[RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).{6,}$",ErrorMessageResourceType = typeof(ValidationMessages),ErrorMessageResourceName = "InvalidPassword")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "PasswordRequired", ErrorMessageResourceType = typeof(ValidationMessages))]
        [MinLength(8, ErrorMessageResourceName = "InvalidPassword", ErrorMessageResourceType = typeof(ValidationMessages))]
        public string Password { get; set; } = string.Empty;



        [Compare("Password", ErrorMessageResourceName = "PasswordsDoNotMatch", ErrorMessageResourceType = typeof(ValidationMessages))]
        public string ConfirmPassword { get; set; } = string.Empty;



        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "ContactRequired", ErrorMessageResourceType = typeof(ValidationMessages))]
        [RegularExpression(@"^\+?[0-9\s\-]{7,15}$", ErrorMessageResourceName = "InvalidPhone", ErrorMessageResourceType = typeof(ValidationMessages))]
       
        public string Phone { get; set; } = string.Empty;


        public string Role { get; set; } = string.Empty;

        public bool NotifyByEmail { get; set; } = true;

        public bool NotifyBySms { get; set; }

        public bool NotifyShipmentStatusUpdates { get; set; } = true;

        public bool NotifyMarketing { get; set; }

        public Guid? DefaultCountryId { get; set; }

        public Guid? DefaultCityId { get; set; }

        public Guid? DefaultCarrierId { get; set; }

        public Guid? DefaultShippingPackageId { get; set; }

        public Guid? DefaultShippingTypeId { get; set; }

        public bool IsLockedOut { get; set; }

        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }


        /// <summary>
        /// Indicates if the user has requested account deactivation.
        /// When true, the account cannot be used for login but data is retained.
        /// </summary>
        public bool IsDeactivated { get; set; }

        /// <summary>
        /// Timestamp when the user requested deactivation (for audit/GDPR purposes).
        /// </summary>
        public DateTime? DeactivatedAt { get; set; }

    }



}

