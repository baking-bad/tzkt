using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Initiator
{
    class BakingRightsCommit : ProtocolCommit
    {
        public Block Block { get; private set; }
        public List<Account> BootstrapedAccounts { get; private set; }
        public List<JsonElement> FutureBakingRights { get; private set; }
        public List<JsonElement> FutureEndorsingRights { get; private set; }

        BakingRightsCommit(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply()
        {
            FutureBakingRights = new List<JsonElement>(Block.Protocol.PreservedCycles + 1);
            FutureEndorsingRights = new List<JsonElement>(Block.Protocol.PreservedCycles + 1);

            var delegates = BootstrapedAccounts
                .Where(x => x.Type == AccountType.Delegate)
                .ToDictionary(k => k.Address, v => v.Id);

            for (int cycle = 0; cycle <= Block.Protocol.PreservedCycles; cycle++)
            {
                var bakingRights = await Proto.Rpc.GetBakingRightsAsync(1, cycle);
                var endorsingRights = await Proto.Rpc.GetEndorsingRightsAsync(1, cycle);

                FutureBakingRights.Add(bakingRights);
                FutureEndorsingRights.Add(endorsingRights);

                var conn = Db.Database.GetDbConnection() as NpgsqlConnection;
                using var writer = conn.BeginBinaryImport(@"COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Priority"", ""Slots"") FROM STDIN (FORMAT BINARY)");

                foreach (var er in endorsingRights.EnumerateArray())
                {
                    writer.StartRow();
                    writer.Write(er.RequiredInt32("level") / Block.Protocol.BlocksPerCycle, NpgsqlTypes.NpgsqlDbType.Integer); // level + 1 (shifted)
                    writer.Write(er.RequiredInt32("level") + 1, NpgsqlTypes.NpgsqlDbType.Integer);                             // level + 1 (shifted)
                    writer.Write(delegates[er.RequiredString("delegate")], NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write((byte)BakingRightType.Endorsing, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.Write((byte)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.WriteNull();
                    writer.Write(er.RequiredArray("slots").Count(), NpgsqlTypes.NpgsqlDbType.Integer);
                }

                var skipLevel = FutureBakingRights[cycle][0].RequiredInt32("level"); //skip bootstrap block rights
                foreach (var br in bakingRights.EnumerateArray().SkipWhile(x => cycle == 0 && x.RequiredInt32("level") == skipLevel))
                {
                    writer.StartRow();
                    writer.Write(cycle, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(br.RequiredInt32("level"), NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(delegates[br.RequiredString("delegate")], NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write((byte)BakingRightType.Baking, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.Write((byte)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.Write(br.RequiredInt32("priority"), NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.WriteNull();
                }

                writer.Complete();
            }
        }

        public override async Task Revert()
        {
            await Db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""BakingRights""");
        }

        #region static
        public static async Task<BakingRightsCommit> Apply(ProtocolHandler proto, Block block, List<Account> accounts)
        {
            var commit = new BakingRightsCommit(proto) { Block = block, BootstrapedAccounts = accounts };
            await commit.Apply();
            return commit;
        }

        public static async Task<BakingRightsCommit> Revert(ProtocolHandler proto)
        {
            var commit = new BakingRightsCommit(proto);
            await commit.Revert();
            return commit;
        }
        #endregion
    }
}
