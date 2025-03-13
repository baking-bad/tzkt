namespace System.ComponentModel.DataAnnotations
{
    public sealed class Min64Attribute(long minimum) : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            return value is long v && v < minimum
                ? new ValidationResult($"The value must be greater than or equal to {minimum}.")
                : ValidationResult.Success!;
        }
    }
}
