using Microsoft.AspNetCore.Mvc.ModelBinding;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api
{
    public class TimestampBinder : IModelBinder
    {
        readonly TimeCache Time;

        public TimestampBinder(TimeCache time)
        {
            Time = time;
        }

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

            var param = new TimestampParameter
            {
                Eq = (value ?? eq) == null ? null : Time.FindLevel((DateTime)((value ?? eq)!), SearchMode.Exact),
                Ne = ne == null ? null : Time.FindLevel((DateTime)ne, SearchMode.Exact),
                In = @in?.Select(x => Time.FindLevel(x, SearchMode.Exact)).ToList(),
                Ni = ni?.Select(x => Time.FindLevel(x, SearchMode.Exact)).ToList(),
            };

            if (gt != null)
            {
                var level = Time.FindLevel((DateTime)gt, SearchMode.ExactOrLower);
                param.Gt = level != -1 ? level : null;
            }
            if (ge != null)
            {
                var level = Time.FindLevel((DateTime)ge, SearchMode.ExactOrHigher);
                param.Ge = level != -1 ? level : int.MaxValue;
            }
            if (lt != null)
            {
                var level = Time.FindLevel((DateTime)lt, SearchMode.ExactOrHigher);
                param.Lt = level != -1 ? level : null;
            }
            if (le != null)
            {
                var level = Time.FindLevel((DateTime)le, SearchMode.ExactOrLower);
                param.Le = level != -1 ? level : int.MinValue;
            }

            bindingContext.Result = ModelBindingResult.Success(param);

            return Task.CompletedTask;
        }
    }
}
