using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Sync.Protocols.Proto6
{
    class BakerCyclesMigration : ProtocolCommit
    {
        BakerCyclesMigration(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply()
        {
            var block = await Cache.Blocks.CurrentAsync();
            var protocol = await Cache.Protocols.GetAsync(block.ProtoCode);
            var cycle = (block.Level - 1) / protocol.BlocksPerCycle;

            await Db.Database.ExecuteSqlRawAsync($@"
                UPDATE  ""BakerCycles""
                SET     ""FutureBlockRewards"" = ""FutureBlocks"" * 40000000 :: bigint,
                        ""FutureEndorsementRewards"" = ""FutureEndorsements"" * 1250000 :: bigint
                WHERE ""Cycle"" > {cycle};");
        }

        public override async Task Revert()
        {
            var block = await Cache.Blocks.CurrentAsync();
            var protocol = await Cache.Protocols.GetAsync(block.ProtoCode);
            var cycle = (block.Level - 1) / protocol.BlocksPerCycle;

            await Db.Database.ExecuteSqlRawAsync($@"
                UPDATE  ""BakerCycles""
                SET     ""FutureBlockRewards"" = ""FutureBlocks"" * 16000000 :: bigint,
                        ""FutureEndorsementRewards"" = ""FutureEndorsements"" * 2000000 :: bigint
                WHERE ""Cycle"" > {cycle};");
        }

        #region static
        public static async Task<BakerCyclesMigration> Apply(ProtocolHandler proto)
        {
            var commit = new BakerCyclesMigration(proto);
            await commit.Apply();

            return commit;
        }

        public static async Task<BakerCyclesMigration> Revert(ProtocolHandler proto)
        {
            var commit = new BakerCyclesMigration(proto);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
