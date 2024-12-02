﻿using System.Numerics;
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

        protected virtual BigInteger? GetDalAttestation(JsonElement content) {
            return content.OptionalBigInteger("dal_attestation");
        }

        protected override async Task ApplyDalAttestations(EndorsementOperation endorsement, Block block, JsonElement content) {

            if (GetDalAttestation(content) is BigInteger endorsementDalAttestation)
            {
                var currentRights = await Cache.DalRights.GetAsync(block.Cycle, block.Level);
                if (!currentRights.TryGetValue(endorsement.DelegateId, out DalRight attesterRight))
                {
                    throw new Exception($"No right found the for the attester {endorsement.Delegate.Address}");
                }

                var shardsThreshold = Math.Round((block.Protocol.DalAttestationThreshold / 100.0f) *
                                                 (block.Protocol.DalShardsPerSlot), MidpointRounding.AwayFromZero);

                var dalAttestations = new List<DalAttestation>(block.Protocol.DalSlotsPerLevel);
                var dalCommits = new List<DalPublishCommitmentOperation>(block.Protocol.DalSlotsPerLevel);

                for (int slot = 0; slot < block.Protocol.DalSlotsPerLevel; slot++)
                {
                    var commitmentStatus = await Cache.DalPublishCommitmentOps.GetOrDefaultAsync(endorsement.Level - block.Protocol.DalAttestationLag, slot);
                    if (commitmentStatus != null)
                    {
                        var dalAttestation = new DalAttestation
                        {
                            DalPublishCommitmentOpsId = commitmentStatus.Id,
                            AttestationId = endorsement.Id,
                            Attested = endorsementDalAttestation.Mem(slot),
                            ShardsCount = attesterRight.Shards,
                        };
                        dalAttestations.Add(dalAttestation);
                        Cache.DalAttestations.Add(block.Level, slot, endorsement.Delegate, dalAttestation);

                        if (dalAttestation.Attested)
                        {
                            commitmentStatus.ShardsAttested += dalAttestation.ShardsCount;
                            commitmentStatus.Attested = (commitmentStatus.ShardsAttested >= shardsThreshold);
                            dalCommits.Add(commitmentStatus);
                        }
                    }
                }

                Db.DalAttestations.AddRange(dalAttestations);
                Db.DalPublishCommitmentOps.UpdateRange(dalCommits);
            }
        }

        protected override async Task RevertDalAttestations(EndorsementOperation endorsement, Block block) {
            var shardsThreshold = Math.Round((block.Protocol.DalAttestationThreshold / 100.0f) *
                                             (block.Protocol.DalShardsPerSlot), MidpointRounding.AwayFromZero);

            var dalAttestations = new List<DalAttestation>(block.Protocol.DalSlotsPerLevel);
            var dalCommits = new List<DalPublishCommitmentOperation>(block.Protocol.DalSlotsPerLevel);

            for (int slot = 0; slot < block.Protocol.DalSlotsPerLevel; slot++)
            {
                var dalAttestation = Cache.DalAttestations.GetOrDefault(endorsement.Level, slot, endorsement.Delegate);
                var commitmentStatus = await Cache.DalPublishCommitmentOps.GetOrDefaultAsync(endorsement.Level - block.Protocol.DalAttestationLag, slot);
                if (dalAttestation != null)
                {
                    if (commitmentStatus != null && dalAttestation.Attested)
                    {
                        commitmentStatus.ShardsAttested -= dalAttestation.ShardsCount;
                        commitmentStatus.Attested = (commitmentStatus.ShardsAttested >= shardsThreshold);
                        dalCommits.Add(commitmentStatus);
                    }
                    dalAttestations.Add(dalAttestation);
                }
            }
            Db.DalAttestations.RemoveRange(dalAttestations);
            Db.DalPublishCommitmentOps.UpdateRange(dalCommits);
        }
    }
}
