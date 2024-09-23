using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;
using Tzkt.Sync.Services.Cache;

namespace Tzkt.Sync.Protocols.Proto19
{
    class EndorsementsCommit : Proto12.EndorsementsCommit
    {
        public EndorsementsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override int GetEndorsedSlots(JsonElement metadata)
        {
            return metadata.RequiredInt32("consensus_power");
        }

        protected override BigInteger? GetDalAttestation(JsonElement content) {
            return content.OptionalBigInteger("dal_attestation");
        }

        protected override async Task ApplyDalAttestations(EndorsementOperation endorsement, Block block) {

            if (endorsement.DalAttestation is BigInteger endorsementDalAttestation)
            {
                var currentRights = await Cache.BakingRights.GetAsync(block.Cycle, block.Level);
                var attesterRight = currentRights
                    .FirstOrDefault(x => x.Type == BakingRightType.Endorsing && x.BakerId == endorsement.DelegateId)
                    ?? throw new Exception($"No right found the for the attester {endorsement.Delegate.Address}");
                var dalAttestations = new List<DalAttestation>(block.Protocol.DalSlotsPerLevel);

                for (int slot = 0; slot < block.Protocol.DalSlotsPerLevel; slot++)
                {
                    var commitmentStatus = await Cache.DalCommitmentStatus.GetOrDefaultAsync(endorsement.Level - block.Protocol.DalAttestationLag, slot);
                    if (commitmentStatus != null)
                    {
                        var dalAttestation = new DalAttestation
                        {
                            DalCommitmentStatusId = commitmentStatus.Id,
                            AttestationId = endorsement.Id,
                            Attested = endorsementDalAttestation.Mem(slot),
                            ShardsCount = attesterRight.DalShards ?? 0,
                        };
                        dalAttestations.Add(dalAttestation);
                    }
                }

                if(dalAttestations.Count > 0)
                {
                    DalAttestationsCache.Add(block.Level, dalAttestations);
                }
                Db.DalAttestations.AddRange(dalAttestations);
            }
        }

        protected override async Task RevertDalAttestations(EndorsementOperation endorsement) {
            DalAttestationsCache.Reset();
            await Db.Database.ExecuteSqlRawAsync($"""
                DELETE FROM "DalAttestations"
                WHERE "AttestationId" = {endorsement.Id}
                """);
        }
    }
}
