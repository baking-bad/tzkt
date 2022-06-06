using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;

namespace Tzkt.Api.Websocket
{
    public class TokenTransfersParameter
    {
        public string Account { get; set; }
        public string Contract { get; set; }
        public string TokenId { get; set; }

        public void EnsureValid()
        {
            if (Account != null && !Regex.IsMatch(Account, "^[0-9A-Za-z]{36,37}$"))
                throw new HubException("Invalid account address");

            if (Contract != null && !Regex.IsMatch(Contract, "^KT1[0-9A-Za-z]{33}$"))
                throw new HubException("Invalid contract address");

            if (TokenId != null)
            {
                if (!Regex.IsMatch(TokenId, "^[0-9]+$"))
                    throw new HubException("Invalid tokenId");

                if (Contract == null)
                    throw new HubException("If you specify token id, contract address must be specified as well");
            }
        }
    }
}