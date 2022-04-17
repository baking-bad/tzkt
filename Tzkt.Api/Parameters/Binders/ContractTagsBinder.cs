using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Tzkt.Api
{
    public class ContractTagsBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var model = bindingContext.ModelName;
            var hasValue = false;

            if (!bindingContext.TryGetContractTags($"{model}", ref hasValue, out var value))
                return Task.CompletedTask;

            if (!bindingContext.TryGetContractTags($"{model}.eq", ref hasValue, out var eq))
                return Task.CompletedTask;

            if (!bindingContext.TryGetContractTags($"{model}.any", ref hasValue, out var any))
                return Task.CompletedTask;

            if (!bindingContext.TryGetContractTags($"{model}.all", ref hasValue, out var all))
                return Task.CompletedTask;

            if (!hasValue)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(new ContractTagsParameter
            {
                Eq = value ?? eq,
                Any = any,
                All = all
            });

            return Task.CompletedTask;
        }
    }
}
