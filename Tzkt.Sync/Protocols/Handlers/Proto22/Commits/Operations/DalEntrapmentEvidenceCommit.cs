using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto22
{
    class DalEntrapmentEvidenceCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var trapLevel = GetTrapLevel(content);
            var trapSlotIndex = content.RequiredInt32("slot_index");

            var accuser = Context.Proposer;
            var offender = Cache.Accounts.GetDelegate(await GetAttester(trapLevel, GetConsensusSlot(content)));

            var operation = new DalEntrapmentEvidenceOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),

                AccuserId = accuser.Id,
                OffenderId = offender.Id,

                TrapLevel = trapLevel,
                TrapSlotIndex = trapSlotIndex,
            };
            #endregion

            #region apply operation
            Db.TryAttach(accuser);
            accuser.DalEntrapmentEvidenceOpsCount++;

            if (offender.Id != accuser.Id)
            {
                Db.TryAttach(offender);
                offender.DalEntrapmentEvidenceOpsCount++;
            }

            block.Operations |= Operations.DalEntrapmentEvidence;

            Cache.AppState.Get().DalEntrapmentEvidenceOpsCount++;
            #endregion

            Db.DalEntrapmentEvidenceOps.Add(operation);
            Context.DalEntrapmentEvidenceOps.Add(operation);
        }

        public void Revert(DalEntrapmentEvidenceOperation operation)
        {
            #region init
            var accuser = Cache.Accounts.GetDelegate(operation.AccuserId);
            var offender = Cache.Accounts.GetDelegate(operation.OffenderId);
            #endregion

            #region revert operation
            Db.TryAttach(accuser);
            accuser.DalEntrapmentEvidenceOpsCount--;

            if (offender.Id != accuser.Id)
            {
                Db.TryAttach(offender);
                offender.DalEntrapmentEvidenceOpsCount--;
            }

            Cache.AppState.Get().DalEntrapmentEvidenceOpsCount--;
            #endregion

            Db.DalEntrapmentEvidenceOps.Remove(operation);
            Cache.AppState.ReleaseOperationId();
        }

        protected virtual int GetTrapLevel(JsonElement content)
        {
            return content.Required("attestation").Required("operations").RequiredInt32("level");
        }

        protected virtual int GetConsensusSlot(JsonElement content)
        {
            return content.Required("attestation").Required("operations").RequiredInt32("slot");
        }

        async Task<int> GetAttester(int level, int slot)
        {
            var cycleIndex = Context.Protocol.GetCycle(level);
            var cycle = await Db.Cycles.SingleAsync(x => x.Index == cycleIndex);

            var bakerCycles = await Cache.BakerCycles.GetAsync(cycle.Index);
            var sampler = GetSampler(bakerCycles.Values
                .Where(x => x.BakingPower > 0)
                .Select(x => (x.BakerId, x.BakingPower))
                .ToList());

            return RightsGenerator.GetAttester(sampler, cycle, level, slot);
        }

        Sampler GetSampler(IEnumerable<(int id, long stake)> selection)
        {
            var sorted = selection.OrderByDescending(x =>
            {
                var baker = Cache.Accounts.GetDelegate(x.id);
                return new byte[] { (byte)baker.PublicKey![0] }.Concat(Base58.Parse(baker.Address));
            }, new BytesComparer());

            return new Sampler([..sorted.Select(x => x.id)], [..sorted.Select(x => x.stake)]);
        }
    }
}
