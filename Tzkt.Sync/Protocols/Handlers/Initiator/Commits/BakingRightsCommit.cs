using System;
using System.Collections.Generic;
using System.Linq;
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
        public List<List<RawBakingRight>> FutureBakingRights { get; private set; }
        public List<List<RawEndorsingRight>> FutureEndorsingRights { get; private set; }

        BakingRightsCommit(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply()
        {
            FutureBakingRights = new List<List<RawBakingRight>>(Block.Protocol.PreservedCycles + 1);
            FutureEndorsingRights = new List<List<RawEndorsingRight>>(Block.Protocol.PreservedCycles + 1);

            var delegates = BootstrapedAccounts
                .Where(x => x.Type == AccountType.Delegate)
                .ToDictionary(k => k.Address, v => v.Id);

            for (int cycle = 0; cycle <= Block.Protocol.PreservedCycles; cycle++)
            {
                using var bakingRightsStream = await Proto.Node.GetBakingRightsAsync(1, cycle, BakingRight.MaxPriority + 1);
                var bakingRights = await (Proto.Serializer as Serializer).DeserializeBakingRights(bakingRightsStream);

                using var endorsingRightsStream = await Proto.Node.GetEndorsingRightsAsync(1, cycle);
                var endorsingRights = await (Proto.Serializer as Serializer).DeserializeEndorsingRights(endorsingRightsStream);

                FutureBakingRights.Add(bakingRights);
                FutureEndorsingRights.Add(endorsingRights);

                var conn = Db.Database.GetDbConnection() as NpgsqlConnection;
                using var writer = conn.BeginBinaryImport(@"COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Priority"", ""Slots"") FROM STDIN (FORMAT BINARY)");

                foreach (var er in endorsingRights)
                {
                    writer.StartRow();
                    writer.Write(er.Level / Block.Protocol.BlocksPerCycle, NpgsqlTypes.NpgsqlDbType.Integer); // level + 1 (shifted)
                    writer.Write(er.Level + 1, NpgsqlTypes.NpgsqlDbType.Integer);                             // level + 1 (shifted)
                    writer.Write(delegates[er.Delegate], NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write((byte)BakingRightType.Endorsing, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.Write((byte)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.WriteNull();
                    writer.Write(er.Slots.Count, NpgsqlTypes.NpgsqlDbType.Integer);
                }

                var skipLevel = FutureBakingRights[cycle][0].Level; //skip bootstrap block rights
                foreach (var br in bakingRights.SkipWhile(x => cycle == 0 && x.Level == skipLevel))
                {
                    writer.StartRow();
                    writer.Write(cycle, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(br.Level, NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write(delegates[br.Delegate], NpgsqlTypes.NpgsqlDbType.Integer);
                    writer.Write((byte)BakingRightType.Baking, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.Write((byte)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Smallint);
                    writer.Write(br.Priority, NpgsqlTypes.NpgsqlDbType.Integer);
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
