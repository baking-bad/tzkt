using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Tzkt.Api
{
    public class UnstakeRequestStatusBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var model = bindingContext.ModelName;
            var hasValue = false;

            if (!bindingContext.TryGetUnstakeRequestStatus($"{model}", ref hasValue, out var value))
                return Task.CompletedTask;

            if (!bindingContext.TryGetUnstakeRequestStatus($"{model}.eq", ref hasValue, out var eq))
                return Task.CompletedTask;

            if (!bindingContext.TryGetUnstakeRequestStatus($"{model}.ne", ref hasValue, out var ne))
                return Task.CompletedTask;

            if (!hasValue)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(new UnstakeRequestStatusParameter
            {
                Eq = value ?? eq,
                Ne = ne,
            });

            return Task.CompletedTask;
        }
    }
}
