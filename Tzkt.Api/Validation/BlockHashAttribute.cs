using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace System.ComponentModel.DataAnnotations
{
    public sealed class BlockHashAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            return value != null && !Regex.IsMatch((string)value, "^B[0-9A-z]{50}$")
                ? new ValidationResult("Invalid block hash.")
                : ValidationResult.Success;
        }
    }
}
