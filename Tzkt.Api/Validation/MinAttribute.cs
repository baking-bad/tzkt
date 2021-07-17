namespace System.ComponentModel.DataAnnotations
{
    public sealed class MinAttribute : ValidationAttribute
    {
        readonly int Minimum;
        
        public MinAttribute(int minimum)
        {
            Minimum = minimum;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            return value != null && (int)value < Minimum
                ? new ValidationResult($"The value must be greater than or equal to {Minimum}.")
                : ValidationResult.Success;
        }
    }
}
