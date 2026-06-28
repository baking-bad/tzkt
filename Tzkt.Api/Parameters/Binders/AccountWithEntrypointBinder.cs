using Microsoft.AspNetCore.Mvc.ModelBinding;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api
{
    public class AccountWithEntrypointBinder(AccountsCache _accounts) : IModelBinder
    {
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var model = bindingContext.ModelName;
            var hasValue = false;

            if (!bindingContext.TryGetAddressWithEntrypoint($"{model}", ref hasValue, out var value))
                return;

            if (!bindingContext.TryGetAddressWithEntrypoint($"{model}.eq", ref hasValue, out var eq))
                return;

            if (!bindingContext.TryGetAddressWithEntrypoint($"{model}.ne", ref hasValue, out var ne))
                return;

            if (!bindingContext.TryGetAddressWithEntrypointNullList($"{model}.in", ref hasValue, out var @in))
                return;

            if (!bindingContext.TryGetAddressWithEntrypointNullList($"{model}.ni", ref hasValue, out var ni))
                return;

            if (!bindingContext.TryGetBool($"{model}.null", ref hasValue, out var isNull))
                return;

            if (!hasValue)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return;
            }

            var eqValue = value ?? eq;

            (int, byte[]?)? _eq = null;
            (int, byte[]?)? _ne = null;
            List<(int, byte[]?)>? _listIn = null;
            List<(int, byte[]?)>? _listNi = null;
            var inHasNull = false;
            var niHasNull = false;

            if (eqValue != null)
                _eq = ((await _accounts.GetAsync(eqValue.Value.Item1))?.Id ?? -1, eqValue.Value.Item2);

            if (ne != null)
                _ne = ((await _accounts.GetAsync(ne.Value.Item1))?.Id ?? -1, ne.Value.Item2);

            if (@in != null)
            {
                _listIn = new List<(int, byte[]?)>(@in.Count);
                foreach (var awe in @in)
                {
                    if (awe.Item1 != null)
                    {
                        var acc = await _accounts.GetAsync(awe.Item1);
                        if (acc != null) _listIn.Add((acc.Id, awe.Item2));
                    }
                    else
                    {
                        inHasNull = true;
                    }
                }
            }

            if (ni != null)
            {
                _listNi = new List<(int, byte[]?)>(ni.Count);
                foreach (var awe in ni)
                {
                    if (awe.Item1 != null)
                    {
                        var acc = await _accounts.GetAsync(awe.Item1);
                        if (acc != null) _listNi.Add((acc.Id, awe.Item2));
                    }
                    else
                    {
                        niHasNull = true;
                    }
                }
            }

            bindingContext.Result = ModelBindingResult.Success(new AccountWithEntrypointParameter
            {
                Eq = _eq,
                Ne = _ne,
                In = _listIn,
                Ni = _listNi,
                Null = isNull,
                InHasNull = inHasNull,
                NiHasNull = niHasNull,
            });
        }
    }
}
