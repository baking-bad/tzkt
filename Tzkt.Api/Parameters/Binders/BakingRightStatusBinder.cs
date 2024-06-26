﻿using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Tzkt.Api
{
    public class BakingRightStatusBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var model = bindingContext.ModelName;
            var hasValue = false;

            if (!bindingContext.TryGetBakingRightStatus($"{model}", ref hasValue, out var value))
                return Task.CompletedTask;

            if (!bindingContext.TryGetBakingRightStatus($"{model}.eq", ref hasValue, out var eq))
                return Task.CompletedTask;

            if (!bindingContext.TryGetBakingRightStatus($"{model}.ne", ref hasValue, out var ne))
                return Task.CompletedTask;

            if (!hasValue)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(new BakingRightStatusParameter
            {
                Eq = value ?? eq,
                Ne = ne
            });

            return Task.CompletedTask;
        }
    }
}
