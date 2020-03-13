using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Tzkt.Api
{
    public class OffsetBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var model = bindingContext.ModelName;
            var hasValue = false;

            if (!bindingContext.TryGetInt32($"{model}", ref hasValue, out var value))
                return Task.CompletedTask;

            if (!bindingContext.TryGetInt32($"{model}.el", ref hasValue, out var el))
                return Task.CompletedTask;

            if (!bindingContext.TryGetInt32($"{model}.id", ref hasValue, out var id))
                return Task.CompletedTask;

            if (!bindingContext.TryGetInt32($"{model}.pg", ref hasValue, out var pg))
                return Task.CompletedTask;

            if (!hasValue)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            if (value != null && value < 0)
            {
                bindingContext.ModelState.AddModelError($"{model}", "The value must be greater than or equal to 0.");
                return Task.CompletedTask;
            }

            if (el != null && el < 0)
            {
                bindingContext.ModelState.AddModelError($"{model}.el", "The value must be greater than or equal to 0.");
                return Task.CompletedTask;
            }

            if (id != null && id < 0)
            {
                bindingContext.ModelState.AddModelError($"{model}.id", "The value must be greater than or equal to 0.");
                return Task.CompletedTask;
            }

            if (pg != null && pg < 0)
            {
                bindingContext.ModelState.AddModelError($"{model}.pg", "The value must be greater than or equal to 0.");
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(new OffsetParameter
            {
                El = value ?? el,
                Id = id,
                Pg = pg
            });

            return Task.CompletedTask;
        }
    }
}
