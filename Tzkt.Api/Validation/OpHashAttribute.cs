using System.Text.RegularExpressions;

namespace System.ComponentModel.DataAnnotations
{
    public sealed class OpHashAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            return value is string str && !Regex.IsMatch(str, "^o[0-9A-Za-z]{50}$")
                ? new ValidationResult("Invalid operation hash.")
                : ValidationResult.Success!;
        }
    }
}
