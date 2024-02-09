﻿using System.Text.Json;
using Mvkt.Data.Models;

namespace Mvkt.Sync.Protocols.Proto12
{
    class DoubleBakingCommit : ProtocolCommit
    {
        public DoubleBakingCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual void Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var balanceUpdates = content.Required("metadata").RequiredArray("balance_updates").EnumerateArray();
            var freezerUpdates = balanceUpdates.Where(x => x.RequiredString("kind") == "freezer" && x.RequiredString("category") == "deposits");
            var contractUpdates = balanceUpdates.Where(x => x.RequiredString("kind") == "contract");

            var offenderAddr = freezerUpdates.Any()
                ? freezerUpdates.First().RequiredString("delegate")
                : block.Proposer.Address; // this is wrong, but no big deal

            var offenderLoss = freezerUpdates.Any()
                ? -freezerUpdates.Sum(x => x.RequiredInt64("change"))
                : 0;

            var accuserReward = contractUpdates.Any()
                ? contractUpdates.Sum(x => x.RequiredInt64("change"))
                : 0;

            var doubleBaking = new DoubleBakingOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),

                SlashedLevel = block.Level,
                AccusedLevel = content.Required("bh1").RequiredInt32("level"),
                Accuser = block.Proposer,
                Offender = Cache.Accounts.GetDelegate(offenderAddr),

                Reward = accuserReward,
                LostStaked = offenderLoss,
                LostUnstaked = 0,
                LostExternalStaked = 0,
                LostExternalUnstaked = 0,

                RoundingLoss = 0
            };
            #endregion

            #region entities
            var accuser = doubleBaking.Accuser;
            var offender = doubleBaking.Offender;
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance += doubleBaking.Reward;
            accuser.StakingBalance += doubleBaking.Reward;

            offender.Balance -= doubleBaking.LostStaked;
            offender.StakingBalance -= doubleBaking.LostStaked;

            accuser.DoubleBakingCount++;
            if (offender != accuser) offender.DoubleBakingCount++;

            block.Operations |= Operations.DoubleBakings;

            Cache.Statistics.Current.TotalBurned += doubleBaking.LostStaked - doubleBaking.Reward;
            Cache.Statistics.Current.TotalFrozen -= doubleBaking.LostStaked;
            #endregion

            Db.DoubleBakingOps.Add(doubleBaking);
        }

        public virtual void Revert(Block block, DoubleBakingOperation doubleBaking)
        {
            #region init
            doubleBaking.Block ??= block;
            doubleBaking.Block.Proposer ??= Cache.Accounts.GetDelegate(block.ProposerId);

            doubleBaking.Accuser ??= Cache.Accounts.GetDelegate(doubleBaking.AccuserId);
            doubleBaking.Offender ??= Cache.Accounts.GetDelegate(doubleBaking.OffenderId);
            #endregion

            #region entities
            var accuser = doubleBaking.Accuser;
            var offender = doubleBaking.Offender;
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance -= doubleBaking.Reward;
            accuser.StakingBalance -= doubleBaking.Reward;

            offender.Balance += doubleBaking.LostStaked;
            offender.StakingBalance += doubleBaking.LostStaked;

            accuser.DoubleBakingCount--;
            if (offender != accuser) offender.DoubleBakingCount--;
            #endregion

            Db.DoubleBakingOps.Remove(doubleBaking);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
