using System;

namespace Tzkt.Api.Models
{
    public class TokenTransferOperation : Operation
    {
        /// <summary>
        /// `token_transfer` - is a synthetic type that represents transfer of FA tokens
        /// </summary>
        public override string Type => OpTypes.TokenTransfer;

        /// <summary>
        /// Internal TzKT id
        /// </summary>
        public override int Id { get; set; }

        /// <summary>
        /// Level of the block, at which the token transfer was made
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Timestamp of the block, at which the token transfer was made
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Contract, created the token.
        /// </summary>
        public Alias Contract { get; set; }

        /// <summary>
        /// Token id, unique within the contract.
        /// </summary>
        public string TokenId { get; set; }

        /// <summary>
        /// Sender account
        /// </summary>
        public Alias From { get; set; }

        /// <summary>
        /// Target account
        /// </summary>
        public Alias To { get; set; }

        /// <summary>
        /// Amount of tokens transferred (raw value, not divided by `decimals`)
        /// </summary>
        public string Amount { get; set; }

        /// <summary>
        /// Token `name`, specified in the metadata
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Token `symbol`, specified in the metadata
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Token `decimals`, specified in the metadata
        /// </summary>
        public string Decimals { get; set; }

        /// <summary>
        /// `shouldPreferSymbol`, specified in the metadata
        /// </summary>
        public string ShouldPreferSymbol { get; set; }

        /// <summary>
        /// `isBooleanAmount`, specified in the metadata
        /// </summary>
        public string IsBooleanAmount { get; set; }

        /// <summary>
        /// Internal TzKT id of the transaction operation, caused the token transfer
        /// </summary>
        public int? TransactionId { get; set; }

        /// <summary>
        /// Internal TzKT id of the origination operation, caused the token transfer
        /// </summary>
        public int? OriginationId { get; set; }

        /// <summary>
        /// Internal TzKT id of the migration operation, caused the token transfer
        /// </summary>
        public int? MigrationId { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the time of operation
        /// </summary>
        public QuoteShort Quote { get; set; }
        #endregion
    }
}
