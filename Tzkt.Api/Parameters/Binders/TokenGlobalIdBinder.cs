using System.Numerics;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api
{
    public class TokenGlobalIdBinder(AccountsCache accounts) : IModelBinder
    {
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var model = bindingContext.ModelName;
            var hasValue = false;

            if (!bindingContext.TryGetTokenGlobalId($"{model}", ref hasValue, out var value))
                return;

            if (!bindingContext.TryGetTokenGlobalId($"{model}.eq", ref hasValue, out var eq))
                return;

            if (!bindingContext.TryGetTokenGlobalIdList($"{model}.in", ref hasValue, out var @in))
                return;

            if (!hasValue)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return;
            }

            (int, BigInteger)? _eq = null;
            if ((value ?? eq) != null)
            {
                var (address, tokenId) = (value ?? eq)!.Value;
                var contract = await accounts.GetAsync(address);
                _eq = (contract?.Id ?? -1, tokenId);
            }

            List<(int, BigInteger)>? _in = null;
            if (@in != null)
            {
                _in = new(@in.Count);
                foreach (var (address, tokenId) in @in)
                {
                    var contract = await accounts.GetAsync(address);
                    _in.Add((contract?.Id ?? -1, tokenId));
                }
            }

            bindingContext.Result = ModelBindingResult.Success(new TokenGlobalIdParameter
            {
                Eq = _eq,
                In = _in
            });
        }
    }
}
