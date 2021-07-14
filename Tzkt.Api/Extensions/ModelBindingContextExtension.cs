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
                    result = value.DateTime;
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
                        result.Add(value.DateTime);
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

        public static bool TryGetProtocol(this ModelBindingContext bindingContext, string name, ref bool hasValue, out string result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (!Regex.IsMatch(valueObject.FirstValue, "^P[0-9A-z]{50}$"))
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
                        if (!Regex.IsMatch(rawValue, "^P[0-9A-z]{50}$"))
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

        public static bool TryGetBakingRightType(this ModelBindingContext bindingContext, string name, ref bool hasValue, out int? result)
        {
            result = null;
            var valueObject = bindingContext.ValueProvider.GetValue(name);

            if (valueObject != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(name, valueObject);
                if (!string.IsNullOrEmpty(valueObject.FirstValue))
                {
                    if (valueObject.FirstValue == "baking")
                    {
                        hasValue = true;
                        result = 0;
                    }
                    else if (valueObject.FirstValue == "endorsing")
                    {
                        hasValue = true;
                        result = 1;
                    }
                    else
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid baking right type.");
                        return false;
                    }
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
                    if (valueObject.FirstValue == "future")
                    {
                        hasValue = true;
                        result = 0;
                    }
                    else if (valueObject.FirstValue == "realized")
                    {
                        hasValue = true;
                        result = 1;
                    }
                    else if (valueObject.FirstValue == "uncovered")
                    {
                        hasValue = true;
                        result = 2;
                    }
                    else if (valueObject.FirstValue == "missed")
                    {
                        hasValue = true;
                        result = 3;
                    }
                    else
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid baking right status.");
                        return false;
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
                    if (valueObject.FirstValue == ContractKinds.Delegator)
                    {
                        hasValue = true;
                        result = 0;
                    }
                    else if (valueObject.FirstValue == ContractKinds.SmartContract)
                    {
                        hasValue = true;
                        result = 1;
                    }
                    else if (valueObject.FirstValue == ContractKinds.Asset)
                    {
                        hasValue = true;
                        result = 2;
                    }
                    else
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid contract kind.");
                        return false;
                    }
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
                        if (rawValue == ContractKinds.Asset)
                        {
                            hasValue = true;
                            result.Add(2);
                        }
                        else if (rawValue == ContractKinds.SmartContract)
                        {
                            hasValue = true;
                            result.Add(1);
                        }
                        else if (rawValue == ContractKinds.Delegator)
                        {
                            hasValue = true;
                            result.Add(0);
                        }
                        else
                        {
                            bindingContext.ModelState.TryAddModelError(name, "List contains invalid contract kind.");
                            return false;
                        }
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
                    switch (valueObject.FirstValue)
                    {
                        case BigMapActions.Allocate:
                            hasValue = true;
                            result = (int)Data.Models.BigMapAction.Allocate;
                            break;
                        case BigMapActions.AddKey:
                            hasValue = true;
                            result = (int)Data.Models.BigMapAction.AddKey;
                            break;
                        case BigMapActions.UpdateKey:
                            hasValue = true;
                            result = (int)Data.Models.BigMapAction.UpdateKey;
                            break;
                        case BigMapActions.RemoveKey:
                            hasValue = true;
                            result = (int)Data.Models.BigMapAction.RemoveKey;
                            break;
                        case BigMapActions.Remove:
                            hasValue = true;
                            result = (int)Data.Models.BigMapAction.Remove;
                            break;
                        default:
                            bindingContext.ModelState.TryAddModelError(name, "Invalid bigmap action.");
                            return false;
                    }
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
                        switch (rawValue)
                        {
                            case BigMapActions.Allocate:
                                hasValue = true;
                                result.Add((int)Data.Models.BigMapAction.Allocate);
                                break;
                            case BigMapActions.AddKey:
                                hasValue = true;
                                result.Add((int)Data.Models.BigMapAction.AddKey);
                                break;
                            case BigMapActions.UpdateKey:
                                hasValue = true;
                                result.Add((int)Data.Models.BigMapAction.UpdateKey);
                                break;
                            case BigMapActions.RemoveKey:
                                hasValue = true;
                                result.Add((int)Data.Models.BigMapAction.RemoveKey);
                                break;
                            case BigMapActions.Remove:
                                hasValue = true;
                                result.Add((int)Data.Models.BigMapAction.Remove);
                                break;
                            default:
                                bindingContext.ModelState.TryAddModelError(name, "List contains invalid bigmap action.");
                                return false;
                        }
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
                        switch (rawValue)
                        {
                            case BigMapTags.Metadata:
                                hasValue = true;
                                result |= (int)Data.Models.BigMapTag.Metadata;
                                break;
                            case BigMapTags.TokenMetadata:
                                hasValue = true;
                                result |= (int)Data.Models.BigMapTag.TokenMetadata;
                                break;
                            default:
                                bindingContext.ModelState.TryAddModelError(name, "Invalid bigmap tags.");
                                return false;
                        }
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
                    switch (valueObject.FirstValue)
                    {
                        case VoterStatuses.None:
                            hasValue = true;
                            result = (int)Data.Models.VoterStatus.None;
                            break;
                        case VoterStatuses.Upvoted:
                            hasValue = true;
                            result = (int)Data.Models.VoterStatus.Upvoted;
                            break;
                        case VoterStatuses.VotedYay:
                            hasValue = true;
                            result = (int)Data.Models.VoterStatus.VotedYay;
                            break;
                        case VoterStatuses.VotedNay:
                            hasValue = true;
                            result = (int)Data.Models.VoterStatus.VotedNay;
                            break;
                        case VoterStatuses.VotedPass:
                            hasValue = true;
                            result = (int)Data.Models.VoterStatus.VotedPass;
                            break;
                        default:
                            bindingContext.ModelState.TryAddModelError(name, "Invalid voter status.");
                            return false;
                    }
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
                        switch (rawValue)
                        {
                            case VoterStatuses.None:
                                hasValue = true;
                                result.Add((int)Data.Models.VoterStatus.None);
                                break;
                            case VoterStatuses.Upvoted:
                                hasValue = true;
                                result.Add((int)Data.Models.VoterStatus.Upvoted);
                                break;
                            case VoterStatuses.VotedYay:
                                hasValue = true;
                                result.Add((int)Data.Models.VoterStatus.VotedYay);
                                break;
                            case VoterStatuses.VotedNay:
                                hasValue = true;
                                result.Add((int)Data.Models.VoterStatus.VotedNay);
                                break;
                            case VoterStatuses.VotedPass:
                                hasValue = true;
                                result.Add((int)Data.Models.VoterStatus.VotedPass);
                                break;
                            default:
                                bindingContext.ModelState.TryAddModelError(name, "List contains invalid voter status.");
                                return false;
                        }
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
                    switch (valueObject.FirstValue)
                    {
                        case MigrationKinds.Bootstrap:
                            hasValue = true;
                            result = 0;
                            break;
                        case MigrationKinds.ActivateDelegate:
                            hasValue = true;
                            result = 1;
                            break;
                        case MigrationKinds.Airdrop:
                            hasValue = true;
                            result = 2;
                            break;
                        case MigrationKinds.ProposalInvoice:
                            hasValue = true;
                            result = 3;
                            break;
                        case MigrationKinds.CodeChange:
                            hasValue = true;
                            result = 4;
                            break;
                        case MigrationKinds.Origination:
                            hasValue = true;
                            result = 5;
                            break;
                        case MigrationKinds.Subsidy:
                            hasValue = true;
                            result = 6;
                            break;
                        default:
                            bindingContext.ModelState.TryAddModelError(name, "Invalid migration kind.");
                            return false;
                    }
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
                        switch (rawValue)
                        {
                            case MigrationKinds.Bootstrap:
                                hasValue = true;
                                result.Add(0);
                                break;
                            case MigrationKinds.ActivateDelegate:
                                hasValue = true;
                                result.Add(1);
                                break;
                            case MigrationKinds.Airdrop:
                                hasValue = true;
                                result.Add(2);
                                break;
                            case MigrationKinds.ProposalInvoice:
                                hasValue = true;
                                result.Add(3);
                                break;
                            case MigrationKinds.CodeChange:
                                hasValue = true;
                                result.Add(4);
                                break;
                            case MigrationKinds.Origination:
                                hasValue = true;
                                result.Add(5);
                                break;
                            case MigrationKinds.Subsidy:
                                hasValue = true;
                                result.Add(6);
                                break;
                            default:
                                bindingContext.ModelState.TryAddModelError(name, "List contains invalid migration kind.");
                                return false;
                        }
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
                    if (valueObject.FirstValue == "applied")
                    {
                        hasValue = true;
                        result = 1;
                    }
                    else if (valueObject.FirstValue == "failed")
                    {
                        hasValue = true;
                        result = 4;
                    }
                    else if (valueObject.FirstValue == "backtracked")
                    {
                        hasValue = true;
                        result = 2;
                    }
                    else if (valueObject.FirstValue == "skipped")
                    {
                        hasValue = true;
                        result = 3;
                    }
                    else
                    {
                        bindingContext.ModelState.TryAddModelError(name, "Invalid operation status.");
                        return false;
                    }
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
                        if (Regex.IsMatch(valueObject.FirstValue, @"^[\w,]+$"))
                        {
                            result = valueObject.FirstValue.Split(',').Select(x => NormalizeJson(x)).ToArray();
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
    }
}
