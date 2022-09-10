using System.Text.Json.Serialization;

namespace Tzkt.Api
{
    public class TokenTransferFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal TzKT id.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64Parameter id { get; set; }

        /// <summary>
        /// Filter by level of the block where the transfer was made.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter level { get; set; }

        /// <summary>
        /// Filter by timestamp (ISO 8601) of the block where the transfer was made.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter timestamp { get; set; }

        /// <summary>
        /// Filter by token.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TokenInfoFilter token { get; set; }

        /// <summary>
        /// Filter by any of the specified fields (`from` or `to`).
        /// Example: `anyof.from.to=tz1...` will return transfers where `from` OR `to` is equal to the specified value.
        /// This parameter is useful when you need to get both incoming and outgoing transfers of the account at once.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AnyOfParameter anyof { get; set; }

        /// <summary>
        /// Filter by sender account address.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter from { get; set; }

        /// <summary>
        /// Filter by target account address.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter to { get; set; }

        /// <summary>
        /// Filter by amount.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public NatParameter amount { get; set; }

        /// <summary>
        /// Filter by id of the transaction, caused the token transfer.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64NullParameter transactionId { get; set; }

        /// <summary>
        /// Filter by id of the origination, caused the token transfer.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64NullParameter originationId { get; set; }

        /// <summary>
        /// Filter by id of the migration, caused the token transfer.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64NullParameter migrationId { get; set; }

        [JsonIgnore]
        public Int32NullParameter indexedAt { get; set; }

        public string Normalize(string name)
        {
            throw new System.NotImplementedException();
        }
    }
}
