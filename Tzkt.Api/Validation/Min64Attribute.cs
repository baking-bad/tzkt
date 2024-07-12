namespace System.ComponentModel.DataAnnotations
{
    public sealed class Min64Attribute : ValidationAttribute
    {
        readonly long Minimum;

        public Min64Attribute(long minimum)
        {
            Minimum = minimum;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            return value != null && (long)value < Minimum
                ? new ValidationResult($"The value must be greater than or equal to {Minimum}.")
                : ValidationResult.Success;
        }
    }
}
