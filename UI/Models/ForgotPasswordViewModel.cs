using System.ComponentModel.DataAnnotations;

namespace UI.Models
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    //public class ForgotPasswordViewModel
    //{
    //    [Required, EmailAddress]
    //    public string Email { get; set; }
    //}
}
