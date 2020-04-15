using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Tzkt.Api
{
    public class ProtocolBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var model = bindingContext.ModelName;
            var hasValue = false;

            if (!bindingContext.TryGetProtocol($"{model}", ref hasValue, out var value))
                return Task.CompletedTask;

            if (!bindingContext.TryGetProtocol($"{model}.eq", ref hasValue, out var eq))
                return Task.CompletedTask;

            if (!bindingContext.TryGetProtocol($"{model}.ne", ref hasValue, out var ne))
                return Task.CompletedTask;

            if (!bindingContext.TryGetProtocolList($"{model}.in", ref hasValue, out var @in))
                return Task.CompletedTask;

            if (!bindingContext.TryGetProtocolList($"{model}.ni", ref hasValue, out var ni))
                return Task.CompletedTask;

            if (!hasValue)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(new ProtocolParameter
            {
                Eq = value ?? eq,
                Ne = ne,
                In = @in,
                Ni = ni
            });

            return Task.CompletedTask;
        }
    }
}
