using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api
{
    public class AccountParameter
    {
        public int Value { get; private set; }
        public string Column { get; private set; }
        public List<int> Values { get; private set; }
        public QueryMode Mode { get; private set; }
        public string Error { get; private set; }

        public bool Invalid => Error != null;

        public static async Task<AccountParameter> Parse(string value, AccountsCache accounts, params string[] columns)
        {
            #region exact
            if (value.Length == 36)
            {
                if (!Regex.IsMatch(value, "^[0-9A-z]{36}$"))
                    return new AccountParameter { Error = "Invalid account address." };

                var account = await accounts.GetAsync(value);
                if (account == null)
                    return new AccountParameter { Mode = QueryMode.Dead };

                return new AccountParameter
                {
                    Value = account.Id,
                    Mode = QueryMode.Exact
                };
            }
            #endregion

            #region any
            if (value.Length > 36)
            {
                var addresses = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var address in addresses)
                    if (!Regex.IsMatch(address, "^[0-9A-z]{36}$"))
                        return new AccountParameter { Error = "Invalid account address in the list." };

                var ids = new List<int>(addresses.Length);
                foreach (var address in addresses)
                {
                    var account = await accounts.GetAsync(address);
                    if (account != null)
                        ids.Add(account.Id);
                }

                if (ids.Count == 0)
                    return new AccountParameter { Mode = QueryMode.Dead };

                return new AccountParameter
                {
                    Values = ids,
                    Mode = QueryMode.Any
                };
            }
            #endregion

            #region null
            if (value == "null")
                return new AccountParameter { Mode = QueryMode.Null };
            #endregion

            #region column
            if (!columns.Contains(value))
                return new AccountParameter { Error = "Unsupported keyword" };

            return new AccountParameter
            {
                Column = value,
                Mode = QueryMode.Column
            };
            #endregion
        }
    }
}
