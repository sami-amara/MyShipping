using System.ComponentModel.DataAnnotations;
using AppResource;

namespace UI.Models
{
    public class AccountSettingsViewModel
    {
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "FristNameRequired", ErrorMessageResourceType = typeof(ValidationMessages))]
        [StringLength(200, MinimumLength = 2, ErrorMessageResourceName = "NameLength", ErrorMessageResourceType = typeof(ValidationMessages))]
        public string FirstName { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "LastNameRequired", ErrorMessageResourceType = typeof(ValidationMessages))]
        [StringLength(200, MinimumLength = 2, ErrorMessageResourceName = "NameLength", ErrorMessageResourceType = typeof(ValidationMessages))]
        public string LastName { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "ContactRequired", ErrorMessageResourceType = typeof(ValidationMessages))]
        [RegularExpression(@"^\+?[0-9\s\-]{7,15}$", ErrorMessageResourceName = "InvalidPhone", ErrorMessageResourceType = typeof(ValidationMessages))]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(en|ar)$")]
        public string Language { get; set; } = "en";
    }
}
