using NJsonSchema.Annotations;

namespace Tzkt.Api.Models
{
    public class SmartRollup : Account
    {
        /// <summary>
        /// Internal TzKT id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Type of the account
        /// </summary>
        public override string Type => AccountTypes.SmartRollup;

        /// <summary>
        /// Address of the account
        /// </summary>
        public override string Address { get; set; }

        /// <summary>
        /// Name of the account
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Information about the account, which has deployed the rollup to the blockchain
        /// </summary>       
        public Alias Creator { get; set; }

        /// <summary>
        /// PVM kind: `arith` or `wasm`
        /// </summary>
        public string PvmKind { get; set; }

        /// <summary>
        /// Genesis commitment hash
        /// </summary>
        public string GenesisCommitment { get; set; }

        /// <summary>
        /// The most recent cemented commitment hash
        /// </summary>
        public string LastCommitment { get; set; }

        /// <summary>
        /// Inbox level of the most recent cemented commitment
        /// </summary>
        public int InboxLevel { get; set; }

        /// <summary>
        /// Total number of stakers.
        /// </summary>
        public int TotalStakers { get; set; }

        /// <summary>
        /// Total number of active stakers.
        /// </summary>
        public int ActiveStakers { get; set; }

        /// <summary>
        /// Number of commitments that were cemented and executed
        /// </summary>
        public int ExecutedCommitments { get; set; }

        /// <summary>
        /// Number of commitments that were cemented (including executed ones)
        /// </summary>
        public int CementedCommitments { get; set; }

        /// <summary>
        /// Number of pending commitments
        /// </summary>
        public int PendingCommitments { get; set; }

        /// <summary>
        /// Number of commitments that were refuted
        /// </summary>
        public int RefutedCommitments { get; set; }

        /// <summary>
        /// Number of commitments that became orphan, due to their parent was refuted
        /// </summary>
        public int OrphanCommitments { get; set; }

        /// <summary>
        /// Amount of mutez locked as bonds
        /// </summary>
        public long SmartRollupBonds { get; set; }

        /// <summary>
        /// Number of account tokens with non-zero balances
        /// </summary>
        public int ActiveTokensCount { get; set; }

        /// <summary>
        /// Number of tokens the account ever had
        /// </summary>
        public int TokenBalancesCount { get; set; }

        /// <summary>
        /// Number of token transfers from/to the account
        /// </summary>
        public int TokenTransfersCount { get; set; }

        /// <summary>
        /// Number of account tickets with non-zero balances.
        /// </summary>
        public int ActiveTicketsCount { get; set; }

        /// <summary>
        /// Number of tickets the account ever had.
        /// </summary>
        public int TicketBalancesCount { get; set; }

        /// <summary>
        /// Number of ticket transfers from/to the account.
        /// </summary>
        public int TicketTransfersCount { get; set; }

        /// <summary>
        /// Number of transaction operations related to the account
        /// </summary>
        public int NumTransactions { get; set; }

        /// <summary>
        /// Number of transfer ticket operations related to the account
        /// </summary>
        public int TransferTicketCount { get; set; }

        /// <summary>
        /// Number of `smart_rollup_cement` operations related to the account
        /// </summary>
        public int SmartRollupCementCount { get; set; }

        /// <summary>
        /// Number of `smart_rollup_execute_outbox_message` operations related to the account
        /// </summary>
        public int SmartRollupExecuteCount { get; set; }

        /// <summary>
        /// Number of `smart_rollup_originate` operations related to the account
        /// </summary>
        public int SmartRollupOriginateCount { get; set; }

        /// <summary>
        /// Number of `smart_rollup_publish` operations related to the account
        /// </summary>
        public int SmartRollupPublishCount { get; set; }

        /// <summary>
        /// Number of `smart_rollup_recover_bond` operations related to the account
        /// </summary>
        public int SmartRollupRecoverBondCount { get; set; }

        /// <summary>
        /// Number of `smart_rollup_refute` operations related to the account
        /// </summary>
        public int SmartRollupRefuteCount { get; set; }

        /// <summary>
        /// Number of smart rollup refutation games related to the account
        /// </summary>
        public int RefutationGamesCount { get; set; }

        /// <summary>
        /// Number of active smart rollup refutation games related to the account
        /// </summary>
        public int ActiveRefutationGamesCount { get; set; }

        /// <summary>
        /// Block height at which the ghost contract appeared first time
        /// </summary>
        public int FirstActivity { get; set; }

        /// <summary>
        /// Block datetime at which the ghost contract appeared first time (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime FirstActivityTime { get; set; }

        /// <summary>
        /// Height of the block in which the ghost contract state was changed last time
        /// </summary>
        public int LastActivity { get; set; }

        /// <summary>
        /// Datetime of the block in which the ghost contract state was changed last time (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime LastActivityTime { get; set; }

        /// <summary>
        /// Off-chain extras
        /// </summary>
        [JsonSchemaType(typeof(object), IsNullable = true)]
        public RawJson Extras { get; set; }
    }
}
