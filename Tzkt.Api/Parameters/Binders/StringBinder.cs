using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Tzkt.Api
{
    public class StringBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var model = bindingContext.ModelName;
            var hasValue = false;

            if (!bindingContext.TryGetString($"{model}", ref hasValue, out var value))
                return Task.CompletedTask;

            if (!bindingContext.TryGetString($"{model}.eq", ref hasValue, out var eq))
                return Task.CompletedTask;

            if (!bindingContext.TryGetString($"{model}.ne", ref hasValue, out var ne))
                return Task.CompletedTask;

            if (!bindingContext.TryGetString($"{model}.as", ref hasValue, out var @as))
                return Task.CompletedTask;

            if (!bindingContext.TryGetString($"{model}.un", ref hasValue, out var un))
                return Task.CompletedTask;

            if (!bindingContext.TryGetStringListEscaped($"{model}.in", ref hasValue, out var @in)) 
                return Task.CompletedTask;

            if (!bindingContext.TryGetStringListEscaped($"{model}.ni", ref hasValue, out var ni))
                return Task.CompletedTask;

            if (!bindingContext.TryGetBool($"{model}.null", ref hasValue, out var isNull))
                return Task.CompletedTask;

            if (!hasValue)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(new StringParameter
            {
                Eq = value ?? eq,
                Ne = ne,
                As = @as?
                    .Replace("%", "\\%")
                    .Replace("\\*", "ъуъ")
                    .Replace("*", "%")
                    .Replace("ъуъ", "*"),
                Un = un?
                    .Replace("%", "\\%")
                    .Replace("\\*", "ъуъ")
                    .Replace("*", "%")
                    .Replace("ъуъ", "*"),
                In = @in,
                Ni = ni,
                Null = isNull
            });

            return Task.CompletedTask;
        }
    }
}
