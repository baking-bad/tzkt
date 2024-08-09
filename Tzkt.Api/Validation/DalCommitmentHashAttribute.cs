using System.Text.RegularExpressions;

namespace System.ComponentModel.DataAnnotations
{
    public sealed class DalCommitmentHashAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            return value != null && !Regex.IsMatch((string)value, "^sh[0-9A-Za-z]{72}$")
                ? new ValidationResult("Invalid DAL commitment hash.")
                : ValidationResult.Success;
        }
    }
}
