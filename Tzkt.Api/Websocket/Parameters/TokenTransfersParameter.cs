using System.Numerics;
using Microsoft.AspNetCore.SignalR;

namespace Tzkt.Api.Websocket
{
    public class TokenTransfersParameter
    {
        public string? Account { get; set; }
        public string? Contract { get; set; }
        public BigInteger? TokenId { get; set; }

        public void EnsureValid()
        {
            if (Account != null && !Regexes.Address().IsMatch(Account))
                throw new HubException("Invalid account address");

            if (Contract != null && !Regexes.Kt1Address().IsMatch(Contract))
                throw new HubException("Invalid contract address");

            if (TokenId != null && Contract == null)
                throw new HubException("If you specify token id, contract address must be specified as well");
        }
    }
}