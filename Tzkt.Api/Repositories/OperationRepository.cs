using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;

using Tzkt.Api.Models;
using Tzkt.Api.Services;

namespace Tzkt.Api.Repositories
{
    public class OperationRepository : DbConnection
    {
        readonly AliasService Aliases;

        public OperationRepository(AliasService aliases, IConfiguration config) : base(config)
        {
            Aliases = aliases;
        }

        #region endorsements
        public async Task<int> GetEndorsementsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""EndorsementOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<EndorsementOperation>> GetEndorsements(string hash)
        {
            var sql = @"
                SELECT   ""Level"", ""Timestamp"", ""DelegateId"", ""Slots""
                FROM     ""EndorsementOps""
                WHERE    ""OpHash"" = @hash::character(51)
                LIMIT    1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new EndorsementOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Delegate = Aliases[row.DelegateId],
                Slots = row.Slots
            });
        }

        public async Task<IEnumerable<EndorsementOperation>> GetEndorsements(int level)
        {
            var sql = @"
                SELECT   ""Timestamp"", ""OpHash"", ""DelegateId"", ""Slots""
                FROM     ""EndorsementOps""
                WHERE    ""Level"" = @level
                ORDER BY ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level });

            return rows.Select(row => new EndorsementOperation
            {
                Level = level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Delegate = Aliases[row.DelegateId],
                Slots = row.Slots
            });
        }

        public async Task<IEnumerable<EndorsementOperation>> GetEndorsements(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT   ""Level"", ""Timestamp"", ""OpHash"", ""DelegateId"", ""Slots""
                FROM     ""EndorsementOps""
                ORDER BY ""Id""
                OFFSET   @offset
                LIMIT    @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new EndorsementOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Delegate = Aliases[row.DelegateId],
                Slots = row.Slots
            });
        }
        #endregion

        #region proposals
        public async Task<int> GetProposalsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""ProposalOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<ProposalOperation>> GetProposals(string hash)
        {
            var sql = @"
                SELECT    op.""Level"", op.""Timestamp"", op.""SenderId"", op.""PeriodId"", proposal.""Hash""
                FROM      ""ProposalOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId"" 
                WHERE     op.""OpHash"" = @hash::character(51)
                ORDER BY  op.""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new ProposalOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Delegate = Aliases[row.SenderId],
                Period = row.PeriodId,
                Proposal = row.Hash
            });
        }

        public async Task<IEnumerable<ProposalOperation>> GetProposals(int level)
        {
            var sql = @"
                SELECT    op.""Timestamp"", op.""OpHash"", op.""SenderId"", op.""PeriodId"", proposal.""Hash""
                FROM      ""ProposalOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId"" 
                WHERE     op.""Level"" = level
                ORDER BY  op.""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level });

            return rows.Select(row => new ProposalOperation
            {
                Level = level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Delegate = Aliases[row.SenderId],
                Period = row.PeriodId,
                Proposal = row.Hash
            });
        }

        public async Task<IEnumerable<ProposalOperation>> GetProposals(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    op.""Level"", op.""Timestamp"", op.""OpHash"", op.""SenderId"", op.""PeriodId"", proposal.""Hash""
                FROM      ""ProposalOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId"" 
                ORDER BY  op.""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new ProposalOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Delegate = Aliases[row.SenderId],
                Period = row.PeriodId,
                Proposal = row.Hash
            });
        }
        #endregion

        #region ballots
        public async Task<int> GetBallotsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""BallotOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<BallotOperation>> GetBallots(string hash)
        {
            var sql = @"
                SELECT    op.""Level"", op.""Timestamp"", op.""SenderId"", op.""PeriodId"", op.""Vote"", proposal.""Hash""
                FROM      ""BallotOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId"" 
                WHERE     op.""OpHash"" = @hash::character(51)
                LIMIT     1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new BallotOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Delegate = Aliases[row.SenderId],
                Period = row.PeriodId,
                Proposal = row.Hash,
                Vote = VoteToString(row.Vote)
            });
        }

        public async Task<IEnumerable<BallotOperation>> GetBallots(int level)
        {
            var sql = @"
                SELECT    op.""Timestamp"", op.""OpHash"", op.""SenderId"", op.""PeriodId"", op.""Vote"", proposal.""Hash""
                FROM      ""BallotOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId"" 
                WHERE     op.""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level });

            return rows.Select(row => new BallotOperation
            {
                Level = level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Delegate = Aliases[row.SenderId],
                Period = row.PeriodId,
                Proposal = row.Hash,
                Vote = VoteToString(row.Vote)
            });
        }

        public async Task<IEnumerable<BallotOperation>> GetBallots(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    op.""Level"", op.""Timestamp"", op.""OpHash"", op.""SenderId"", op.""PeriodId"", op.""Vote"", proposal.""Hash""
                FROM      ""BallotOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId"" 
                ORDER BY  op.""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new BallotOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Delegate = Aliases[row.SenderId],
                Period = row.PeriodId,
                Proposal = row.Hash,
                Vote = VoteToString(row.Vote)
            });
        }
        #endregion

        #region activations
        public async Task<int> GetActivationsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""ActivationOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<ActivationOperation>> GetActivations(string hash)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""AccountId"", ""Balance""
                FROM      ""ActivationOps""
                WHERE     ""OpHash"" = @hash::character(51)
                LIMIT     1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new ActivationOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Account = Aliases[row.AccountId],
                Balance = row.Balance
            });
        }

        public async Task<IEnumerable<ActivationOperation>> GetActivations(int level)
        {
            var sql = @"
                SELECT    ""Timestamp"", ""OpHash"", ""AccountId"", ""Balance""
                FROM      ""ActivationOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level });

            return rows.Select(row => new ActivationOperation
            {
                Level = level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Account = Aliases[row.AccountId],
                Balance = row.Balance
            });
        }

        public async Task<IEnumerable<ActivationOperation>> GetActivations(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""OpHash"", ""AccountId"", ""Balance""
                FROM      ""ActivationOps""
                ORDER BY  ""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new ActivationOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Account = Aliases[row.AccountId],
                Balance = row.Balance
            });
        }
        #endregion

        #region double baking
        public async Task<int> GetDoubleBakingsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""DoubleBakingOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<DoubleBakingOperation>> GetDoubleBakings(string hash)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""AccusedLevel"", ""AccuserId"", ""AccuserReward"",
                          ""OffenderId"", ""OffenderLostDeposit"", ""OffenderLostReward"", ""OffenderLostFee""
                FROM      ""DoubleBakingOps""
                WHERE     ""OpHash"" = @hash::character(51)
                LIMIT     1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new DoubleBakingOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Aliases[row.AccuserId],
                AccuserRewards = row.AccuserReward,
                Offender = Aliases[row.OffenderId],
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee
            });
        }

        public async Task<IEnumerable<DoubleBakingOperation>> GetDoubleBakings(int level)
        {
            var sql = @"
                SELECT    ""Timestamp"", ""OpHash"", ""AccusedLevel"", ""AccuserId"", ""AccuserReward"",
                          ""OffenderId"", ""OffenderLostDeposit"", ""OffenderLostReward"", ""OffenderLostFee""
                FROM      ""DoubleBakingOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level });

            return rows.Select(row => new DoubleBakingOperation
            {
                Level = level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Aliases[row.AccuserId],
                AccuserRewards = row.AccuserReward,
                Offender = Aliases[row.OffenderId],
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee
            });
        }

        public async Task<IEnumerable<DoubleBakingOperation>> GetDoubleBakings(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""OpHash"", ""AccusedLevel"", ""AccuserId"", ""AccuserReward"",
                          ""OffenderId"", ""OffenderLostDeposit"", ""OffenderLostReward"", ""OffenderLostFee""
                FROM      ""DoubleBakingOps""
                ORDER BY  ""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new DoubleBakingOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Aliases[row.AccuserId],
                AccuserRewards = row.AccuserReward,
                Offender = Aliases[row.OffenderId],
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee
            });
        }
        #endregion

        #region double endorsing
        public async Task<int> GetDoubleEndorsingsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""DoubleEndorsingOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<DoubleEndorsingOperation>> GetDoubleEndorsings(string hash)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""AccusedLevel"", ""AccuserId"", ""AccuserReward"",
                          ""OffenderId"", ""OffenderLostDeposit"", ""OffenderLostReward"", ""OffenderLostFee""
                FROM      ""DoubleEndorsingOps""
                WHERE     ""OpHash"" = @hash::character(51)
                LIMIT     1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new DoubleEndorsingOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Aliases[row.AccuserId],
                AccuserRewards = row.AccuserReward,
                Offender = Aliases[row.OffenderId],
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee
            });
        }

        public async Task<IEnumerable<DoubleEndorsingOperation>> GetDoubleEndorsings(int level)
        {
            var sql = @"
                SELECT    ""Timestamp"", ""OpHash"", ""AccusedLevel"", ""AccuserId"", ""AccuserReward"",
                          ""OffenderId"", ""OffenderLostDeposit"", ""OffenderLostReward"", ""OffenderLostFee""
                FROM      ""DoubleEndorsingOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level });

            return rows.Select(row => new DoubleEndorsingOperation
            {
                Level = level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Aliases[row.AccuserId],
                AccuserRewards = row.AccuserReward,
                Offender = Aliases[row.OffenderId],
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee
            });
        }

        public async Task<IEnumerable<DoubleEndorsingOperation>> GetDoubleEndorsings(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""OpHash"", ""AccusedLevel"", ""AccuserId"", ""AccuserReward"",
                          ""OffenderId"", ""OffenderLostDeposit"", ""OffenderLostReward"", ""OffenderLostFee""
                FROM      ""DoubleEndorsingOps""
                ORDER BY  ""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new DoubleEndorsingOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                AccusedLevel = row.AccusedLevel,
                Accuser = Aliases[row.AccuserId],
                AccuserRewards = row.AccuserReward,
                Offender = Aliases[row.OffenderId],
                OffenderLostDeposits = row.OffenderLostDeposit,
                OffenderLostRewards = row.OffenderLostReward,
                OffenderLostFees = row.OffenderLostFee
            });
        }
        #endregion

        #region nonce revelations
        public async Task<int> GetNonceRevelationsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""NonceRevelationOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelations(string hash)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""SenderId"", ""RevealedLevel""
                FROM      ""NonceRevelationOps""
                WHERE     ""OpHash"" = @hash::character(51)
                LIMIT     1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new NonceRevelationOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Delegate = Aliases[row.SenderId],
                RevealedLevel = row.RevealedLevel
            });
        }

        public async Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelations(int level)
        {
            var sql = @"
                SELECT    ""Timestamp"", ""OpHash"", ""SenderId"", ""RevealedLevel""
                FROM      ""NonceRevelationOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level });

            return rows.Select(row => new NonceRevelationOperation
            {
                Level = level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Delegate = Aliases[row.SenderId],
                RevealedLevel = row.RevealedLevel
            });
        }

        public async Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelations(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""RevealedLevel""
                FROM      ""NonceRevelationOps""
                ORDER BY  ""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new NonceRevelationOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Delegate = Aliases[row.SenderId],
                RevealedLevel = row.RevealedLevel
            });
        }
        #endregion

        #region delegations
        public async Task<int> GetDelegationsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""DelegationOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(string hash)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""SenderId"", ""Counter"", ""BakerFee"",
                          ""GasLimit"", ""GasUsed"", ""Status"", ""Nonce"", ""DelegateId""
                FROM      ""DelegationOps""
                WHERE     ""OpHash"" = @hash::character(51)
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new DelegationOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Sender = Aliases[row.SenderId],
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Delegate = row.DelegateId != null ? Aliases[row.DelegateId] : null,
                Status = StatusToString(row.Status)
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(string hash, int counter)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""SenderId"", ""BakerFee"",
                          ""GasLimit"", ""GasUsed"", ""Status"", ""Nonce"", ""DelegateId""
                FROM      ""DelegationOps""
                WHERE     ""OpHash"" = @hash::character(51) AND ""Counter"" = @counter
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter });

            return rows.Select(row => new DelegationOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Sender = Aliases[row.SenderId],
                Counter = counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Delegate = row.DelegateId != null ? Aliases[row.DelegateId] : null,
                Status = StatusToString(row.Status)
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(string hash, int counter, int nonce)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""SenderId"", ""BakerFee"",
                          ""GasLimit"", ""GasUsed"", ""Status"", ""DelegateId""
                FROM      ""DelegationOps""
                WHERE     ""OpHash"" = @hash::character(51) AND ""Counter"" = @counter AND ""Nonce"" = @nonce
                LIMIT     1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter, nonce });

            return rows.Select(row => new DelegationOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Sender = Aliases[row.SenderId],
                Counter = counter,
                Nonce = nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Delegate = row.DelegateId != null ? Aliases[row.DelegateId] : null,
                Status = StatusToString(row.Status)
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(int level)
        {
            var sql = @"
                SELECT    ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""BakerFee"",
                          ""GasLimit"", ""GasUsed"", ""Status"", ""Nonce"", ""DelegateId""
                FROM      ""DelegationOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level });

            return rows.Select(row => new DelegationOperation
            {
                Level = level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Sender = Aliases[row.SenderId],
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Delegate = row.DelegateId != null ? Aliases[row.DelegateId] : null,
                Status = StatusToString(row.Status)
            });
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""BakerFee"",
                          ""GasLimit"", ""GasUsed"", ""Status"", ""Nonce"", ""DelegateId""
                FROM      ""DelegationOps""
                ORDER BY  ""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new DelegationOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Sender = Aliases[row.SenderId],
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Delegate = row.DelegateId != null ? Aliases[row.DelegateId] : null,
                Status = StatusToString(row.Status)
            });
        }
        #endregion

        #region originations
        public async Task<int> GetOriginationsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""OriginationOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(string hash)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""SenderId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", 
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""ContractId"", ""DelegateId"", ""Balance""
                FROM      ""OriginationOps""
                WHERE     ""OpHash"" = @hash::character(51)
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new OriginationOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Sender = Aliases[row.SenderId],
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                AllocationFee = row.AllocationFee ?? 0,
                ContractDelegate = row.DelegateId != null ? Aliases[row.DelegateId] : null,
                ContractBalance = row.Balance,
                Status = StatusToString(row.Status),
                OriginatedContract = row.ContractId != null ? Aliases[row.ContractId] : null
            });
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(string hash, int counter)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""SenderId"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", 
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""ContractId"", ""DelegateId"", ""Balance""
                FROM      ""OriginationOps""
                WHERE     ""OpHash"" = @hash::character(51) AND ""Counter"" = @counter
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter });

            return rows.Select(row => new OriginationOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Sender = Aliases[row.SenderId],
                Counter = counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                AllocationFee = row.AllocationFee ?? 0,
                ContractDelegate = row.DelegateId != null ? Aliases[row.DelegateId] : null,
                ContractBalance = row.Balance,
                Status = StatusToString(row.Status),
                OriginatedContract = row.ContractId != null ? Aliases[row.ContractId] : null
            });
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(string hash, int counter, int nonce)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""SenderId"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", 
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""ContractId"", ""DelegateId"", ""Balance""
                FROM      ""OriginationOps""
                WHERE     ""OpHash"" = @hash::character(51) AND ""Counter"" = @counter AND ""Nonce"" = @nonce
                LIMIT     1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter, nonce });

            return rows.Select(row => new OriginationOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Sender = Aliases[row.SenderId],
                Counter = counter,
                Nonce = nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                AllocationFee = row.AllocationFee ?? 0,
                ContractDelegate = row.DelegateId != null ? Aliases[row.DelegateId] : null,
                ContractBalance = row.Balance,
                Status = StatusToString(row.Status),
                OriginatedContract = row.ContractId != null ? Aliases[row.ContractId] : null
            });
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(int level)
        {
            var sql = @"
                SELECT    ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", 
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""ContractId"", ""DelegateId"", ""Balance""
                FROM      ""OriginationOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level });

            return rows.Select(row => new OriginationOperation
            {
                Level = level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Sender = Aliases[row.SenderId],
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                AllocationFee = row.AllocationFee ?? 0,
                ContractDelegate = row.DelegateId != null ? Aliases[row.DelegateId] : null,
                ContractBalance = row.Balance,
                Status = StatusToString(row.Status),
                OriginatedContract = row.ContractId != null ? Aliases[row.ContractId] : null
            });
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"", 
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""ContractId"", ""DelegateId"", ""Balance""
                FROM      ""OriginationOps""
                ORDER BY  ""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new OriginationOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Sender = Aliases[row.SenderId],
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                AllocationFee = row.AllocationFee ?? 0,
                ContractDelegate = row.DelegateId != null ? Aliases[row.DelegateId] : null,
                ContractBalance = row.Balance,
                Status = StatusToString(row.Status),
                OriginatedContract = row.ContractId != null ? Aliases[row.ContractId] : null
            });
        }
        #endregion

        #region transactions
        public async Task<int> GetTransactionsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""TransactionOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(string hash)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""SenderId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"",
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""TargetId"", ""Amount""
                FROM      ""TransactionOps""
                WHERE     ""OpHash"" = @hash::character(51)
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new TransactionOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Sender = Aliases[row.SenderId],
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                AllocationFee = row.AllocationFee ?? 0,
                Target = row.TargetId != null ? Aliases[row.TargetId] : null,
                Amount = row.Amount,
                Status = StatusToString(row.Status)
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(string hash, int counter)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""SenderId"", ""BakerFee"", ""StorageFee"", ""AllocationFee"",
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""TargetId"", ""Amount""
                FROM      ""TransactionOps""
                WHERE     ""OpHash"" = @hash::character(51) AND ""Counter"" = @counter
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter });

            return rows.Select(row => new TransactionOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Sender = Aliases[row.SenderId],
                Counter = counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                AllocationFee = row.AllocationFee ?? 0,
                Target = row.TargetId != null ? Aliases[row.TargetId] : null,
                Amount = row.Amount,
                Status = StatusToString(row.Status)
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(string hash, int counter, int nonce)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""SenderId"", ""BakerFee"", ""StorageFee"", ""AllocationFee"",
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""TargetId"", ""Amount""
                FROM      ""TransactionOps""
                WHERE     ""OpHash"" = @hash::character(51) AND ""Counter"" = @counter AND ""Nonce"" = @nonce
                LIMIT     1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter, nonce });

            return rows.Select(row => new TransactionOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Sender = Aliases[row.SenderId],
                Counter = counter,
                Nonce = nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                AllocationFee = row.AllocationFee ?? 0,
                Target = row.TargetId != null ? Aliases[row.TargetId] : null,
                Amount = row.Amount,
                Status = StatusToString(row.Status)
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(int level)
        {
            var sql = @"
                SELECT    ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"",
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""TargetId"", ""Amount""
                FROM      ""TransactionOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level });

            return rows.Select(row => new TransactionOperation
            {
                Level = level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Sender = Aliases[row.SenderId],
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                AllocationFee = row.AllocationFee ?? 0,
                Target = row.TargetId != null ? Aliases[row.TargetId] : null,
                Amount = row.Amount,
                Status = StatusToString(row.Status)
            });
        }

        public async Task<IEnumerable<TransactionOperation>> GetTransactions(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""BakerFee"", ""StorageFee"", ""AllocationFee"",
                          ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", ""Status"", ""Nonce"", ""TargetId"", ""Amount""
                FROM      ""TransactionOps""
                ORDER BY  ""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new TransactionOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Sender = Aliases[row.SenderId],
                Counter = row.Counter,
                Nonce = row.Nonce,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                StorageLimit = row.StorageLimit,
                StorageUsed = row.StorageUsed,
                BakerFee = row.BakerFee,
                StorageFee = row.StorageFee ?? 0,
                AllocationFee = row.AllocationFee ?? 0,
                Target = row.TargetId != null ? Aliases[row.TargetId] : null,
                Amount = row.Amount,
                Status = StatusToString(row.Status)
            });
        }
        #endregion

        #region reveals
        public async Task<int> GetRevealsCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""RevealOps""";

            using var db = GetConnection();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<IEnumerable<RevealOperation>> GetReveals(string hash)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""SenderId"", ""Counter"", ""BakerFee"", ""GasLimit"", ""GasUsed"", ""Status""
                FROM      ""RevealOps""
                WHERE     ""OpHash"" = @hash::character(51)
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash });

            return rows.Select(row => new RevealOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Sender = Aliases[row.SenderId],
                Counter = row.Counter,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Status = StatusToString(row.Status),
            });
        }

        public async Task<IEnumerable<RevealOperation>> GetReveals(string hash, int counter)
        {
            var sql = @"
                SELECT  ""Level"", ""Timestamp"", ""SenderId"", ""BakerFee"", ""GasLimit"", ""GasUsed"", ""Status""
                FROM    ""RevealOps""
                WHERE   ""OpHash"" = @hash::character(51) AND ""Counter"" = @counter
                LIMIT   1";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { hash, counter });

            return rows.Select(row => new RevealOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = hash,
                Sender = Aliases[row.SenderId],
                Counter = counter,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Status = StatusToString(row.Status),
            });
        }

        public async Task<IEnumerable<RevealOperation>> GetReveals(int level)
        {
            var sql = @"
                SELECT    ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""BakerFee"", ""GasLimit"", ""GasUsed"", ""Status""
                FROM      ""RevealOps""
                WHERE     ""Level"" = @level
                ORDER BY  ""Id""";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { level });

            return rows.Select(row => new RevealOperation
            {
                Level = level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Sender = Aliases[row.SenderId],
                Counter = row.Counter,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Status = StatusToString(row.Status),
            });
        }

        public async Task<IEnumerable<RevealOperation>> GetReveals(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""BakerFee"", ""GasLimit"", ""GasUsed"", ""Status""
                FROM      ""RevealOps""
                ORDER BY  ""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new RevealOperation
            {
                Level = row.Level,
                Timestamp = row.Timestamp,
                Hash = row.OpHash,
                Sender = Aliases[row.SenderId],
                Counter = row.Counter,
                GasLimit = row.GasLimit,
                GasUsed = row.GasUsed,
                BakerFee = row.BakerFee,
                Status = StatusToString(row.Status),
            });
        }
        #endregion

        string VoteToString(int vote)
        {
            return vote switch
            {
                0 => "yay",
                1 => "nay",
                2 => "pass",
                _ => "unknown"
            };
        }

        string StatusToString(int status)
        {
            return status switch
            {
                1 => "applied",
                2 => "backtracked",
                3 => "skipped",
                4 => "failed",
                _ => "unknown"
            };
        }
    }
}
