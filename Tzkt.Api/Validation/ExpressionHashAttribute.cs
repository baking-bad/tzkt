using Tzkt.Api;

namespace System.ComponentModel.DataAnnotations
{
    public sealed class ExpressionHashAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            return value is string str && !Regexes.Expression().IsMatch(str)
                ? new ValidationResult("Invalid expression hash.")
                : ValidationResult.Success!;
        }
    }
}
