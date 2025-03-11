namespace System.ComponentModel.DataAnnotations
{
    public sealed class Min64Attribute(long minimum) : ValidationAttribute
    {
        readonly long Minimum = minimum;

        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            return value is long v && v < Minimum
                ? new ValidationResult($"The value must be greater than or equal to {Minimum}.")
                : ValidationResult.Success!;
        }
    }
}
