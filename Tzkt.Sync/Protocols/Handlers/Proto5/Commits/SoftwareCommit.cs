using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto5
{
    class SoftwareCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(Block block, JsonElement rawBlock)
        {
            var version = rawBlock.Required("header").RequiredString("proof_of_work_nonce")[..8];
            var software = await Cache.Software.GetOrCreateAsync(version, () => new Software
            {
                Id = Cache.AppState.NextSoftwareId(),
                FirstLevel = block.Level,
                LastLevel = block.Level,
                ShortHash = version
            });

            if (software.BlocksCount == 0)
                Db.Software.Add(software);
            else
                Db.TryAttach(software);

            software.BlocksCount++;
            software.LastLevel = block.Level;

            block.SoftwareId = software.Id;

            var blockProducer = Cache.Accounts.GetDelegate(block.ProducerId!.Value);
            //Db.TryAttach(blockProducer);
            if (blockProducer.SoftwareId != software.Id)
            {
                blockProducer.SoftwareId = software.Id;
                blockProducer.SoftwareUpdateLevel = block.Level;
            }
        }

        public virtual async Task Revert(Block block)
        {
            var software = await Cache.Software.GetAsync(block.SoftwareId!.Value);

            Db.TryAttach(software);
            software.BlocksCount--;

            // don't revert Baker.SoftwareId and Software.LastLevel
            // don't remove emptied software for historical purposes
        }
    }
}
