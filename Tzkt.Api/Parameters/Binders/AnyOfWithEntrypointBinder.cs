using Microsoft.AspNetCore.Mvc.ModelBinding;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api
{
    public class AnyOfWithEntrypointBinder(AccountsCache _accounts) : IModelBinder
    {
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var key = bindingContext.HttpContext.Request.Query.Keys.FirstOrDefault(x => x.StartsWith("anyof."));
            if (key == null)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return;
            }

            var ss = key.Split(".", StringSplitOptions.RemoveEmptyEntries);
            var (mode, skip) = ss[^1] switch
            {
                "eq" => (3, 1),
                "in" => (3, 1),
                "null" => (5, 1),
                _ => (0, 0)
            };
            key = key[..^mode];

            var fields = ss.Skip(1).SkipLast(skip);
            if (fields.Count() < 2)
            {
                bindingContext.ModelState.TryAddModelError(key, "Invalid syntax of `anyof` parameter. At least two fields must be specified, e.g. `anyof.field1.field2=value`.");
                return;
            }

            var hasValue = false;

            if (!bindingContext.TryGetAddressWithEntrypoint($"{key}", ref hasValue, out var value))
                return;

            if (!bindingContext.TryGetAddressWithEntrypoint($"{key}.eq", ref hasValue, out var eq))
                return;

            if (!bindingContext.TryGetAddressWithEntrypointNullList($"{key}.in", ref hasValue, out var @in))
                return;

            if (!bindingContext.TryGetBool($"{key}.null", ref hasValue, out var isNull))
                return;

            if (!hasValue)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return;
            }

            var eqValue = value ?? eq;

            (int, byte[]?)? _eq = null;
            List<(int, byte[]?)>? _listIn = null;
            var inHasNull = false;

            if (eqValue != null)
                _eq = ((await _accounts.GetAsync(eqValue.Value.Item1))?.Id ?? -1, eqValue.Value.Item2);

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

            bindingContext.Result = ModelBindingResult.Success(new AnyOfWithEntrypointParameter
            {
                Fields = fields,
                Eq = _eq,
                In = _listIn,
                Null = isNull,
                InHasNull = inHasNull,
            });
        }
    }
}
