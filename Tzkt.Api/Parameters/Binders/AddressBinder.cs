using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Tzkt.Api
{
    public class AddressBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var model = bindingContext.ModelName;
            var hasValue = false;

            if (!bindingContext.TryGetAccount($"{model}", ref hasValue, out var value))
                return Task.CompletedTask;

            if (!bindingContext.TryGetAccount($"{model}.eq", ref hasValue, out var eq))
                return Task.CompletedTask;

            if (!bindingContext.TryGetAccount($"{model}.ne", ref hasValue, out var ne))
                return Task.CompletedTask;

            if (!bindingContext.TryGetAccountList($"{model}.in", ref hasValue, out var @in))
                return Task.CompletedTask;

            if (!bindingContext.TryGetAccountList($"{model}.ni", ref hasValue, out var ni))
                return Task.CompletedTask;

            if (!bindingContext.TryGetBool($"{model}.null", ref hasValue, out var isNull))
                return Task.CompletedTask;

            if (!hasValue)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(new AddressParameter
            {
                Eq = value ?? eq,
                Ne = ne,
                In = @in?.Where(x => x != null).ToList(),
                Ni = ni?.Where(x => x != null).ToList(),
                Null = isNull,
                InHasNull = @in?.Any(x => x == null) == true,
                NiHasNull = ni?.Any(x => x == null) == true
            });

            return Task.CompletedTask;
        }
    }
}
