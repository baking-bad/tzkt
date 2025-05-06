using System.Text.RegularExpressions;

namespace System.ComponentModel.DataAnnotations
{
    public sealed class HexAttribute(int len) : ValidationAttribute
    {
        readonly string Pattern = $@"^[0-9A-Fa-f]{{{len}}}$";

        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            return value is string str && !Regex.IsMatch(str, Pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100))
                ? new ValidationResult("Invalid hex format or length")
                : ValidationResult.Success!;
        }
    }
}
