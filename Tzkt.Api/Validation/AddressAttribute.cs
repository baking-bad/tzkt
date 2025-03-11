using System.Text.RegularExpressions;

namespace System.ComponentModel.DataAnnotations
{
    public sealed class AddressAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            return value is string str && !Regex.IsMatch(str, "^[0-9A-Za-z]{36,37}$")
                ? new ValidationResult("Invalid account address.")
                : ValidationResult.Success!;
        }
    }
}
