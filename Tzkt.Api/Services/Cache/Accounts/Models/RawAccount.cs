using Tzkt.Api.Models;

namespace Tzkt.Api.Services.Cache
{
    public class RawAccount
    {
        public int Id { get; set; }
        public string Address { get; set; }
        public int FirstLevel { get; set; }
        public int LastLevel { get; set; }

        public long Balance { get; set; }
        public long RollupBonds { get; set; }
        public int Counter { get; set; }

        public int ContractsCount { get; set; }
        public int RollupsCount { get; set; }
        public int ActiveTokensCount { get; set; }
        public int TokenBalancesCount { get; set; }
        public int TokenTransfersCount { get; set; }

        public int DelegationsCount { get; set; }
        public int OriginationsCount { get; set; }
        public int TransactionsCount { get; set; }
        public int RevealsCount { get; set; }
        public int MigrationsCount { get; set; }

        public int TxRollupOriginationCount { get; set; }
        public int TxRollupSubmitBatchCount { get; set; }
        public int TxRollupCommitCount { get; set; }
        public int TxRollupReturnBondCount { get; set; }
        public int TxRollupFinalizeCommitmentCount { get; set; }
        public int TxRollupRemoveCommitmentCount { get; set; }
        public int TxRollupRejectionCount { get; set; }
        public int TxRollupDispatchTicketsCount { get; set; }
        public int TransferTicketCount { get; set; }

        public int IncreasePaidStorageCount { get; set; }
        public int UpdateConsensusKeyCount { get; set; }
        public int DrainDelegateCount { get; set; }

        public int? DelegateId { get; set; }
        public int? DelegationLevel { get; set; }
        public bool Staked { get; set; }

        public string Extras { get; set; }

        public string Alias { get; set; }
        public string Profile { get; set; }
        public Alias Info => new()
        {
            Name = Alias,
            Address = Address
        };
    }
}
