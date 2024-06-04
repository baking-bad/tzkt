using System.Text.RegularExpressions;

namespace System.ComponentModel.DataAnnotations
{
    public sealed class ExpressionHashAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            return value != null && !Regex.IsMatch((string)value, "^expr[0-9A-Za-z]{50}$")
                ? new ValidationResult("Invalid expression hash.")
                : ValidationResult.Success;
        }
    }
}
