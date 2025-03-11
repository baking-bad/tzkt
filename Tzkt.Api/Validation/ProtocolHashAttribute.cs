using System.Text.RegularExpressions;

namespace System.ComponentModel.DataAnnotations
{
    public sealed class ProtocolHashAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            return value is string str && !Regex.IsMatch(str, "^P[0-9A-Za-z]{50}$")
                ? new ValidationResult("Invalid protocol hash.")
                : ValidationResult.Success!;
        }
    }
}
