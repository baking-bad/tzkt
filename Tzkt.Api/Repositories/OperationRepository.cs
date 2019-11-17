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
        public async Task<EndorsementOperation> GetEndorsement(string hash)
        {
            var sql = @"
                SELECT   ""Level"", ""Timestamp"", ""DelegateId"", ""Slots""
                FROM     ""EndorsementOps""
                WHERE    ""OpHash"" = @hash
                LIMIT    1";

            using var db = GetConnection();
            var item = await db.QueryFirstOrDefaultAsync(sql, new { hash });
            if (item == null) return null;

            return new EndorsementOperation
            {
                Level = item.Level,
                Timestamp = item.Timestamp,
                Hash = hash,
                Delegate = Aliases[(int)item.DelegateId],
                Slots = item.Slots
            };
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
            var items = await db.QueryAsync(sql, new { limit, offset });

            var result = new List<EndorsementOperation>(items.Count());
            foreach (var item in items)
            {
                result.Add(new EndorsementOperation
                {
                    Level = item.Level,
                    Timestamp = item.Timestamp,
                    Hash = item.OpHash,
                    Delegate = Aliases[(int)item.DelegateId],
                    Slots = item.Slots
                });
            }

            return result;
        }
        #endregion

        #region proposals
        public async Task<ProposalOperation> GetProposal(string hash)
        {
            var sql = @"
                SELECT    op.""Level"", op.""Timestamp"", op.""SenderId"", op.""PeriodId"", proposal.""Hash""
                FROM      ""ProposalOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId"" 
                WHERE     op.""OpHash"" = @hash
                LIMIT     1";

            using var db = GetConnection();
            var item = await db.QueryFirstOrDefaultAsync(sql, new { hash });
            if (item == null) return null;

            return new ProposalOperation
            {
                Level = item.Level,
                Timestamp = item.Timestamp,
                Hash = hash,
                Delegate = Aliases[(int)item.SenderId],
                Period = item.PeriodId,
                Proposal = item.Hash
            };
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
            var items = await db.QueryAsync(sql, new { limit, offset });

            var result = new List<ProposalOperation>(items.Count());
            foreach (var item in items)
            {
                result.Add(new ProposalOperation
                {
                    Level = item.Level,
                    Timestamp = item.Timestamp,
                    Hash = item.OpHash,
                    Delegate = Aliases[(int)item.SenderId],
                    Period = item.PeriodId,
                    Proposal = item.Hash
                });
            }

            return result;
        }
        #endregion

        #region ballots
        public async Task<BallotOperation> GetBallot(string hash)
        {
            var sql = @"
                SELECT    op.""Level"", op.""Timestamp"", op.""SenderId"", op.""PeriodId"", op.""Vote"", proposal.""Hash""
                FROM      ""BallotOps"" as op
                LEFT JOIN ""Proposals"" as proposal ON proposal.""Id"" = op.""ProposalId"" 
                WHERE     op.""OpHash"" = @hash
                LIMIT     1";

            using var db = GetConnection();
            var item = await db.QueryFirstOrDefaultAsync(sql, new { hash });
            if (item == null) return null;

            var vote = (int)item.Vote;
            return new BallotOperation
            {
                Level = item.Level,
                Timestamp = item.Timestamp,
                Hash = hash,
                Delegate = Aliases[(int)item.SenderId],
                Period = item.PeriodId,
                Proposal = item.Hash,
                Vote = vote switch
                {
                    0 => "yay",
                    1 => "nay",
                    2 => "pass",
                    _ => "unknown"
                }
            };
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
            var items = await db.QueryAsync(sql, new { limit, offset });

            var result = new List<BallotOperation>(items.Count());
            foreach (var item in items)
            {
                var vote = (int)item.Vote;
                result.Add(new BallotOperation
                {
                    Level = item.Level,
                    Timestamp = item.Timestamp,
                    Hash = item.OpHash,
                    Delegate = Aliases[(int)item.SenderId],
                    Period = item.PeriodId,
                    Proposal = item.Hash,
                    Vote = vote switch
                    {
                        0 => "yay",
                        1 => "nay",
                        2 => "pass",
                        _ => "unknown"
                    }
                });
            }

            return result;
        }
        #endregion

        #region activations
        public async Task<ActivationOperation> GetActivation(string hash)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""AccountId"", ""Balance""
                FROM      ""ActivationOps""
                WHERE     ""OpHash"" = @hash
                LIMIT     1";

            using var db = GetConnection();
            var item = await db.QueryFirstOrDefaultAsync(sql, new { hash });
            if (item == null) return null;

            return new ActivationOperation
            {
                Level = item.Level,
                Timestamp = item.Timestamp,
                Hash = hash,
                Account = Aliases[(int)item.AccountId],
                Balance = item.Balance
            };
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
            var items = await db.QueryAsync(sql, new { limit, offset });

            var result = new List<ActivationOperation>(items.Count());
            foreach (var item in items)
            {
                result.Add(new ActivationOperation
                {
                    Level = item.Level,
                    Timestamp = item.Timestamp,
                    Hash = item.OpHash,
                    Account = Aliases[(int)item.AccountId],
                    Balance = item.Balance
                });
            }

            return result;
        }
        #endregion

        #region double baking
        public async Task<DoubleBakingOperation> GetDoubleBaking(string hash)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""AccusedLevel"", ""AccuserId"", ""AccuserReward"",
                          ""OffenderId"", ""OffenderLostDeposit"", ""OffenderLostReward"", ""OffenderLostFee""
                FROM      ""DoubleBakingOps""
                WHERE     ""OpHash"" = @hash
                LIMIT     1";

            using var db = GetConnection();
            var item = await db.QueryFirstOrDefaultAsync(sql, new { hash });
            if (item == null) return null;

            return new DoubleBakingOperation
            {
                Level = item.Level,
                Timestamp = item.Timestamp,
                Hash = hash,
                AccusedLevel = item.AccusedLevel,
                Accuser = Aliases[(int)item.AccuserId],
                AccuserRewards = item.AccuserReward,
                Offender = Aliases[(int)item.OffenderId],
                OffenderLostDeposits = item.OffenderLostDeposit,
                OffenderLostRewards = item.OffenderLostReward,
                OffenderLostFees = item.OffenderLostFee
            };
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
            var items = await db.QueryAsync(sql, new { limit, offset });

            var result = new List<DoubleBakingOperation>(items.Count());
            foreach (var item in items)
            {
                result.Add(new DoubleBakingOperation
                {
                    Level = item.Level,
                    Timestamp = item.Timestamp,
                    Hash = item.OpHash,
                    AccusedLevel = item.AccusedLevel,
                    Accuser = Aliases[(int)item.AccuserId],
                    AccuserRewards = item.AccuserReward,
                    Offender = Aliases[(int)item.OffenderId],
                    OffenderLostDeposits = item.OffenderLostDeposit,
                    OffenderLostRewards = item.OffenderLostReward,
                    OffenderLostFees = item.OffenderLostFee
                });
            }

            return result;
        }
        #endregion

        #region double endorsing
        public async Task<DoubleEndorsingOperation> GetDoubleEndorsing(string hash)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""AccusedLevel"", ""AccuserId"", ""AccuserReward"",
                          ""OffenderId"", ""OffenderLostDeposit"", ""OffenderLostReward"", ""OffenderLostFee""
                FROM      ""DoubleEndorsingOps""
                WHERE     ""OpHash"" = @hash
                LIMIT     1";

            using var db = GetConnection();
            var item = await db.QueryFirstOrDefaultAsync(sql, new { hash });
            if (item == null) return null;

            return new DoubleEndorsingOperation
            {
                Level = item.Level,
                Timestamp = item.Timestamp,
                Hash = hash,
                AccusedLevel = item.AccusedLevel,
                Accuser = Aliases[(int)item.AccuserId],
                AccuserRewards = item.AccuserReward,
                Offender = Aliases[(int)item.OffenderId],
                OffenderLostDeposits = item.OffenderLostDeposit,
                OffenderLostRewards = item.OffenderLostReward,
                OffenderLostFees = item.OffenderLostFee
            };
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
            var items = await db.QueryAsync(sql, new { limit, offset });

            var result = new List<DoubleEndorsingOperation>(items.Count());
            foreach (var item in items)
            {
                result.Add(new DoubleEndorsingOperation
                {
                    Level = item.Level,
                    Timestamp = item.Timestamp,
                    Hash = item.OpHash,
                    AccusedLevel = item.AccusedLevel,
                    Accuser = Aliases[(int)item.AccuserId],
                    AccuserRewards = item.AccuserReward,
                    Offender = Aliases[(int)item.OffenderId],
                    OffenderLostDeposits = item.OffenderLostDeposit,
                    OffenderLostRewards = item.OffenderLostReward,
                    OffenderLostFees = item.OffenderLostFee
                });
            }

            return result;
        }
        #endregion

        #region nonce revelations
        public async Task<NonceRevelationOperation> GetNonceRevelation(string hash)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""SenderId"", ""RevealedLevel""
                FROM      ""NonceRevelationOps""
                WHERE     ""OpHash"" = @hash
                LIMIT     1";

            using var db = GetConnection();
            var item = await db.QueryFirstOrDefaultAsync(sql, new { hash });
            if (item == null) return null;

            return new NonceRevelationOperation
            {
                Level = item.Level,
                Timestamp = item.Timestamp,
                Hash = hash,
                Delegate = Aliases[(int)item.SenderId],
                RevealedLevel = item.RevealedLevel
            };
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
            var items = await db.QueryAsync(sql, new { limit, offset });

            var result = new List<NonceRevelationOperation>(items.Count());
            foreach (var item in items)
            {
                result.Add(new NonceRevelationOperation
                {
                    Level = item.Level,
                    Timestamp = item.Timestamp,
                    Hash = item.OpHash,
                    Delegate = Aliases[(int)item.SenderId],
                    RevealedLevel = item.RevealedLevel
                });
            }

            return result;
        }
        #endregion

        #region delegations
        public async Task<DelegationOperation> GetDelegation(string hash)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""SenderId"", ""Counter"", ""GasLimit"",
                          ""GasUsed"", ""BakerFee"", ""DelegateId"", ""Status"", ""ParentId""
                FROM      ""DelegationOps""
                WHERE     ""OpHash"" = @hash
                LIMIT     1";

            using var db = GetConnection();
            var item = await db.QueryFirstOrDefaultAsync(sql, new { hash });
            if (item == null) return null;

            var delegateId = (int?)item.DelegateId;
            return new DelegationOperation
            {
                Level = item.Level,
                Timestamp = item.Timestamp,
                Hash = hash,
                Sender = Aliases[(int)item.SenderId],
                Counter = item.Counter,
                GasLimit = item.GasLimit,
                GasUsed = item.GasUsed,
                BakerFee = item.BakerFee,
                Delegate = delegateId == null ? null : Aliases[(int)delegateId],
                Status = OpStatus(item.Status),
                Internal = item.ParentId != null
            };
        }

        public async Task<IEnumerable<DelegationOperation>> GetDelegations(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""GasLimit"",
                          ""GasUsed"", ""BakerFee"", ""DelegateId"", ""Status"", ""ParentId""
                FROM      ""DelegationOps""
                ORDER BY  ""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var items = await db.QueryAsync(sql, new { limit, offset });

            var result = new List<DelegationOperation>(items.Count());
            foreach (var item in items)
            {
                var delegateId = (int?)item.DelegateId;
                result.Add(new DelegationOperation
                {
                    Level = item.Level,
                    Timestamp = item.Timestamp,
                    Hash = item.OpHash,
                    Sender = Aliases[(int)item.SenderId],
                    Counter = item.Counter,
                    GasLimit = item.GasLimit,
                    GasUsed = item.GasUsed,
                    BakerFee = item.BakerFee,
                    Delegate = delegateId == null ? null : Aliases[(int)delegateId],
                    Status = OpStatus(item.Status),
                    Internal = item.ParentId != null
                });
            }

            return result;
        }
        #endregion

        #region originations
        public async Task<OriginationOperation> GetOrigination(string hash)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""SenderId"", ""Counter"", ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", 
                          ""BakerFee"", ""StorageFee"", ""AllocationFee"", ""DelegateId"", ""Balance"", ""Status"", ""ContractId"", ""ParentId""
                FROM      ""OriginationOps""
                WHERE     ""OpHash"" = @hash
                LIMIT     1";

            using var db = GetConnection();
            var item = await db.QueryFirstOrDefaultAsync(sql, new { hash });
            if (item == null) return null;

            var contractId = (int?)item.ContractId;
            var delegateId = (int?)item.DelegateId;
            return new OriginationOperation
            {
                Level = item.Level,
                Timestamp = item.Timestamp,
                Hash = hash,
                Sender = Aliases[(int)item.SenderId],
                Counter = item.Counter,
                GasLimit = item.GasLimit,
                GasUsed = item.GasUsed,
                StorageLimit = item.StorageLimit,
                StorageUsed = item.StorageUsed,
                BakerFee = item.BakerFee,
                StorageFee = item.StorageFee,
                AllocationFee = item.AllocationFee,
                ContractDelegate = delegateId == null ? null : Aliases[(int)delegateId],
                ContractBalance = item.Balance,
                Status = OpStatus(item.Status),
                OriginatedContract = contractId == null ? null : Aliases[(int)contractId],
                Internal = item.ParentId != null
            };
        }

        public async Task<IEnumerable<OriginationOperation>> GetOriginations(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", 
                          ""BakerFee"", ""StorageFee"", ""AllocationFee"", ""DelegateId"", ""Balance"", ""Status"", ""ContractId"", ""ParentId""
                FROM      ""OriginationOps""
                ORDER BY  ""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var items = await db.QueryAsync(sql, new { limit, offset });

            var result = new List<OriginationOperation>(items.Count());
            foreach (var item in items)
            {
                var contractId = (int?)item.ContractId;
                var delegateId = (int?)item.DelegateId;
                result.Add(new OriginationOperation
                {
                    Level = item.Level,
                    Timestamp = item.Timestamp,
                    Hash = item.OpHash,
                    Sender = Aliases[(int)item.SenderId],
                    Counter = item.Counter,
                    GasLimit = item.GasLimit,
                    GasUsed = item.GasUsed,
                    StorageLimit = item.StorageLimit,
                    StorageUsed = item.StorageUsed,
                    BakerFee = item.BakerFee,
                    StorageFee = item.StorageFee,
                    AllocationFee = item.AllocationFee,
                    ContractDelegate = delegateId == null ? null : Aliases[(int)delegateId],
                    ContractBalance = item.Balance,
                    Status = OpStatus(item.Status),
                    OriginatedContract = contractId == null ? null : Aliases[(int)contractId],
                    Internal = item.ParentId != null
                });
            }

            return result;
        }
        #endregion

        #region transactions
        public async Task<TransactionOperation> GetTransaction(string hash)
        {
            var sql = @"
                SELECT    tx.""Level"", tx.""Timestamp"", tx.""SenderId"", tx.""Counter"", tx.""GasLimit"", tx.""GasUsed"", tx.""StorageLimit"", tx.""StorageUsed"", 
                          tx.""BakerFee"", tx.""StorageFee"", tx.""AllocationFee"", tx.""TargetId"", tx.""Amount"", tx.""Status""
                FROM      ""TransactionOps"" as tx
                WHERE     ""OpHash"" = @hash
                LIMIT     1";

            using var db = GetConnection();
            var item = await db.QueryFirstOrDefaultAsync(sql, new { hash });
            if (item == null) return null;

            var internals = (Data.Models.Operations)item.InternalOperations;
            if (internals != Data.Models.Operations.None)
            {

            }

            var targetId = (int?)item.TargetId;
            return new TransactionOperation
            {
                Level = item.Level,
                Timestamp = item.Timestamp,
                Hash = hash,
                Sender = Aliases[(int)item.SenderId],
                Counter = item.Counter,
                GasLimit = item.GasLimit,
                GasUsed = item.GasUsed,
                StorageLimit = item.StorageLimit,
                StorageUsed = item.StorageUsed,
                BakerFee = item.BakerFee,
                StorageFee = item.StorageFee,
                AllocationFee = item.AllocationFee,
                Target = targetId == null ? null : Aliases[(int)targetId],
                Amount = item.Amount,
                Status = OpStatus(item.Status),
            };
        }

        //public async Task<IEnumerable<TransactionOperation>> GetTransactions(int limit = 100, int offset = 0)
        //{
        //    var sql = @"
        //        SELECT    ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""GasLimit"", ""GasUsed"", ""StorageLimit"", ""StorageUsed"", 
        //                  ""BakerFee"", ""StorageFee"", ""AllocationFee"", ""TargetId"", ""Amount"", ""Status"", ""InternalOperations""
        //        FROM      ""TransactionOps""
        //        ORDER BY  ""Id""
        //        OFFSET    @offset
        //        LIMIT     @limit";

        //    using var db = GetConnection();
        //    var items = await db.QueryAsync(sql, new { limit, offset });

        //    var result = new List<TransactionOperation>(items.Count());
        //    foreach (var item in items)
        //    {
        //        var contractId = (int?)item.ContractId;
        //        var delegateId = (int?)item.DelegateId;
        //        result.Add(new TransactionOperation
        //        {
        //            Level = item.Level,
        //            Timestamp = item.Timestamp,
        //            Hash = item.OpHash,
        //            Sender = Aliases[(int)item.SenderId],
        //            Counter = item.Counter,
        //            GasLimit = item.GasLimit,
        //            GasUsed = item.GasUsed,
        //            StorageLimit = item.StorageLimit,
        //            StorageUsed = item.StorageUsed,
        //            BakerFee = item.BakerFee,
        //            StorageFee = item.StorageFee,
        //            AllocationFee = item.AllocationFee,
        //            ContractDelegate = delegateId == null ? null : Aliases[(int)delegateId],
        //            ContractBalance = item.Balance,
        //            Status = OpStatus(item.Status),
        //            OriginatedContract = contractId == null ? null : Aliases[(int)contractId]
        //        });
        //    }

        //    return result;
        //}
        #endregion

        #region reveals
        public async Task<RevealOperation> GetReveal(string hash)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""SenderId"", ""Counter"", ""GasLimit"", ""GasUsed"", ""BakerFee"", ""Status""
                FROM      ""RevealOps""
                WHERE     ""OpHash"" = @hash
                LIMIT     1";

            using var db = GetConnection();
            var item = await db.QueryFirstOrDefaultAsync(sql, new { hash });
            if (item == null) return null;

            return new RevealOperation
            {
                Level = item.Level,
                Timestamp = item.Timestamp,
                Hash = hash,
                Sender = Aliases[(int)item.SenderId],
                Counter = item.Counter,
                GasLimit = item.GasLimit,
                GasUsed = item.GasUsed,
                BakerFee = item.BakerFee,
                Status = OpStatus(item.Status),
            };
        }

        public async Task<IEnumerable<RevealOperation>> GetReveals(int limit = 100, int offset = 0)
        {
            var sql = @"
                SELECT    ""Level"", ""Timestamp"", ""OpHash"", ""SenderId"", ""Counter"", ""GasLimit"", ""GasUsed"", ""BakerFee"", ""Status""
                FROM      ""RevealOps""
                ORDER BY  ""Id""
                OFFSET    @offset
                LIMIT     @limit";

            using var db = GetConnection();
            var items = await db.QueryAsync(sql, new { limit, offset });

            var result = new List<RevealOperation>(items.Count());
            foreach (var item in items)
            {
                var contractId = (int?)item.ContractId;
                var delegateId = (int?)item.DelegateId;
                result.Add(new RevealOperation
                {
                    Level = item.Level,
                    Timestamp = item.Timestamp,
                    Hash = item.OpHash,
                    Sender = Aliases[(int)item.SenderId],
                    Counter = item.Counter,
                    GasLimit = item.GasLimit,
                    GasUsed = item.GasUsed,
                    BakerFee = item.BakerFee,
                    Status = OpStatus(item.Status),
                });
            }

            return result;
        }
        #endregion

        string OpStatus(int status)
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
