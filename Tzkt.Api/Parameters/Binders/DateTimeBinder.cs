using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Tzkt.Api
{
    public class DateTimeBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var model = bindingContext.ModelName;
            var hasValue = false;

            if (!bindingContext.TryGetDateTime($"{model}", ref hasValue, out var value))
                return Task.CompletedTask;

            if (!bindingContext.TryGetDateTime($"{model}.eq", ref hasValue, out var eq))
                return Task.CompletedTask;

            if (!bindingContext.TryGetDateTime($"{model}.ne", ref hasValue, out var ne))
                return Task.CompletedTask;

            if (!bindingContext.TryGetDateTime($"{model}.gt", ref hasValue, out var gt))
                return Task.CompletedTask;

            if (!bindingContext.TryGetDateTime($"{model}.ge", ref hasValue, out var ge))
                return Task.CompletedTask;

            if (!bindingContext.TryGetDateTime($"{model}.lt", ref hasValue, out var lt))
                return Task.CompletedTask;

            if (!bindingContext.TryGetDateTime($"{model}.le", ref hasValue, out var le))
                return Task.CompletedTask;

            if (!bindingContext.TryGetDateTimeList($"{model}.in", ref hasValue, out var @in))
                return Task.CompletedTask;

            if (!bindingContext.TryGetDateTimeList($"{model}.ni", ref hasValue, out var ni))
                return Task.CompletedTask;

            if (!hasValue)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(new DateTimeParameter
            {
                Eq = value ?? eq,
                Ne = ne,
                Gt = gt,
                Ge = ge,
                Lt = lt,
                Le = le,
                In = @in,
                Ni = ni
            });

            return Task.CompletedTask;
        }
    }
}
