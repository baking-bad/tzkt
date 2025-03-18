using Tzkt.Api;

namespace System.ComponentModel.DataAnnotations
{
    public sealed class AddressAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            return value is string str && !Regexes.Address().IsMatch(str)
                ? new ValidationResult("Invalid account address.")
                : ValidationResult.Success!;
        }
    }
}
