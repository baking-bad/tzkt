using Microsoft.AspNetCore.Mvc.ModelBinding;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api
{
    public class SmartRollupBinder : IModelBinder
    {
        readonly AccountsCache Accounts;

        public SmartRollupBinder(AccountsCache accounts)
        {
            Accounts = accounts;
        }

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var model = bindingContext.ModelName;
            var hasValue = false;

            if (!bindingContext.TryGetSr1Address($"{model}", ref hasValue, out var value))
                return;

            if (!bindingContext.TryGetSr1Address($"{model}.eq", ref hasValue, out var eq))
                return;

            if (!bindingContext.TryGetSr1Address($"{model}.ne", ref hasValue, out var ne))
                return;

            if (!bindingContext.TryGetSr1AddressList($"{model}.in", ref hasValue, out var @in))
                return;

            if (!bindingContext.TryGetSr1AddressList($"{model}.ni", ref hasValue, out var ni))
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

            if ((value ?? eq) != null)
                _eq = (await Accounts.GetAsync(value ?? eq))?.Id ?? -1;

            if (ne != null)
                _ne = (await Accounts.GetAsync(ne))?.Id ?? -1;

            if (@in != null)
            {
                _listIn = new List<int>(@in.Count);
                foreach (var addr in @in)
                {
                    var acc = await Accounts.GetAsync(addr);
                    if (acc != null) _listIn.Add(acc.Id);
                }
            }

            if (ni != null)
            {
                _listNi = new List<int>(ni.Count);
                foreach (var addr in ni)
                {
                    var acc = await Accounts.GetAsync(addr);
                    if (acc != null) _listNi.Add(acc.Id);
                }
            }

            bindingContext.Result = ModelBindingResult.Success(new SmartRollupParameter
            {
                Eq = _eq,
                Ne = _ne,
                In = _listIn,
                Ni = _listNi
            });
        }
    }
}
