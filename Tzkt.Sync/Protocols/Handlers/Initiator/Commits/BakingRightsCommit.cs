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
        public Protocol Protocol { get; private set; }
        public IEnumerable<Account> BootstrapedAccounts { get; private set; }

        BakingRightsCommit(ProtocolHandler protocol) : base(protocol) { }

        public Task Init(Block block, IEnumerable<Account> accounts)
        {
            Protocol = block.Protocol;
            BootstrapedAccounts = accounts;
            return Task.CompletedTask;
        }

        public Task Init(Block block)
        {
            return Task.CompletedTask;
        }

        public override async Task Apply()
        {
            Proto.TouchAccounts(1);
            await Db.SaveChangesAsync();

            var delegates = BootstrapedAccounts
                .Where(x => x.Type == AccountType.Delegate)
                .ToDictionary(k => k.Address, v => v.Id);

            if (delegates.Count > 0)
            {
                for (int cycle = 0; cycle <= 5; cycle++)
                {
                    var rights = await Task.WhenAll(
                        Proto.Node.GetBakingRightsAsync(1, cycle, BakingRight.MaxPriority + 1),
                        Proto.Node.GetEndorsingRightsAsync(1, cycle));

                    var bakingRights = await (Proto.Serializer as Serializer).DeserializeBakingRights(rights[0]);
                    var endorsingRights = await (Proto.Serializer as Serializer).DeserializeEndorsingRights(rights[1]);

                    var conn = Db.Database.GetDbConnection() as NpgsqlConnection;
                    using var writer = conn.BeginBinaryImport(@"COPY ""BakingRights"" (""Cycle"", ""Level"", ""BakerId"", ""Type"", ""Status"", ""Priority"", ""Slots"") FROM STDIN (FORMAT BINARY)");
                    
                    foreach (var br in bakingRights.Skip(cycle == 0 ? BakingRight.MaxPriority + 1 : 0))
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

                    foreach (var er in endorsingRights)
                    {
                        writer.StartRow();
                        writer.Write(er.Level / Protocol.BlocksPerCycle, NpgsqlTypes.NpgsqlDbType.Integer); // level + 1
                        writer.Write(er.Level + 1, NpgsqlTypes.NpgsqlDbType.Integer);                       // level + 1
                        writer.Write(delegates[er.Delegate], NpgsqlTypes.NpgsqlDbType.Integer);
                        writer.Write((byte)BakingRightType.Endorsing, NpgsqlTypes.NpgsqlDbType.Smallint);
                        writer.Write((byte)BakingRightStatus.Future, NpgsqlTypes.NpgsqlDbType.Smallint);
                        writer.WriteNull();
                        writer.Write(er.Slots.Count, NpgsqlTypes.NpgsqlDbType.Integer);
                    }

                    writer.Complete();
                    GC.Collect();
                }
            }
        }

        public override async Task Revert()
        {
            await Db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""BakingRights""");
        }

        #region static
        public static async Task<BakingRightsCommit> Apply(ProtocolHandler proto, Block block, IEnumerable<Account> accounts)
        {
            var commit = new BakingRightsCommit(proto);
            await commit.Init(block, accounts);
            await commit.Apply();

            return commit;
        }

        public static async Task<BakingRightsCommit> Revert(ProtocolHandler proto, Block block)
        {
            var commit = new BakingRightsCommit(proto);
            await commit.Init(block);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
