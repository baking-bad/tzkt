using System.Text.Json;
using Netezos.Encoding;
using Netezos.Forging.Models;
using Netezos.Forging;
using Netezos.Keys;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto22
{
    class DalEntrapmentEvidenceCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            // temp check
            if (content.Required("metadata").OptionalArray("balance_updates")?.EnumerateArray().Any() == true)
                throw new Exception("Unexpected balance updates in DalEntrapmentEvidence");

            var accuser = Context.Proposer;
            var offender = await GetAttester(op.RequiredString("chain_id"), content.Required("attestation"));

            var trapLevel = content.Required("attestation").Required("operations").RequiredInt32("level");
            var trapSlotIndex = content.RequiredInt32("slot_index");

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

        protected async Task<Data.Models.Delegate> GetAttester(string chainId, JsonElement op)
        {
            var branch = op.RequiredString("branch");
            var content = op.Required("operations");
            var attestation = new AttestationContent
            {
                Level = content.RequiredInt32("level"),
                Round = content.RequiredInt32("round"),
                Slot = content.RequiredInt32("slot"),
                PayloadHash = content.RequiredString("block_payload_hash"),
                DalAttestation = content.OptionalBigInteger("dal_attestation")
            };
            var signature = Base58.Parse(op.RequiredString("signature"), 3);

            var bytes = new byte[1] { 19 }
                .Concat(Base58.Parse(chainId, 3))
                .Concat(await new LocalForge().ForgeOperationAsync(branch, attestation))
                .ToArray();

            foreach (var baker in Cache.Accounts.GetDelegates().OrderByDescending(x => x.LastLevel))
                if (PubKey.FromBase58(baker.PublicKey!).Verify(bytes, signature))
                    return baker;

            throw new Exception("Failed to determine trapped dal slot attester");
        }
    }
}
