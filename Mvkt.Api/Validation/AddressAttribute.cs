using System.Text.RegularExpressions;

namespace System.ComponentModel.DataAnnotations
{
    public sealed class AddressAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            return value != null && !Regex.IsMatch((string)value, "^[0-9A-Za-z]{36,37}$")
                ? new ValidationResult("Invalid account address.")
                : ValidationResult.Success;
        }
    }
}
