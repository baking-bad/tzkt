using System.Text.RegularExpressions;

namespace System.ComponentModel.DataAnnotations
{
    public sealed class KTAddressAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            return value != null && !Regex.IsMatch((string)value, "^KT1[0-9A-Za-z]{33}$")
                ? new ValidationResult("Invalid KT1-address.")
                : ValidationResult.Success;
        }
    }
}
