using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class TicketBalanceShortFilter : INormalizable
    {
        /// <summary>
        /// Filter by account address.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter account { get; set; }

        /// <summary>
        /// Filter by ticket.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TicketInfoFilter ticket { get; set; }

        /// <summary>
        /// Filter by balance.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public NatParameter balance { get; set; }

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("", 
                ("account", account), ("ticket", ticket), ("balance", balance));
        }
    }
}
