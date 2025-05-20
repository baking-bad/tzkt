using Tzkt.Api;

namespace System.ComponentModel.DataAnnotations
{
    public sealed class ProtocolHashAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            return value is string str && !Regexes.Protocol().IsMatch(str)
                ? new ValidationResult("Invalid protocol hash.")
                : ValidationResult.Success!;
        }
    }
}
