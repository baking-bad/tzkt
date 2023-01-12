using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api
{
    public class AnyOfBinder : IModelBinder
    {
        readonly AccountsCache Accounts;

        public AnyOfBinder(AccountsCache accounts)
        {
            Accounts = accounts;
        }

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

            if (!bindingContext.TryGetAddress($"{key}", ref hasValue, out var value))
                return;

            if (!bindingContext.TryGetAddress($"{key}.eq", ref hasValue, out var eq))
                return;

            if (!bindingContext.TryGetAddressNullList($"{key}.in", ref hasValue, out var @in))
                return;

            if (!bindingContext.TryGetBool($"{key}.null", ref hasValue, out var isNull))
                return;

            if (!hasValue)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return;
            }

            int? _eq = null;
            List<int> _listIn = null;
            var inHasNull = false;

            if ((value ?? eq) != null)
                _eq = (await Accounts.GetAsync(value ?? eq))?.Id ?? -1;

            if (@in != null)
            {
                _listIn = new List<int>(@in.Count);
                foreach (var addr in @in)
                {
                    if (addr != null)
                    {
                        var acc = await Accounts.GetAsync(addr);
                        if (acc != null) _listIn.Add(acc.Id);
                    }
                    else
                    {
                        inHasNull = true;
                    }
                }
            }

            bindingContext.Result = ModelBindingResult.Success(new AnyOfParameter
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
