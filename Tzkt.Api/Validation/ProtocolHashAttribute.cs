using System.Text.RegularExpressions;

namespace System.ComponentModel.DataAnnotations
{
    public sealed class ProtocolHashAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            return value != null && !Regex.IsMatch((string)value, "^P[0-9A-Za-z]{50}$")
                ? new ValidationResult("Invalid protocol hash.")
                : ValidationResult.Success;
        }
    }
}
