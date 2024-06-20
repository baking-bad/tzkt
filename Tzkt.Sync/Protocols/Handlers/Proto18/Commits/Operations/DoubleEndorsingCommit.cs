using System.Text.Json;
using Netezos.Encoding;
using Netezos.Forging;
using Netezos.Forging.Models;
using Netezos.Keys;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class DoubleEndorsingCommit : ProtocolCommit
    {
        public DoubleEndorsingCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var accusedLevel = content.Required("op1").Required("operations").RequiredInt32("level");

            var accuser = block.Proposer;
            var offender = await GetEndorser(op.RequiredString("chain_id"), content.Required("op1"));

            var operation = new DoubleEndorsingOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),

                AccusedLevel = accusedLevel,
                SlashedLevel = GetSlashingLevel(block, block.Protocol, accusedLevel),

                Accuser = accuser,
                Offender = offender,

                Reward = 0,
                LostStaked = 0,
                LostUnstaked = 0,
                LostExternalStaked = 0,
                LostExternalUnstaked = 0
            };
            #endregion

            #region apply operation
            Db.TryAttach(accuser);
            accuser.DoubleEndorsingCount++;

            if (offender != accuser)
            {
                Db.TryAttach(offender);
                offender.DoubleEndorsingCount++;
            }

            block.Operations |= Operations.DoubleEndorsings;
            #endregion

            Db.DoubleEndorsingOps.Add(operation);
        }

        public void Revert(DoubleEndorsingOperation operation)
        {
            #region init
            var accuser = Cache.Accounts.GetDelegate(operation.AccuserId);
            var offender = Cache.Accounts.GetDelegate(operation.OffenderId);
            #endregion

            #region revert operation
            Db.TryAttach(accuser);
            accuser.DoubleEndorsingCount--;

            if (offender != accuser)
            {
                Db.TryAttach(offender);
                offender.DoubleEndorsingCount--;
            }
            #endregion

            Db.DoubleEndorsingOps.Remove(operation);
            Cache.AppState.ReleaseOperationId();
        }

        protected async Task<Data.Models.Delegate> GetEndorser(string chainId, JsonElement op)
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
                if (PubKey.FromBase58(baker.PublicKey).Verify(bytes, signature))
                    return baker;

            throw new Exception("Failed to determine double endorser");
        }

        protected virtual int GetSlashingLevel(Block block, Protocol protocol, int accusedLevel)
        {
            return Cache.Protocols.GetCycleEnd(block.Cycle);
        }
    }
}
