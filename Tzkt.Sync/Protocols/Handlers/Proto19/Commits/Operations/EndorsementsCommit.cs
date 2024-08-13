using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

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
                var dalAttestationsStatus = new List<DalAttestationStatus>(block.Protocol.DalSlotsPerLevel);

                for (int slot = 0; slot < block.Protocol.DalSlotsPerLevel; slot++)
                {
                    var commitmentStatus = await Cache.DalCommitmentStatus.GetOrDefaultAsync(endorsement.Level - block.Protocol.DalAttestationLag, slot);
                    if (commitmentStatus != null)
                    {
                        var attestationsStatus = new DalAttestationStatus
                        {
                            DalCommitmentStatusId = commitmentStatus.Id,
                            AttestationId = endorsement.Id,
                            Attested = endorsementDalAttestation.Mem(slot),
                        };
                        dalAttestationsStatus.Add(attestationsStatus);
                    }
                }
                Db.DalAttestationStatus.AddRange(dalAttestationsStatus);
            }
        }

        protected override async Task RevertDalAttestations(EndorsementOperation endorsement) {
            await Db.Database.ExecuteSqlRawAsync($"""
                DELETE FROM "DalAttestationStatus"
                WHERE "AttestationId" = {endorsement.Id}
                """);
        }
    }
}
