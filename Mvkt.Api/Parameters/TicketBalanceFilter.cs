using NSwag.Annotations;
using Mvkt.Api.Services;

namespace Mvkt.Api
{
    public class TicketBalanceFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal MvKT id.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64Parameter id { get; set; }

        /// <summary>
        /// Filter by ticket.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TicketInfoFilter ticket { get; set; }

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

        /// <summary>
        /// Filter by number of transfers.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter transfersCount { get; set; }

        /// <summary>
        /// Filter by level of the block where the balance was first changed.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter firstLevel { get; set; }

        /// <summary>
        /// Filter by timestamp (ISO 8601) of the block where the balance was first changed.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter firstTime { get; set; }

        /// <summary>
        /// Filter by level of the block where the balance was last changed.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter lastLevel { get; set; }

        /// <summary>
        /// Filter by timestamp (ISO 8601) of the block where the balance was last changed.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter lastTime { get; set; }

        [OpenApiIgnore]
        public bool Empty =>
            id == null &&
            (ticket == null || ticket.Empty) &&
            account == null &&
            balance == null &&
            transfersCount == null &&
            firstLevel == null &&
            firstTime == null &&
            lastLevel == null &&
            lastTime == null;

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("ticket", ticket), ("account", account), ("balance", balance), ("transfersCount", transfersCount),
                ("firstLevel", firstLevel), ("firstTime", firstTime), ("lastLevel", lastLevel), ("lastTime", lastTime));
        }
    }
}
