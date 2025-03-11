namespace System.ComponentModel.DataAnnotations
{
    public sealed class MinAttribute(int minimum) : ValidationAttribute
    {
        readonly int Minimum = minimum;

        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            return value is int v && v < Minimum
                ? new ValidationResult($"The value must be greater than or equal to {Minimum}.")
                : ValidationResult.Success!;
        }
    }
}
