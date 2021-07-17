namespace System.ComponentModel.DataAnnotations
{
    public sealed class MaxAttribute : ValidationAttribute
    {
        readonly int Maximum;
        
        public MaxAttribute(int maximum)
        {
            Maximum = maximum;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            return value != null && (int)value > Maximum
                ? new ValidationResult($"The value must be less than or equal to {Maximum}.")
                : ValidationResult.Success;
        }
    }
}
