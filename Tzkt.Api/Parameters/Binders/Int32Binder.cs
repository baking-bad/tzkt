using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Tzkt.Api
{
    public class Int32Binder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var model = bindingContext.ModelName;
            var hasValue = false;

            if (!bindingContext.TryGetInt32($"{model}", ref hasValue, out var value))
                return Task.CompletedTask;

            if (!bindingContext.TryGetInt32($"{model}.eq", ref hasValue, out var eq))
                return Task.CompletedTask;

            if (!bindingContext.TryGetInt32($"{model}.ne", ref hasValue, out var ne))
                return Task.CompletedTask;

            if (!bindingContext.TryGetInt32($"{model}.gt", ref hasValue, out var gt))
                return Task.CompletedTask;

            if (!bindingContext.TryGetInt32($"{model}.ge", ref hasValue, out var ge))
                return Task.CompletedTask;

            if (!bindingContext.TryGetInt32($"{model}.lt", ref hasValue, out var lt))
                return Task.CompletedTask;

            if (!bindingContext.TryGetInt32($"{model}.le", ref hasValue, out var le))
                return Task.CompletedTask;

            if (!bindingContext.TryGetInt32List($"{model}.in", ref hasValue, out var @in))
                return Task.CompletedTask;

            if (!bindingContext.TryGetInt32List($"{model}.ni", ref hasValue, out var ni))
                return Task.CompletedTask;

            if (!hasValue)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }
            
            if (ge != null && ge == le)
            {
                bindingContext.Result = ModelBindingResult.Success(new Int32Parameter
                {
                    Eq = ge,
                    Ne = ne,
                    Gt = gt,
                    Lt = lt,
                    In = @in,
                    Ni = ni
                });

                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(new Int32Parameter
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
