using System.Numerics;
using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class DoublePreendorsingCommit : ProtocolCommit
    {
        public DoublePreendorsingCommit(ProtocolHandler protocol) : base(protocol) { }

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

            var operation = new DoublePreendorsingOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),

                AccusedLevel = content.Required("op1").Required("operations").RequiredInt32("level"),
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

            accuser.DoublePreendorsingCount++;
            if (offender != accuser) offender.DoublePreendorsingCount++;

            block.Operations |= Operations.DoublePreendorsings;

            Cache.Statistics.Current.TotalBurned += operation.OffenderLossOwn + operation.OffenderLossShared - operation.AccuserReward;
            Cache.Statistics.Current.TotalFrozen -= operation.OffenderLossOwn + operation.OffenderLossShared;
            #endregion

            Db.DoublePreendorsingOps.Add(operation);
        }

        public void Revert(Block block, DoublePreendorsingOperation operation)
        {
            #region init
            operation.Block ??= block;
            operation.Block.Proposer ??= Cache.Accounts.GetDelegate(block.ProposerId);

            operation.Accuser ??= Cache.Accounts.GetDelegate(operation.AccuserId);
            operation.Offender ??= Cache.Accounts.GetDelegate(operation.OffenderId);
            #endregion

            #region entities
            var accuser = operation.Accuser;
            var offender = operation.Offender;
            Db.TryAttach(accuser);
            Db.TryAttach(offender);
            #endregion

            #region apply operation
            accuser.Balance -= operation.AccuserReward;
            accuser.StakingBalance -= operation.AccuserReward;

            offender.Balance += operation.OffenderLossOwn;
            offender.StakingBalance += operation.OffenderLossOwn + operation.OffenderLossShared;
            offender.StakedBalance += operation.OffenderLossOwn;
            offender.ExternalStakedBalance += operation.OffenderLossShared;
            offender.TotalStakedBalance += operation.OffenderLossOwn + operation.OffenderLossShared;

            accuser.DoublePreendorsingCount--;
            if (offender != accuser) offender.DoublePreendorsingCount--;
            #endregion

            Db.DoublePreendorsingOps.Remove(operation);
            Cache.AppState.ReleaseOperationId();
        }
    }
}
