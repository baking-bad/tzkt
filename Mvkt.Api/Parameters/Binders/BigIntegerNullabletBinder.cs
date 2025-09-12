using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Mvkt.Api
{
    public class BigIntegerNullabletBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var model = bindingContext.ModelName;
            var hasValue = false;

            if (!bindingContext.TryGetBigInteger($"{model}", ref hasValue, out var value))
                return Task.CompletedTask;

            if (!bindingContext.TryGetBigInteger($"{model}.eq", ref hasValue, out var eq))
                return Task.CompletedTask;

            if (!bindingContext.TryGetBigInteger($"{model}.ne", ref hasValue, out var ne))
                return Task.CompletedTask;

            if (!bindingContext.TryGetBigInteger($"{model}.gt", ref hasValue, out var gt))
                return Task.CompletedTask;

            if (!bindingContext.TryGetBigInteger($"{model}.ge", ref hasValue, out var ge))
                return Task.CompletedTask;

            if (!bindingContext.TryGetBigInteger($"{model}.lt", ref hasValue, out var lt))
                return Task.CompletedTask;

            if (!bindingContext.TryGetBigInteger($"{model}.le", ref hasValue, out var le))
                return Task.CompletedTask;

            if (!bindingContext.TryGetBigIntegerList($"{model}.in", ref hasValue, out var @in))
                return Task.CompletedTask;

            if (!bindingContext.TryGetBigIntegerList($"{model}.ni", ref hasValue, out var ni))
                return Task.CompletedTask;

            if (!bindingContext.TryGetBool($"{model}.null", ref hasValue, out var isNull))
                return Task.CompletedTask;

            if (!hasValue)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(new BigIntegerNullableParameter
            {
                Eq = value ?? eq,
                Ne = ne,
                Gt = gt,
                Ge = ge,
                Lt = lt,
                Le = le,
                In = @in,
                Ni = ni,
                Null = isNull
            });

            return Task.CompletedTask;
        }
    }
}
