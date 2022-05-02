using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Tzkt.Api
{
    public class VoteBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var model = bindingContext.ModelName;
            var hasValue = false;

            if (!bindingContext.TryGetVote($"{model}", ref hasValue, out var value))
                return Task.CompletedTask;

            if (!bindingContext.TryGetVote($"{model}.eq", ref hasValue, out var eq))
                return Task.CompletedTask;

            if (!bindingContext.TryGetVote($"{model}.ne", ref hasValue, out var ne))
                return Task.CompletedTask;

            if (!bindingContext.TryGetVotesList($"{model}.in", ref hasValue, out var @in))
                return Task.CompletedTask;

            if (!bindingContext.TryGetVotesList($"{model}.ni", ref hasValue, out var ni))
                return Task.CompletedTask;

            if (!hasValue)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(new VoteParameter
            {
                Eq = value ?? eq,
                Ne = ne,
                In = @in,
                Ni = ni
            });

            return Task.CompletedTask;
        }
    }
}
