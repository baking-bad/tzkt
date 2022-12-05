using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Tzkt.Api
{
    static class ModelBindingContextExtension
    {
        public static bool TryGetNat(this ModelBindingContext bindingContext, string name, ref bool hasValue, out string result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!Regex.IsMatch(valueObject.FirstValue, @"^[0-9]+$"))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid nat value.");
                        return false;
                    }

                    hasValue = true;
                    result = valueObject.FirstValue;
                }
            }

            return true;
        }

        public static bool TryGetNatList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<string> result)
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
                        if (!Regex.IsMatch(rawValue, @"^[0-9]+$"))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid nat value.");
                            return false;
                        }
                        result.Add(rawValue);
                    }
                }
            }

            return true;
        }

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

        public static bool TryGetDateTime(this ModelBindingContext bindingContext, string name, ref bool hasValue, out DateTime? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!DateTimeOffset.TryParse(valueObject.FirstValue, out var value))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid datetime value.");
                        return false;
                    }

                    hasValue = true;
                    result = value.UtcDateTime;
                }
            }

            return true;
        }

        public static bool TryGetDateTimeList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<DateTime> result)
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
                    result = new List<DateTime>(rawValues.Length);

                    foreach (var rawValue in rawValues)
                    {
                        if (!DateTimeOffset.TryParse(rawValue, out var value))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid datetime value.");
                            return false;
                        }
                        result.Add(value.UtcDateTime);
                    }
                }
            }

            return true;
        }

        public static bool TryGetAddress(this ModelBindingContext bindingContext, string name, ref bool hasValue, out string result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!Regex.IsMatch(valueObject.FirstValue, "^[0-9A-Za-z]{36,37}$"))
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

        public static bool TryGetAddressList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<string> result)
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
                        if (!Regex.IsMatch(rawValue, "^[0-9A-Za-z]{36,37}$"))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid account address.");
                            return false;
                        }
                        else
                        {
                            result.Add(rawValue);
                        }
                    }
                }
            }

            return true;
        }

        public static bool TryGetAddressNullList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<string> result)
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
                        if (!Regex.IsMatch(rawValue, "^[0-9A-Za-z]{36,37}$"))
                        {
                            if (rawValue != "null")
                            {
                                bindingContext.ModelState.TryAddModelError(name, "List contains invalid account address.");
                                return false;
                            }
                            else
                            {
                                result.Add(null);
                            }
                        }
                        else
                        {
                            result.Add(rawValue);
                        }
                    }
                }
            }

            return true;
        }

        public static bool TryGetProtocol(this ModelBindingContext bindingContext, string name, ref bool hasValue, out string result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!Regex.IsMatch(valueObject.FirstValue, "^P[0-9A-Za-z]{50}$"))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid protocol hash.");
                        return false;
                    }

                    hasValue = true;
                    result = valueObject.FirstValue;
                }
            }

            return true;
        }

        public static bool TryGetProtocolList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<string> result)
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
                        if (!Regex.IsMatch(rawValue, "^P[0-9A-Za-z]{50}$"))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid protocol hash.");
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
                    if (!AccountTypes.TryParse(valueObject.FirstValue, out var type))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid account type.");
                        return false;
                    }
                    hasValue = true;
                    result = type;
                }
            }

            return true;
        }
        
        public static bool TryGetTokenStandard(this ModelBindingContext bindingContext, string name, ref bool hasValue, out int? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!TokenStandards.TryParse(valueObject.FirstValue, out var standard))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid token standard value.");
                        return false;
                    }
                    hasValue = true;
                    result = standard;
                }
            }

            return true;
        }

        public static bool TryGetBakingRightType(this ModelBindingContext bindingContext, string name, ref bool hasValue, out int? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!BakingRightTypes.TryParse(valueObject.FirstValue, out var type))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid baking right type.");
                        return false;
                    }
                    hasValue = true;
                    result = type;
                }
            }

            return true;
        }

        public static bool TryGetBakingRightStatus(this ModelBindingContext bindingContext, string name, ref bool hasValue, out int? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!BakingRightStatuses.TryParse(valueObject.FirstValue, out var status))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid baking right status.");
                        return false;
                    }
                    hasValue = true;
                    result = status;
                }
            }

            return true;
        }

        public static bool TryGetExpression(this ModelBindingContext bindingContext, string name, ref bool hasValue, out string result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!Regex.IsMatch(valueObject.FirstValue, "^expr[0-9A-Za-z]{50}$"))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid expression hash.");
                        return false;
                    }

                    hasValue = true;
                    result = valueObject.FirstValue;
                }
            }

            return true;
        }

        public static bool TryGetExpressionList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<string> result)
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
                        if (!Regex.IsMatch(rawValue, "^expr[0-9A-Za-z]{50}$"))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid expression hash.");
                            return false;
                        }

                        result.Add(rawValue);
                    }
                }
            }

            return true;
        }

        public static bool TryGetContractKind(this ModelBindingContext bindingContext, string name, ref bool hasValue, out int? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!ContractKinds.TryParse(valueObject.FirstValue, out var kind))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid contract kind.");
                        return false;
                    }
                    hasValue = true;
                    result = kind;
                }
            }

            return true;
        }

        public static bool TryGetContractKindList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<int> result)
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
                        if (!ContractKinds.TryParse(rawValue, out var kind))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid contract kind.");
                            return false;
                        }
                        hasValue = true;
                        result.Add(kind);
                    }
                }
            }

            return true;
        }

        public static bool TryGetBigMapAction(this ModelBindingContext bindingContext, string name, ref bool hasValue, out int? result)
        {
            result = null;
            var valueObject = (bindingContext.ValueProvider as CompositeValueProvider)?
                .FirstOrDefault(x => x is QueryStringValueProvider)?
                .GetValue(name) ?? ValueProviderResult.None;
            
            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!BigMapActions.TryParse(valueObject.FirstValue, out var action))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid bigmap action.");
                        return false;
                    }
                    hasValue = true;
                    result = action;
                }
            }

            return true;
        }

        public static bool TryGetBigMapActionList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<int> result)
        {
            result = null;
            var valueObject = (bindingContext.ValueProvider as CompositeValueProvider)?
                .FirstOrDefault(x => x is QueryStringValueProvider)?
                .GetValue(name) ?? ValueProviderResult.None;

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
                        if (!BigMapActions.TryParse(rawValue, out var action))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid bigmap action.");
                            return false;
                        }
                        hasValue = true;
                        result.Add(action);
                    }
                }
            }

            return true;
        }

        public static bool TryGetContractTags(this ModelBindingContext bindingContext, string name, ref bool hasValue, out int? result)
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
                    result = (int)Data.Models.ContractTags.None;

                    foreach (var rawValue in rawValues)
                    {
                        if (!ContractTags.TryParse(rawValue, out var tag))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "Invalid contract tags.");
                            return false;
                        }
                        hasValue = true;
                        result |= tag;
                    }


                }
            }

            return true;
        }

        public static bool TryGetBigMapTags(this ModelBindingContext bindingContext, string name, ref bool hasValue, out int? result)
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
                    result = (int)Data.Models.BigMapTag.None;

                    foreach (var rawValue in rawValues)
                    {
                        if (!BigMapTags.TryParse(rawValue, out var tag))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "Invalid bigmap tags.");
                            return false;
                        }
                        hasValue = true;
                        result |= tag;
                    }

                    
                }
            }

            return true;
        }

        public static bool TryGetVote(this ModelBindingContext bindingContext, string name, ref bool hasValue, out int? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!Votes.TryParse(valueObject.FirstValue, out var vote))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid vote.");
                        return false;
                    }
                    hasValue = true;
                    result = vote;
                }
            }

            return true;
        }

        public static bool TryGetVotesList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<int> result)
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
                        if (!Votes.TryParse(rawValue, out var status))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid vote.");
                            return false;
                        }
                        hasValue = true;
                        result.Add(status);
                    }
                }
            }

            return true;
        }

        public static bool TryGetVoterStatus(this ModelBindingContext bindingContext, string name, ref bool hasValue, out int? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!VoterStatuses.TryParse(valueObject.FirstValue, out var status))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid voter status.");
                        return false;
                    }
                    hasValue = true;
                    result = status;
                }
            }

            return true;
        }

        public static bool TryGetVoterStatusList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<int> result)
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
                        if (!VoterStatuses.TryParse(rawValue, out var status))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid voter status.");
                            return false;
                        }
                        hasValue = true;
                        result.Add(status);
                    }
                }
            }

            return true;
        }

        public static bool TryGetMigrationKind(this ModelBindingContext bindingContext, string name, ref bool hasValue, out int? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!MigrationKinds.TryParse(valueObject.FirstValue, out var kind))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid migration kind.");
                        return false;
                    }
                    hasValue = true;
                    result = kind;
                }
            }

            return true;
        }

        public static bool TryGetMigrationKindList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<int> result)
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
                        if (!MigrationKinds.TryParse(rawValue, out var kind))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid migration kind.");
                            return false;
                        }
                        hasValue = true;
                        result.Add(kind);
                    }
                }
            }

            return true;
        }

        public static bool TryGetOperationStatus(this ModelBindingContext bindingContext, string name, ref bool hasValue, out int? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!OpStatuses.TryParse(valueObject.FirstValue, out var status))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid operation status.");
                        return false;
                    }
                    hasValue = true;
                    result = status;
                }
            }

            return true;
        }

        public static bool TryGetBool(this ModelBindingContext bindingContext, string name, out bool? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);
            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                result = !(valueObject.FirstValue == "false" || valueObject.FirstValue == "0");
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

        public static bool TryGetString(this ModelBindingContext bindingContext, string name, out string result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);
            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    result = valueObject.FirstValue;
                    return true;
                }
            }
            bindingContext.ModelState.TryAddModelError(name, "Invalid value.");
            return false;
        }

        public static bool TryGetJson(this ModelBindingContext bindingContext, string name, out string result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);
            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    try
                    {
                        var json = NormalizeJson(valueObject.FirstValue);
                        using var doc = JsonDocument.Parse(json);
                        result = json;
                        return true;
                    }
                    catch (JsonException) { }
                }
            }
            bindingContext.ModelState.TryAddModelError(name, "Invalid JSON value.");
            return false;
        }

        public static bool TryGetJsonArray(this ModelBindingContext bindingContext, string name, out string[] result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);
            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    try
                    {
                        if (Regex.IsMatch(valueObject.FirstValue, @"^[\w\s,]+$"))
                        {
                            result = valueObject.FirstValue.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => NormalizeJson(x)).ToArray();
                        }
                        else
                        {
                            using var doc = JsonDocument.Parse(valueObject.FirstValue);
                            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                            {
                                bindingContext.ModelState.TryAddModelError(name, "Invalid JSON array.");
                                return false;
                            }
                            result = doc.RootElement.EnumerateArray().Select(x => NormalizeJson(x.GetRawText())).ToArray();
                        }
                        if (result.Length < 2)
                        {
                            bindingContext.ModelState.TryAddModelError(name, "JSON array must contain at least two items.");
                            return false;
                        }
                        return true;
                    }
                    catch (JsonException) { }
                }
            }
            bindingContext.ModelState.TryAddModelError(name, "Invalid JSON array.");
            return false;
        }

        static string NormalizeJson(string value)
        {
            switch (value[0])
            {
                case '{':
                case '[':
                case '"':
                case 't' when value == "true":
                case 'f' when value == "false":
                case 'n' when value == "null":
                    return value;
                default:
                    return $"\"{value}\"";
            }
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

        public static bool TryGetStringList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out string[] result)
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
                    result = rawValues;
                }
            }

            return true;
        }

        public static bool TryGetStringListEscaped(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<string> result)
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

        public static bool TryGetSelectionFields(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<SelectionField> result)
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

                    var res = new List<SelectionField>(rawValues.Length);
                    foreach (var rawValue in rawValues)
                    {
                        if (!SelectionField.TryParse(rawValue, out var field))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid value.");
                            return false;
                        }
                        res.Add(field);
                    }

                    hasValue = true;
                    result = res;
                }
            }

            return true;
        }
    }
}
