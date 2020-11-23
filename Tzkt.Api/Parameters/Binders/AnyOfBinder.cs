using System;
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
            var model = bindingContext.ModelName;

            var key = bindingContext.HttpContext.Request.Query.Keys.FirstOrDefault(x => x.StartsWith("anyof"));
            if (key == null)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return;
            }

            var fields = key.Split(".", StringSplitOptions.RemoveEmptyEntries).Skip(1);
            if (fields.Count() < 2)
            {
                bindingContext.ModelState.TryAddModelError(key, "Invalid syntax of `anyof` parameter. At least two fields must be specified, e.g. `anyof.field1.field2=value`.");
                return;
            }

            var hasValue = false;
            if (!bindingContext.TryGetAccount(key, ref hasValue, out var value))
                return;

            if (!hasValue)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return;
            }

            bindingContext.Result = ModelBindingResult.Success(new AnyOfParameter
            {
                Fields = fields,
                Value = (await Accounts.GetAsync(value))?.Id ?? -1
            });
        }
    }
}
