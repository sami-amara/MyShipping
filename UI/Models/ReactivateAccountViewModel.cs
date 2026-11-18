using System.ComponentModel.DataAnnotations;

namespace UI.Models
{
    public class ReactivateAccountViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(6, MinimumLength = 6)]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Enter the 6-digit code.")]
        public string Code { get; set; } = string.Empty;
    }
}
