using System.Text.RegularExpressions;

namespace System.ComponentModel.DataAnnotations
{
    public sealed class BlindedAddressAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            return value is string str && !Regex.IsMatch(str, "^btz[0-9A-Za-z]{34}$")
                ? new ValidationResult("Invalid blinded address.")
                : ValidationResult.Success!;
        }
    }
}
