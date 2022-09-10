using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class ContractEventFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal TzKT id.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter id { get; set; }

        /// <summary>
        /// Filter by level of the block where the event was emitted.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter level { get; set; }

        /// <summary>
        /// Filter by timestamp (ISO 8601) of the block where the event was emitted.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter timestamp { get; set; }

        /// <summary>
        /// Filter by contract address.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter contract { get; set; }

        /// <summary>
        /// Filter by hash of the code of the contract emitted the event.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter codeHash { get; set; }

        /// <summary>
        /// Filter by event tag.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public StringParameter tag { get; set; }

        /// <summary>
        /// Filter by payload.  
        /// Note, this parameter supports the following format: `payload{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by (for example, `?payload.foo.bar.in=1,2,3`).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public JsonParameter payload { get; set; }

        /// <summary>
        /// Filter by id of the transaction, in which the event was emitted.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64Parameter transactionId { get; set; }

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("level", level), ("timestamp", timestamp), ("contract", contract),
                ("codeHash", codeHash), ("tag", tag), ("payload", payload), ("transactionId", transactionId));
        }
    }
}
