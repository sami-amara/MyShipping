using AppResource;
using System;
using System.ComponentModel.DataAnnotations;

namespace Business.DTOS
{
    public class UserSenderDto : BaseDto
    {

        public Guid UserId { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "NameRequired", ErrorMessageResourceType = typeof(ValidationMessages))]
        [StringLength(200, MinimumLength = 2, ErrorMessageResourceName = "NameLength", ErrorMessageResourceType = typeof(ValidationMessages))]
        [RegularExpression(@"^[\p{L}\p{M}0-9\s\.\-']+$", ErrorMessageResourceName = "OnlyValidNamesAllowed", ErrorMessageResourceType = typeof(ValidationMessages))]
        public string SenderName { get; set; } = null!;



        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "ContactRequired", ErrorMessageResourceType = typeof(ValidationMessages))]
        [RegularExpression(@"^\+?[0-9\s\-]{7,15}$", ErrorMessageResourceName = "InvalidPhone", ErrorMessageResourceType = typeof(ValidationMessages))]
        public string? Contact { get; set; }


        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "ContactRequired", ErrorMessageResourceType = typeof(ValidationMessages))]
        [RegularExpression(@"^\+?[0-9\s\-]{7,15}$", ErrorMessageResourceName = "InvalidPhone", ErrorMessageResourceType = typeof(ValidationMessages))]
        public string Phone { get; set; } = null!;


        public Guid CountryId { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "AddressRequired", ErrorMessageResourceType = typeof(ValidationMessages))]
        [StringLength(250, MinimumLength = 3, ErrorMessageResourceName = "AddressLength", ErrorMessageResourceType = typeof(ValidationMessages))]
        public string? Address { get; set; }



        //[StringLength(500, ErrorMessageResourceName = "AddressLength", ErrorMessageResourceType = typeof(ValidationMessages))]
        public string Address2 { get; set; }



        //[StringLength(500, ErrorMessageResourceName = "AddressLength", ErrorMessageResourceType = typeof(ValidationMessages))]
        public string Address3 { get; set; }




        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "PostalRequired", ErrorMessageResourceType = typeof(ValidationMessages))]
        [RegularExpression(@"^[A-Za-z0-9\s\-]{3,10}$", ErrorMessageResourceName = "InvalidPostal", ErrorMessageResourceType = typeof(ValidationMessages))]
        [StringLength(30, MinimumLength = 3, ErrorMessageResourceName = "PostalLength", ErrorMessageResourceType = typeof(ValidationMessages))]
        public string? PostalCode { get; set; }




        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "CityRequired", ErrorMessageResourceType = typeof(ValidationMessages))]
        public Guid? CityId { get; set; }



        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "EmailRequired", ErrorMessageResourceType = typeof(ValidationMessages))]
        [EmailAddress(ErrorMessageResourceName = "InvalidEmail", ErrorMessageResourceType = typeof(ValidationMessages))]
        public string? Email { get; set; }




        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "OtherAddressRequired", ErrorMessageResourceType = typeof(ValidationMessages))]
        [StringLength(300, MinimumLength = 3, ErrorMessageResourceName = "OtherAddressLength", ErrorMessageResourceType = typeof(ValidationMessages))]
        public string OtherAddress { get; set; } = null!;



        // Renamed to match domain/entity property
        public bool IsDefault { get; set; }



        // New: friendly location strings
        public string? CityName { get; set; }
        public string? CountryName { get; set; }
    }
}

