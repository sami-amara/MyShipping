using System.ComponentModel.DataAnnotations;
using AppResource;

namespace UI.Models
{
    public class AccountSecuritySettingsViewModel
    {
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "PasswordRequired", ErrorMessageResourceType = typeof(ValidationMessages))]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "PasswordRequired", ErrorMessageResourceType = typeof(ValidationMessages))]
        [MinLength(8, ErrorMessageResourceName = "InvalidPassword", ErrorMessageResourceType = typeof(ValidationMessages))]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Compare("NewPassword", ErrorMessageResourceName = "PasswordsDoNotMatch", ErrorMessageResourceType = typeof(ValidationMessages))]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
