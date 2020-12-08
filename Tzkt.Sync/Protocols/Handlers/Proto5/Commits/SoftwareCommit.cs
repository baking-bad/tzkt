using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto5
{
    class SoftwareCommit : ProtocolCommit
    {
        public SoftwareCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement rawBlock)
        {
            var version = rawBlock.Required("header").RequiredString("proof_of_work_nonce").Substring(0, 8);
            var software = await Cache.Software.GetOrCreateAsync(version, () => new Software
            {
                FirstLevel = block.Level,
                ShortHash = version
            });

            if (software.Id == 0)
                Db.Software.Add(software);
            else
                Db.TryAttach(software);

            software.BlocksCount++;
            software.LastLevel = block.Level;

            block.Software = software;
            block.Baker.Software = software;
        }

        public virtual async Task Revert(Block block)
        {
            var software = await Cache.Software.GetAsync(block.SoftwareId);

            Db.TryAttach(software);
            software.BlocksCount--;

            // don't revert Baker.SoftwareId and Software.LastLevel
            // don't remove emptied software for historical purposes
        }
    }
}
