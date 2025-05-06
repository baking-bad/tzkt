using Tzkt.Api;

namespace System.ComponentModel.DataAnnotations
{
    public sealed class OpHashAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            return value is string str && !Regexes.Operation().IsMatch(str)
                ? new ValidationResult("Invalid operation hash.")
                : ValidationResult.Success!;
        }
    }
}
