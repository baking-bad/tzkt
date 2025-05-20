using Tzkt.Api;

namespace System.ComponentModel.DataAnnotations
{
    public sealed class TzAddressAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            return value is string str && !Regexes.TzAddress().IsMatch(str)
                ? new ValidationResult("Invalid tz-address.")
                : ValidationResult.Success!;
        }
    }
}
