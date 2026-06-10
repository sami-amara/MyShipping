using System.ComponentModel.DataAnnotations;

namespace UI.Models
{
    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Token { get; set; }
    }
    //public class ResetPasswordViewModel
    //{
    //    [Required, EmailAddress]
    //    public string Email { get; set; }

    //    [Required]
    //    public string Token { get; set; }

    //    [Required, DataType(DataType.Password)]
    //    public string NewPassword { get; set; }

    //    [Required, DataType(DataType.Password), Compare("NewPassword")]
    //    public string ConfirmPassword { get; set; }
    //}
}
