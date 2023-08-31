using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class TicketBalanceShortFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal TzKT id.
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64Parameter id { get; set; }

        /// <summary>
        /// Filter by ticket.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TicketInfoShortFilter ticket { get; set; }

        /// <summary>
        /// Filter by account address.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter account { get; set; }

        /// <summary>
        /// Filter by balance.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public NatParameter balance { get; set; }

        public bool Empty =>
            id == null &&
            (ticket == null || ticket.Empty) &&
            account == null &&
            balance == null;

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("ticket", ticket), ("account", account), ("balance", balance));
        }
    }
}
