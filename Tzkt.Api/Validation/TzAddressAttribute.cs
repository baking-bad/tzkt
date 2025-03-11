using System.Text.RegularExpressions;

namespace System.ComponentModel.DataAnnotations
{
    public sealed class TzAddressAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            return value is string str && !Regex.IsMatch(str, "^tz[0-9A-Za-z]{34}$")
                ? new ValidationResult("Invalid tz-address.")
                : ValidationResult.Success!;
        }
    }
}
