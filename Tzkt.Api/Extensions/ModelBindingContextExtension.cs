using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Netezos.Encoding;

namespace Tzkt.Api
{
    static class ModelBindingContextExtension
    {
        public static bool TryGetBigInteger(this ModelBindingContext bindingContext, string name, ref bool hasValue, out BigInteger? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!BigInteger.TryParse(valueObject.FirstValue, out var value))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid BigInteger value.");
                        return false;
                    }

                    hasValue = true;
                    result = value;
                }
            }

            return true;
        }

        public static bool TryGetBigIntegerList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<BigInteger>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    var rawValues = valueObject.FirstValue.Split(',', StringSplitOptions.RemoveEmptyEntries);

                    if (rawValues.Length == 0)
                    {
                        bindingContext.ModelState.TryAddModelError(name, "List should contain at least one item.");
                        return false;
                    }

                    hasValue = true;
                    result = new List<BigInteger>(rawValues.Length);

                    foreach (var rawValue in rawValues)
                    {
                        if (!BigInteger.TryParse(rawValue, out var value))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid BigInteger value.");
                            return false;
                        }
                        result.Add(value);
                    }
                }
            }

            return true;
        }

        public static bool TryGetNat(this ModelBindingContext bindingContext, string name, ref bool hasValue, out string? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!Regexes.Number().IsMatch(valueObject.FirstValue))
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

        public static bool TryGetNatList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<string>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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
                        if (!Regexes.Number().IsMatch(rawValue))
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

        public static bool TryGetInt32List(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<int>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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

        public static bool TryGetInt64List(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<long>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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

        public static bool TryGetMicheline(this ModelBindingContext bindingContext, string name, ref bool hasValue, out IMicheline? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    try
                    {
                        result = Micheline.FromJson(valueObject.FirstValue);
                        hasValue = true;
                    }
                    catch
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid Micheline value.");
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool TryGetMichelineList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<IMicheline>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    try
                    {
                        var json = valueObject.FirstValue.Trim();
                        if (json[0] != '[') json = $"[{json}]";
                        var values = JsonSerializer.Deserialize<List<IMicheline>>(json) ?? [];

                        if (values.Count == 0)
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List should contain at least one item.");
                            return false;
                        }

                        hasValue = true;
                        result = values;
                    }
                    catch
                    {
                        bindingContext.ModelState.TryAddModelError(name, "List contains invalid Micheline value(s).");
                        return false;
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

        public static bool TryGetDateTimeList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<DateTime>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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

        public static bool TryGetAddress(this ModelBindingContext bindingContext, string name, ref bool hasValue, out string? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!Regexes.Address().IsMatch(valueObject.FirstValue))
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

        public static bool TryGetAddressList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<string>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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
                        if (!Regexes.Address().IsMatch(rawValue))
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

        public static bool TryGetAddressNullList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<string?>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    var rawValues = valueObject.FirstValue.Split(',', StringSplitOptions.RemoveEmptyEntries);

                    if (rawValues.Length == 0)
                    {
                        bindingContext.ModelState.TryAddModelError(name, "List should contain at least one item.");
                        return false;
                    }

                    hasValue = true;
                    result = new(rawValues.Length);

                    foreach (var rawValue in rawValues)
                    {
                        if (!Regexes.Address().IsMatch(rawValue))
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

        public static bool TryGetSr1Address(this ModelBindingContext bindingContext, string name, ref bool hasValue, out string? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!Regexes.Sr1Address().IsMatch(valueObject.FirstValue))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid smart rollup address.");
                        return false;
                    }

                    hasValue = true;
                    result = valueObject.FirstValue;
                }
            }

            return true;
        }

        public static bool TryGetSr1AddressList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<string>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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
                        if (!Regexes.Sr1Address().IsMatch(rawValue))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid smart rollup address.");
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

        public static bool TryGetProtocol(this ModelBindingContext bindingContext, string name, ref bool hasValue, out string? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!Regexes.Protocol().IsMatch(valueObject.FirstValue))
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

        public static bool TryGetProtocolList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<string>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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
                        if (!Regexes.Protocol().IsMatch(rawValue))
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


        public static bool TryGetTokenGlobalId(this ModelBindingContext bindingContext, string name, ref bool hasValue, out (string, BigInteger)? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    var ss = valueObject.FirstValue.Split(':', StringSplitOptions.RemoveEmptyEntries);
                    if (ss.Length != 2 || !Regexes.Address().IsMatch(ss[0]) || !BigInteger.TryParse(ss[1], out var tokenId))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid token global id.");
                        return false;
                    }

                    hasValue = true;
                    result = (ss[0], tokenId);
                }
            }

            return true;
        }

        public static bool TryGetTokenGlobalIdList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<(string, BigInteger)>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    var rawValues = valueObject.FirstValue.Split(',', StringSplitOptions.RemoveEmptyEntries);

                    if (rawValues.Length == 0)
                    {
                        bindingContext.ModelState.TryAddModelError(name, "List should contain at least one item.");
                        return false;
                    }

                    hasValue = true;
                    result = new List<(string, BigInteger)>(rawValues.Length);

                    foreach (var rawValue in rawValues)
                    {
                        var ss = rawValue.Split(':', StringSplitOptions.RemoveEmptyEntries);
                        if (ss.Length != 2 || !Regexes.Address().IsMatch(ss[0]) || !BigInteger.TryParse(ss[1], out var tokenId))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid token global id.");
                            return false;
                        }
                        else
                        {
                            result.Add((ss[0], tokenId));
                        }
                    }
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

        public static bool TryGetExpression(this ModelBindingContext bindingContext, string name, ref bool hasValue, out string? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!Regexes.Expression().IsMatch(valueObject.FirstValue))
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

        public static bool TryGetExpressionList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<string>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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
                        if (!Regexes.Expression().IsMatch(rawValue))
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

        public static bool TryGetOpHash(this ModelBindingContext bindingContext, string name, ref bool hasValue, out string? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!Regexes.Operation().IsMatch(valueObject.FirstValue))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid operation hash.");
                        return false;
                    }

                    hasValue = true;
                    result = valueObject.FirstValue;
                }
            }

            return true;
        }

        public static bool TryGetOpHashList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<string>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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
                        if (!Regexes.Operation().IsMatch(rawValue))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid operation hash.");
                            return false;
                        }

                        result.Add(rawValue);
                    }
                }
            }

            return true;
        }

        public static bool TryGetSrc1Hash(this ModelBindingContext bindingContext, string name, ref bool hasValue, out string? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!Regexes.Src1Hash().IsMatch(valueObject.FirstValue))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid smart rollup commitment hash.");
                        return false;
                    }

                    hasValue = true;
                    result = valueObject.FirstValue;
                }
            }

            return true;
        }

        public static bool TryGetSrc1HashList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<string>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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
                        if (!Regexes.Src1Hash().IsMatch(rawValue))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid smart rollup commitment hash.");
                            return false;
                        }

                        result.Add(rawValue);
                    }
                }
            }

            return true;
        }

        public static bool TryGetEpochStatus(this ModelBindingContext bindingContext, string name, ref bool hasValue, out string? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!EpochStatuses.IsValid(valueObject.FirstValue))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid epoch status.");
                        return false;
                    }
                    hasValue = true;
                    result = valueObject.FirstValue;
                }
            }

            return true;
        }

        public static bool TryGetEpochStatusList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<string>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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
                        if (!EpochStatuses.IsValid(rawValue))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid epoch status.");
                            return false;
                        }
                        hasValue = true;
                        result.Add(rawValue);
                    }
                }
            }

            return true;
        }

        public static bool TryGetSecondaryKeyType(this ModelBindingContext bindingContext, string name, ref bool hasValue, out int? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!SecondaryKeyTypes.TryParse(valueObject.FirstValue, out var kind))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid secondary key type.");
                        return false;
                    }
                    hasValue = true;
                    result = kind;
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

        public static bool TryGetContractKindList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<int>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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

        public static bool TryGetBigMapActionList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<int>? result)
        {
            result = null;
            var valueObject = (bindingContext.ValueProvider as CompositeValueProvider)?
                .FirstOrDefault(x => x is QueryStringValueProvider)?
                .GetValue(name) ?? ValueProviderResult.None;

            if (valueObject != ValueProviderResult.None)
            {
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

        public static bool TryGetVotesList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<int>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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

        public static bool TryGetVoterStatusList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<int>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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

        public static bool TryGetRefutationMove(this ModelBindingContext bindingContext, string name, ref bool hasValue, out int? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!RefutationMoves.TryParse(valueObject.FirstValue, out var status))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid refutation game move.");
                        return false;
                    }
                    hasValue = true;
                    result = status;
                }
            }

            return true;
        }

        public static bool TryGetRefutationMoveList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<int>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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
                        if (!RefutationMoves.TryParse(rawValue, out var status))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid refutation game move.");
                            return false;
                        }
                        hasValue = true;
                        result.Add(status);
                    }
                }
            }

            return true;
        }

        public static bool TryGetRefutationGameStatus(this ModelBindingContext bindingContext, string name, ref bool hasValue, out int? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!RefutationGameStatuses.TryParse(valueObject.FirstValue, out var status))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid refutation game status.");
                        return false;
                    }
                    hasValue = true;
                    result = status;
                }
            }

            return true;
        }

        public static bool TryGetRefutationGameStatusList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<int>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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
                        if (!RefutationGameStatuses.TryParse(rawValue, out var status))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid refutation game status.");
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

        public static bool TryGetMigrationKindList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<int>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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

        public static bool TryGetUnstakeRequestStatus(this ModelBindingContext bindingContext, string name, ref bool hasValue, out string? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!UnstakeRequestStatuses.TryParse(valueObject.FirstValue, out var status))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid unstake request status.");
                        return false;
                    }
                    hasValue = true;
                    result = status;
                }
            }

            return true;
        }

        public static bool TryGetSrCommitmentStatus(this ModelBindingContext bindingContext, string name, ref bool hasValue, out int? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!SrCommitmentStatuses.TryParse(valueObject.FirstValue, out var status))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid smart rollup commitment status.");
                        return false;
                    }
                    hasValue = true;
                    result = status;
                }
            }

            return true;
        }

        public static bool TryGetSrCommitmentStatusList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<int>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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
                        if (!SrCommitmentStatuses.TryParse(rawValue, out var status))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid smart rollup commitment status.");
                            return false;
                        }
                        hasValue = true;
                        result.Add(status);
                    }
                }
            }

            return true;
        }

        public static bool TryGetSrBondStatus(this ModelBindingContext bindingContext, string name, ref bool hasValue, out int? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!SrBondStatuses.TryParse(valueObject.FirstValue, out var status))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid smart rollup bond status.");
                        return false;
                    }
                    hasValue = true;
                    result = status;
                }
            }

            return true;
        }

        public static bool TryGetSrBondStatusList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<int>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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
                        if (!SrBondStatuses.TryParse(rawValue, out var status))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid smart rollup bond status.");
                            return false;
                        }
                        hasValue = true;
                        result.Add(status);
                    }
                }
            }

            return true;
        }

        public static bool TryGetStakingAction(this ModelBindingContext bindingContext, string name, ref bool hasValue, out int? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!StakingActions.TryParse(valueObject.FirstValue, out var value))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid staking action.");
                        return false;
                    }
                    hasValue = true;
                    result = value;
                }
            }

            return true;
        }

        public static bool TryGetStakingActionsList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<int>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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
                        if (!StakingActions.TryParse(rawValue, out var value))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid staking action.");
                            return false;
                        }
                        hasValue = true;
                        result.Add(value);
                    }
                }
            }

            return true;
        }

        public static bool TryGetStakingUpdateType(this ModelBindingContext bindingContext, string name, ref bool hasValue, out int? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!StakingUpdateTypes.TryParse(valueObject.FirstValue, out var value))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid staking update type.");
                        return false;
                    }
                    hasValue = true;
                    result = value;
                }
            }

            return true;
        }

        public static bool TryGetStakingUpdateTypesList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<int>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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
                        if (!StakingUpdateTypes.TryParse(rawValue, out var value))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid staking update type.");
                            return false;
                        }
                        hasValue = true;
                        result.Add(value);
                    }
                }
            }

            return true;
        }

        public static bool TryGetSrMessageType(this ModelBindingContext bindingContext, string name, ref bool hasValue, out int? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!SrMessageTypes.TryParse(valueObject.FirstValue, out var status))
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid inbox message type.");
                        return false;
                    }
                    hasValue = true;
                    result = status;
                }
            }

            return true;
        }

        public static bool TryGetSrMessageTypeList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<int>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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
                        if (!SrMessageTypes.TryParse(rawValue, out var status))
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid inbox message type.");
                            return false;
                        }
                        hasValue = true;
                        result.Add(status);
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
                result = !(valueObject.FirstValue == "false" || valueObject.FirstValue == "0");
                hasValue = true;
            }

            return true;
        }

        public static bool TryGetString(this ModelBindingContext bindingContext, string name, [NotNullWhen(true)] out string? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);
            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    result = valueObject.FirstValue;
                    return true;
                }
            }
            bindingContext.ModelState.TryAddModelError(name, "Invalid value.");
            return false;
        }

        public static bool TryGetJson(this ModelBindingContext bindingContext, string name, [NotNullWhen(true)] out string? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);
            if (valueObject != ValueProviderResult.None)
            {
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

        public static bool TryGetJsonArray(this ModelBindingContext bindingContext, string name, [NotNullWhen(true)] out string[]? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);
            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    try
                    {
                        if (Regexes.CommaSeparatedWords().IsMatch(valueObject.FirstValue))
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

        public static bool TryGetString(this ModelBindingContext bindingContext, string name, ref bool hasValue, out string? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    hasValue = true;
                    result = valueObject.FirstValue;
                }
            }

            return true;
        }

        public static bool TryGetStringList(this ModelBindingContext bindingContext, string name, ref bool hasValue, out string[]? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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

        public static bool TryGetStringListEscaped(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<string>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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

        public static bool TryGetSelectionFields(this ModelBindingContext bindingContext, string name, ref bool hasValue, out List<SelectionField>? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
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
