namespace System.ComponentModel.DataAnnotations
{
    public sealed class MaxAttribute(int maximum) : ValidationAttribute
    {
        readonly int Maximum = maximum;

        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            return value is int v && v > Maximum
                ? new ValidationResult($"The value must be less than or equal to {Maximum}.")
                : ValidationResult.Success!;
        }
    }
}
