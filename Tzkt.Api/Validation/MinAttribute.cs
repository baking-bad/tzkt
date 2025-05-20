namespace System.ComponentModel.DataAnnotations
{
    public sealed class MinAttribute(int minimum) : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            return value is int v && v < minimum
                ? new ValidationResult($"The value must be greater than or equal to {minimum}.")
                : ValidationResult.Success!;
        }
    }
}
