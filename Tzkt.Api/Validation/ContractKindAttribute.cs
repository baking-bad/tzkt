using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace System.ComponentModel.DataAnnotations
{
    public sealed class ContractKindAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            return value != null && (string)value != "delegator_contract" && (string)value != "smart_contract"
                ? new ValidationResult("Invalid contract kind.")
                : ValidationResult.Success;
        }
    }
}
