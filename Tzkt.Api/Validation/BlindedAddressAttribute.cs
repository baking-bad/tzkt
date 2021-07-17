using System.Text.RegularExpressions;

namespace System.ComponentModel.DataAnnotations
{
    public sealed class BlindedAddressAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            return value != null && !Regex.IsMatch((string)value, "^btz[0-9A-Za-z]{34}$")
                ? new ValidationResult("Invalid blinded address.")
                : ValidationResult.Success;
        }
    }
}
