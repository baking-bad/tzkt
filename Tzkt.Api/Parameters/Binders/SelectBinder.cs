﻿using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Tzkt.Api
{
    public class SelectBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var model = bindingContext.ModelName;
            var hasValue = false;

            if (!bindingContext.TryGetStringList($"{model}", ref hasValue, out var value))
                return Task.CompletedTask;

            if (!bindingContext.TryGetStringList($"{model}.fields", ref hasValue, out var fields))
                return Task.CompletedTask;

            if (!bindingContext.TryGetStringList($"{model}.values", ref hasValue, out var values))
                return Task.CompletedTask;

            if (!hasValue)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(new SelectParameter
            {
                Fields = value ?? fields,
                Values = values
            });

            return Task.CompletedTask;
        }
    }
}
