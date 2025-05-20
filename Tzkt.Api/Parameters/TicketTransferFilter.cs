using NSwag.Annotations;
using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class TicketTransferFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal TzKT id.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64Parameter? id { get; set; }

        /// <summary>
        /// Filter by level of the block where the transfer was made.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter? level { get; set; }

        /// <summary>
        /// Filter by timestamp (ISO 8601) of the block where the transfer was made.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter? timestamp { get; set; }

        /// <summary>
        /// Filter by ticket.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TicketInfoFilter ticket { get; set; } = new();

        /// <summary>
        /// Filter by any of the specified fields (`from` or `to`).
        /// Example: `anyof.from.to=tz1...` will return transfers where `from` OR `to` is equal to the specified value.
        /// This parameter is useful when you need to get both incoming and outgoing transfers of the account at once.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AnyOfParameter? anyof { get; set; }

        /// <summary>
        /// Filter by sender address.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter? from { get; set; }

        /// <summary>
        /// Filter by recepient address.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter? to { get; set; }

        /// <summary>
        /// Filter by amount.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public NatParameter? amount { get; set; }

        /// <summary>
        /// Filter by id of the transaction, caused the ticket transfer.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64NullParameter? transactionId { get; set; }

        /// <summary>
        /// Filter by id of the transfer_ticket operation, caused the ticket transfer.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64NullParameter? transferTicketId { get; set; }

        /// <summary>
        /// Filter by id of the smart_rollup_execute operation, caused the ticket transfer.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64NullParameter? smartRollupExecuteId { get; set; }

        [OpenApiIgnore]
        public OrParameter? or { get; set; }

        [OpenApiIgnore]
        public bool Empty =>
            id == null &&
            level == null &&
            timestamp == null &&
            ticket.Empty &&
            anyof == null &&
            from == null &&
            to == null &&
            amount == null &&
            transactionId == null &&
            transferTicketId == null &&
            smartRollupExecuteId == null;

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("level", level), ("timestamp", timestamp), ("ticket", ticket), ("anyof", anyof), ("from", from), ("to", to),
                ("amount", amount), ("transactionId", transactionId), ("transferTicketId", transferTicketId), ("smartRollupExecuteId", smartRollupExecuteId));
        }
    }
}
