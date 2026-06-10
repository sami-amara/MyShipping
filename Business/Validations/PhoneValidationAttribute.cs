using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Business.Validations
{
    public class PhoneValidationAttribute : ValidationAttribute
    {
        // Example pattern: allows +, digits, spaces, dashes, and parentheses, 7-20 chars
        private const string PhonePattern = @"^\+?[\d\s\-()]{7,20}$";

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success; // Use [Required] for null check

            var phone = value as string;
            if (string.IsNullOrWhiteSpace(phone))
                return ValidationResult.Success; // Use [Required] for empty check

            if (!Regex.IsMatch(phone, PhonePattern))
            {
                // Use the resource manager to get the error message string directly
                var errorMessage = ErrorMessageResourceType?
                    .GetProperty(ErrorMessageResourceName)?
                    .GetValue(null, null) as string ?? ErrorMessage;

                return new ValidationResult(errorMessage, new[] { validationContext.MemberName });
            }

            return ValidationResult.Success;
        }
    }
}
