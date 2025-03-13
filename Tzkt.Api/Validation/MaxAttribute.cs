namespace System.ComponentModel.DataAnnotations
{
    public sealed class MaxAttribute(int maximum) : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            return value is int v && v > maximum
                ? new ValidationResult($"The value must be less than or equal to {maximum}.")
                : ValidationResult.Success!;
        }
    }
}
