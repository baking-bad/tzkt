using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Tzkt.Api
{
    static class ModelBindingContextExtension
    {
        public static bool TryGetInt32(this ModelBindingContext bindingContext, string name, ref bool hasValue, out int? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!int.TryParse(valueObject.FirstValue, out var value))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid integer value.");
                        return false;
                    }

                    hasValue = true;
                    result = value;
                }
            }

            return true;
        }

        public static bool TryGetInt32List(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<int> result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    var rawValues = valueObject.FirstValue.Split(',', StringSplitOptions.RemoveEmptyEntries);

                    if (rawValues.Length == 0)
                    {
                        bindingContext.ModelState.TryAddModelError(name, "List should contain at least one item.");
                        return false;
                    }

                    hasValue = true;
                    result = new List<int>(rawValues.Length);

                    foreach (var rawValue in rawValues)
                    {
                        if (!int.TryParse(rawValue, out var value))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid integer value.");
                            return false;
                        }
                        result.Add(value);
                    }
                }
            }

            return true;
        }

        public static bool TryGetInt64(this ModelBindingContext bindingContext, string name, ref bool hasValue, out long? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!long.TryParse(valueObject.FirstValue, out var value))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid integer value.");
                        return false;
                    }

                    hasValue = true;
                    result = value;
                }
            }

            return true;
        }

        public static bool TryGetInt64List(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<long> result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    var rawValues = valueObject.FirstValue.Split(',', StringSplitOptions.RemoveEmptyEntries);

                    if (rawValues.Length == 0)
                    {
                        bindingContext.ModelState.TryAddModelError(name, "List should contain at least one item.");
                        return false;
                    }

                    hasValue = true;
                    result = new List<long>(rawValues.Length);

                    foreach (var rawValue in rawValues)
                    {
                        if (!long.TryParse(rawValue, out var value))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid integer value.");
                            return false;
                        }
                        result.Add(value);
                    }
                }
            }

            return true;
        }

        public static bool TryGetAccount(this ModelBindingContext bindingContext, string name, ref bool hasValue, out string result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!Regex.IsMatch(valueObject.FirstValue, "^[0-9A-z]{36}$"))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid account address.");
                        return false;
                    }

                    hasValue = true;
                    result = valueObject.FirstValue;
                }
            }

            return true;
        }

        public static bool TryGetAccountList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<string> result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    var rawValues = valueObject.FirstValue.Split(',', StringSplitOptions.RemoveEmptyEntries);

                    if (rawValues.Length == 0)
                    {
                        bindingContext.ModelState.TryAddModelError(name, "List should contain at least one item.");
                        return false;
                    }

                    hasValue = true;
                    result = new List<string>(rawValues.Length);

                    foreach (var rawValue in rawValues)
                    {
                        if (!Regex.IsMatch(rawValue, "^[0-9A-z]{36}$"))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid account address.");
                            return false;
                        }

                        result.Add(rawValue);
                    }
                }
            }

            return true;
        }

        public static bool TryGetAccountType(this ModelBindingContext bindingContext, string name, ref bool hasValue, out int? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (valueObject.FirstValue == AccountTypes.User)
                    {
                        hasValue = true;
                        result = 0;
                    }
                    else if (valueObject.FirstValue == AccountTypes.Delegate)
                    {
                        hasValue = true;
                        result = 1;
                    }
                    else if (valueObject.FirstValue == AccountTypes.Contract)
                    {
                        hasValue = true;
                        result = 2;
                    }
                    else
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid account type.");
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool TryGetBool(this ModelBindingContext bindingContext, string name, ref bool hasValue, out bool? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                result = !(valueObject.FirstValue == "false" || valueObject.FirstValue == "0");
                hasValue = true;
            }

            return true;
        }

        public static bool TryGetString(this ModelBindingContext bindingContext, string name, ref bool hasValue, out string result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    hasValue = true;
                    result = valueObject.FirstValue;
                }
            }

            return true;
        }

        public static bool TryGetStringList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<string> result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    var rawValues = valueObject.FirstValue
                        .Replace("\\,", "ъуъ")
                        .Split(',', StringSplitOptions.RemoveEmptyEntries);

                    if (rawValues.Length == 0)
                    {
                        bindingContext.ModelState.TryAddModelError(name, "List should contain at least one item.");
                        return false;
                    }

                    hasValue = true;
                    result = new List<string>(rawValues.Length);

                    foreach (var rawValue in rawValues)
                        result.Add(rawValue.Replace("ъуъ", ","));
                }
            }

            return true;
        }
    }
}
