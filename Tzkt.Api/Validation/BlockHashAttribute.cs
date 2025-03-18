using Tzkt.Api;

namespace System.ComponentModel.DataAnnotations
{
    public sealed class BlockHashAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            return value is string str && !Regexes.Block().IsMatch(str)
                ? new ValidationResult("Invalid block hash.")
                : ValidationResult.Success!;
        }
    }
}
