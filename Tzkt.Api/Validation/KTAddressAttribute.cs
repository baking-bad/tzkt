using Tzkt.Api;

namespace System.ComponentModel.DataAnnotations
{
    public sealed class KTAddressAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            return value is string str && !Regexes.Kt1Address().IsMatch(str)
                ? new ValidationResult("Invalid KT1-address.")
                : ValidationResult.Success!;
        }
    }
}
