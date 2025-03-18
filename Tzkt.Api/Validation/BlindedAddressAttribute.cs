using Tzkt.Api;

namespace System.ComponentModel.DataAnnotations
{
    public sealed class BlindedAddressAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            return value is string str && !Regexes.BtzAddress().IsMatch(str)
                ? new ValidationResult("Invalid blinded address.")
                : ValidationResult.Success!;
        }
    }
}
