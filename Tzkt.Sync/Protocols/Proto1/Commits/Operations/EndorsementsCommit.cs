using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto1
{
    public class EndorsementsCommit : ICommit<List<EndorsementOperation>>
    {
        #region constants
        protected virtual int EndorsementDeposit => 0;
        protected virtual int EndorsementReward => 0;
        #endregion

        public List<EndorsementOperation> Content { get; protected set; }

        protected readonly TzktContext Db;
        protected readonly CacheService Cache;

        public EndorsementsCommit(TzktContext db, CacheService cache)
        {
            Db = db;
            Cache = cache;
        }

        public virtual async Task<EndorsementsCommit> Init(JToken rawBlock, Block currentBlock)
        {
            await Validate(rawBlock);
            Content = await Parse(rawBlock, currentBlock);
            return this;
        }

        public virtual Task<EndorsementsCommit> Init(List<EndorsementOperation> operations)
        {
            Content = operations;
            return Task.FromResult(this);
        }

        public Task Apply()
        {
            foreach (var endorsement in Content)
            {
                #region update balances
                var baker = endorsement.Delegate;
                baker.Balance += endorsement.Reward;
                baker.FrozenRewards += endorsement.Reward;
                baker.FrozenDeposits += EndorsementDeposit * endorsement.Slots;
                #endregion

                #region counters
                baker.Operations |= Operations.Endorsements;
                #endregion

                Db.Delegates.Update(baker);
                Db.EndorsementOps.Add(endorsement);
            }
            return Task.CompletedTask;
        }

        public async Task Revert()
        {
            foreach (var endorsement in Content)
            {
                #region update balances
                var baker = (Data.Models.Delegate)await Cache.Accounts.GetAccountAsync(endorsement.DelegateId);
                baker.Balance -= endorsement.Reward;
                baker.FrozenRewards -= endorsement.Reward;
                baker.FrozenDeposits -= EndorsementDeposit * endorsement.Slots;
                #endregion

                #region counters
                if (await Db.EndorsementOps.CountAsync(x => x.DelegateId == baker.Id) == 1)
                    baker.Operations &= ~Operations.Endorsements;
                #endregion

                Db.Delegates.Update(baker);
                Db.EndorsementOps.Remove(endorsement);
            }
        }

        public async Task Validate(JToken block)
        {
            var lastBlock = await Cache.State.GetCurrentBlock();

            foreach (var operation in block["operations"]?[0] ?? throw new Exception("Endorsements missed"))
            {
                var opHash = operation["hash"]?.String();
                if (String.IsNullOrEmpty(opHash))
                    throw new Exception($"Invalid endorsement operation hash '{opHash}'");

                foreach (var content in operation["contents"])
                {
                    var kind = content["kind"]?.String();
                    if (kind != "endorsement")
                        throw new Exception("Invalid endorsement content kind");

                    var level = content["level"]?.Int32() ?? 0;
                    if (level != lastBlock.Level)
                        throw new Exception("Invalid endorsement level");

                    var metadata = content["metadata"];
                    if (metadata == null)
                        throw new Exception("Invalid endorsement metadata");

                    var delegat = metadata["delegate"]?.String();
                    if (!await Cache.Accounts.ExistsAsync(delegat, AccountType.Delegate))
                        throw new Exception($"Invalid endorsement delegate '{delegat}'");

                    var slotsCount = metadata["slots"]?.Count() ?? -1;
                    if (slotsCount <= 0)
                        throw new Exception($"Invalid endorsement slots number '{slotsCount}'");

                    var balanceUpdates = metadata["balance_updates"];
                    if (balanceUpdates == null)
                        throw new Exception($"Invalid endorsement balance updates");

                    var updates = BalanceUpdates.Parse((JArray)balanceUpdates);
                    if (updates.Count != 0 && updates.Count != 3)
                        throw new Exception($"Invalid endorsement balance updates count");

                    if (updates.Count > 0)
                    {
                        if (!(updates.FirstOrDefault(x => x is ContractUpdate) is ContractUpdate contractUpdate) ||
                            contractUpdate.Contract != delegat ||
                            contractUpdate.Change != slotsCount * EndorsementDeposit)
                            throw new Exception($"Invalid endorsement contract update");

                        if (!(updates.FirstOrDefault(x => x is DepositsUpdate) is DepositsUpdate depostisUpdate) ||
                            depostisUpdate.Delegate != delegat ||
                            depostisUpdate.Change != slotsCount * EndorsementDeposit)
                            throw new Exception($"Invalid endorsement depostis update");
                        
                        if (!(updates.FirstOrDefault(x => x is RewardsUpdate) is RewardsUpdate rewardsUpdate) ||
                            rewardsUpdate.Delegate != delegat ||
                            rewardsUpdate.Change != GetEndorsementReward(slotsCount, lastBlock.Priority))
                            throw new Exception($"Invalid endorsement depostis update");
                    }
                }
            }
        }

        public async Task<List<EndorsementOperation>> Parse(JToken rawBlock, Block currentBlock)
        {
            var operations = (JArray)rawBlock["operations"][0];
            var result = new List<EndorsementOperation>(operations.Count);

            foreach (var operation in operations)
            {
                var opHash = operation["hash"]?.String();

                foreach (var content in operation["contents"])
                {
                    var metadata = content["metadata"];

                    var delegat = metadata["delegate"].String();
                    var slots = metadata["slots"].Count();
                    var balanceUpdates = BalanceUpdates.Parse((JArray)metadata["balance_updates"]);

                    result.Add(new EndorsementOperation
                    {
                        Block = currentBlock,
                        Timestamp = currentBlock.Timestamp,
                        OpHash = opHash,
                        Slots = slots,
                        Delegate = (Data.Models.Delegate)await Cache.Accounts.GetAccountAsync(delegat),
                        Reward = balanceUpdates.FirstOrDefault(x => x is RewardsUpdate)?.Change ?? 0
                    });
                }
            }

            return result;
        }

        long GetEndorsementReward(int slots, int priority)
            => (long)Math.Round((double)slots * EndorsementReward / (priority + 1));
    }
}
