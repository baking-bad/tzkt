using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Tzkt.Api
{
    public class DoubleConsensusKindBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var model = bindingContext.ModelName;
            var hasValue = false;

            if (!bindingContext.TryGetDoubleConsensusKind($"{model}", ref hasValue, out var value))
                return Task.CompletedTask;

            if (!bindingContext.TryGetDoubleConsensusKind($"{model}.eq", ref hasValue, out var eq))
                return Task.CompletedTask;

            if (!bindingContext.TryGetDoubleConsensusKind($"{model}.ne", ref hasValue, out var ne))
                return Task.CompletedTask;

            if (!hasValue)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(new DoubleConsensusKindParameter
            {
                Eq = value ?? eq,
                Ne = ne
            });

            return Task.CompletedTask;
        }
    }
}
