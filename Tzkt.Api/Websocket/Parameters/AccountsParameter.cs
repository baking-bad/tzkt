using Microsoft.AspNetCore.SignalR;

namespace Tzkt.Api.Websocket
{
    public class AccountsParameter
    {
        public List<string>? Addresses { get; set; }

        public void EnsureValid()
        {
            if (Addresses?.Count > 0)
            {
                if (Addresses.Any(string.IsNullOrEmpty))
                    throw new HubException("Empty address. Array should not contain nulls or empty strings");
                if (Addresses.Any(x => !Regexes.Address().IsMatch(x)))
                    throw new HubException("Array contains an invalid address");
            }
        }
    }
}