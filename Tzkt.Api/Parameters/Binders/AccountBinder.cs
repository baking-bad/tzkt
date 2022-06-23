using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using Tzkt.Api.Services.Cache;

namespace Tzkt.Api
{
    public class AccountBinder : IModelBinder
    {
        readonly AccountsCache Accounts;

        public AccountBinder(AccountsCache accounts)
        {
            Accounts = accounts;
        }

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var model = bindingContext.ModelName;
            var hasValue = false;

            if (!bindingContext.TryGetAccount($"{model}", ref hasValue, out var value))
                return;

            if (!bindingContext.TryGetAccount($"{model}.eq", ref hasValue, out var eq))
                return;

            if (!bindingContext.TryGetAccount($"{model}.ne", ref hasValue, out var ne))
                return;

            if (!bindingContext.TryGetAccountList($"{model}.in", ref hasValue, out var @in))
                return;

            if (!bindingContext.TryGetAccountList($"{model}.ni", ref hasValue, out var ni))
                return;

            if (!bindingContext.TryGetString($"{model}.eqx", ref hasValue, out var eqx))
                return;

            if (!bindingContext.TryGetString($"{model}.nex", ref hasValue, out var nex))
                return;

            if (!bindingContext.TryGetBool($"{model}.null", ref hasValue, out var isNull))
                return;

            if (!hasValue)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return;
            }

            int? _eq = null;
            int? _ne = null;
            List<int> _listIn = null;
            List<int> _listNi = null;
            var inHasNull = false;
            var niHasNull = false;

            if ((value ?? eq) != null)
                _eq = (await Accounts.GetAsync(value ?? eq))?.Id ?? -1;

            if (ne != null)
                _ne = (await Accounts.GetAsync(ne))?.Id ?? -1;

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

            if (ni != null)
            {
                _listNi = new List<int>(ni.Count);
                foreach (var addr in ni)
                {
                    if (addr != null)
                    {
                        var acc = await Accounts.GetAsync(addr);
                        if (acc != null) _listNi.Add(acc.Id);
                    }
                    else
                    {
                        niHasNull = true;
                    }
                }
            }

            stopwatch.Stop();
            Console.WriteLine($"BINDER_OF_ACCOUNTS TOOK {stopwatch.ElapsedMilliseconds}");
            stopwatch.Reset();
            bindingContext.Result = ModelBindingResult.Success(new AccountParameter
            {
                Eq = _eq,
                Ne = _ne,
                In = _listIn,
                Ni = _listNi,
                Eqx = eqx,
                Nex = nex,
                Null = isNull,
                InHasNull = inHasNull,
                NiHasNull = niHasNull,
            });
        }
    }
}
