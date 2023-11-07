using System.Numerics;
using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class DoubleBakingCommit : ProtocolCommit
    {
        public DoubleBakingCommit(ProtocolHandler protocol) : base(protocol) { }

        public void Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var balanceUpdates = content.Required("metadata").OptionalArray("balance_updates")?.EnumerateArray()
                ?? Enumerable.Empty<JsonElement>();

            var freezerUpdates = balanceUpdates.Where(x => x.RequiredString("kind") == "freezer" && x.RequiredString("category") == "deposits");
            var contractUpdates = balanceUpdates.Where(x => x.RequiredString("kind") == "contract");

            // there are also ("freezer", "unstaked_deposits") updates, but they don't work properly in oxford,
            // so we count slashed unstaked deposits at the moment of finalize_update
            // TODO: count slashing here, when all protocol bugs are fixed

            var offenderAddr = freezerUpdates.Any()
                ? freezerUpdates.First().Required("staker").RequiredString("delegate")
                : block.Proposer.Address; // this is wrong, but no big deal

            var offender = Cache.Accounts.GetDelegate(offenderAddr);
            var offenderLoss = freezerUpdates.Any()
                ? contractUpdates.Sum(x => -x.RequiredInt64("change"))
                : 0;
            var offenderLossOwn = (long)((BigInteger)offenderLoss * offender.StakedBalance / offender.TotalStakedBalance);
            var offenderLossShared = offenderLoss - offenderLossOwn;

            //var offenderLossOwn = 0L;
            //var offenderLossShared = 0L;
            //foreach (var freezerUpdate in freezerUpdates)
            //{
            //    var change = -freezerUpdate.RequiredInt64("change");
            //    var changeOwn = (long)((BigInteger)change * offender.StakedBalance / offender.TotalStakedBalance);
            //    var changeShared = change - changeOwn;
            //    offenderLossOwn += changeOwn;
            //    offenderLossShared += changeShared;
            //}

            var accuser = block.Proposer;
            var accuserReward = contractUpdates.Any()
                ? contractUpdates.Sum(x => x.RequiredInt64("change"))
                : 0;

            var operation = new DoubleBakingOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),

                AccusedLevel = content.Required("bh1").RequiredInt32("level"),
                Accuser = accuser,
                Offender = offender,

                AccuserReward = accuserReward,
                OffenderLossOwn = offenderLossOwn,
                OffenderLossShared = offenderLossShared
            };
            #endregion

            #region entities
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance += operation.AccuserReward;
            accuser.StakingBalance += operation.AccuserReward;

            offender.Balance -= operation.OffenderLossOwn;
            offender.StakingBalance -= operation.OffenderLossOwn + operation.OffenderLossShared;
            offender.StakedBalance -= operation.OffenderLossOwn;
            offender.ExternalStakedBalance -= operation.OffenderLossShared;
            offender.TotalStakedBalance -= operation.OffenderLossOwn + operation.OffenderLossShared;

            accuser.DoubleBakingCount++;
            if (offender != accuser) offender.DoubleBakingCount++;

            block.Operations |= Operations.DoubleBakings;

            Cache.Statistics.Current.TotalBurned += operation.OffenderLossOwn + operation.OffenderLossShared - operation.AccuserReward;
            Cache.Statistics.Current.TotalFrozen -= operation.OffenderLossOwn + operation.OffenderLossShared;
            #endregion

            Db.DoubleBakingOps.Add(operation);
        }

        public void Revert(Block block, DoubleBakingOperation doubleBaking)
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
            accuser.Balance -= doubleBaking.AccuserReward;
            accuser.StakingBalance -= doubleBaking.AccuserReward;

            offender.Balance += doubleBaking.OffenderLossOwn;
            offender.StakingBalance += doubleBaking.OffenderLossOwn + doubleBaking.OffenderLossShared;
            offender.StakedBalance += doubleBaking.OffenderLossOwn;
            offender.ExternalStakedBalance += doubleBaking.OffenderLossShared;
            offender.TotalStakedBalance += doubleBaking.OffenderLossOwn + doubleBaking.OffenderLossShared;

            accuser.DoubleBakingCount--;
            if (offender != accuser) offender.DoubleBakingCount--;
            #endregion

            Db.DoubleBakingOps.Remove(doubleBaking);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
