using System.Text.RegularExpressions;

namespace System.ComponentModel.DataAnnotations
{
    public sealed class HexAttribute : ValidationAttribute
    {
        readonly string Pattern;
        public HexAttribute(int len) => Pattern = $@"^[0-9A-Fa-f]{{{len}}}$";

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            return value != null && !Regex.IsMatch((string)value, Pattern)
                ? new ValidationResult("Invalid hex format or length")
                : ValidationResult.Success;
        }
    }
}
