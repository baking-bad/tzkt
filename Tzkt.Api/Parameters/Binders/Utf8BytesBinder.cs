using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Tzkt.Api
{
    public class Utf8BytesBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var model = bindingContext.ModelName;
            var hasValue = false;

            if (!bindingContext.TryGetUtf8Bytes($"{model}", ref hasValue, out var value))
                return Task.CompletedTask;

            if (!bindingContext.TryGetUtf8Bytes($"{model}.eq", ref hasValue, out var eq))
                return Task.CompletedTask;

            if (!bindingContext.TryGetUtf8Bytes($"{model}.ne", ref hasValue, out var ne))
                return Task.CompletedTask;

            if (!bindingContext.TryGetUtf8BytesList($"{model}.in", ref hasValue, out var @in)) 
                return Task.CompletedTask;

            if (!bindingContext.TryGetUtf8BytesList($"{model}.ni", ref hasValue, out var ni))
                return Task.CompletedTask;

            if (!bindingContext.TryGetBool($"{model}.null", ref hasValue, out var isNull))
                return Task.CompletedTask;

            if (!hasValue)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(new Utf8BytesParameter
            {
                Eq = value ?? eq,
                Ne = ne,
                In = @in,
                Ni = ni,
                Null = isNull
            });

            return Task.CompletedTask;
        }
    }
}
