using System.Text.Json;
using Netezos.Encoding;
using Netezos.Forging;
using Netezos.Forging.Models;
using Netezos.Keys;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class DoublePreendorsingCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var accusedLevel = content.Required("op1").Required("operations").RequiredInt32("level");

            var accuser = Context.Proposer;
            var offender = await GetPreendorser(op.RequiredString("chain_id"), content.Required("op1"));

            var operation = new DoublePreendorsingOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = block.Level,
                Timestamp = block.Timestamp,
                OpHash = op.RequiredString("hash"),

                AccusedLevel = accusedLevel,
                SlashedLevel = GetSlashingLevel(block, Context.Protocol, accusedLevel),

                AccuserId = accuser.Id,
                OffenderId = offender.Id,

                Reward = 0,
                LostStaked = 0,
                LostUnstaked = 0,
                LostExternalStaked = 0,
                LostExternalUnstaked = 0
            };
            #endregion

            #region apply operation
            Db.TryAttach(accuser);
            accuser.DoublePreendorsingCount++;

            if (offender != accuser)
            {
                Db.TryAttach(offender);
                offender.DoublePreendorsingCount++;
            }

            block.Operations |= Operations.DoublePreendorsings;

            Cache.AppState.Get().DoublePreendorsingOpsCount++;
            #endregion

            Db.DoublePreendorsingOps.Add(operation);
            Context.DoublePreendorsingOps.Add(operation);
        }

        public void Revert(DoublePreendorsingOperation operation)
        {
            #region init
            var accuser = Cache.Accounts.GetDelegate(operation.AccuserId);
            var offender = Cache.Accounts.GetDelegate(operation.OffenderId);
            #endregion

            #region revert operation
            Db.TryAttach(accuser);
            accuser.DoublePreendorsingCount--;

            if (offender != accuser)
            {
                Db.TryAttach(offender);
                offender.DoublePreendorsingCount--;
            }

            Cache.AppState.Get().DoublePreendorsingOpsCount--;
            #endregion

            Db.DoublePreendorsingOps.Remove(operation);
            Cache.AppState.ReleaseOperationId();
        }

        protected async Task<Data.Models.Delegate> GetPreendorser(string chainId, JsonElement op)
        {
            var branch = op.RequiredString("branch");
            var content = op.Required("operations");
            var preattestation = new PreattestationContent
            {
                Level = content.RequiredInt32("level"),
                Round = content.RequiredInt32("round"),
                Slot = content.RequiredInt32("slot"),
                PayloadHash = content.RequiredString("block_payload_hash")
            };
            var signature = Base58.Parse(op.RequiredString("signature"), 3);

            var bytes = new byte[1] { 18 }
                .Concat(Base58.Parse(chainId, 3))
                .Concat(await new LocalForge().ForgeOperationAsync(branch, preattestation))
                .ToArray();

            foreach (var baker in Cache.Accounts.GetDelegates().OrderByDescending(x => x.LastLevel))
                if (PubKey.FromBase58(baker.PublicKey!).Verify(bytes, signature))
                    return baker;

            throw new Exception("Failed to determine double preendorser");
        }

        protected virtual int GetSlashingLevel(Block block, Protocol protocol, int accusedLevel)
        {
            return Cache.Protocols.GetCycleEnd(block.Cycle);
        }
    }
}
