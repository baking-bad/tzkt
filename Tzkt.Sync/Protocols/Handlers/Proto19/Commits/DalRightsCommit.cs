using Microsoft.EntityFrameworkCore;
using Netezos.Encoding;
using Npgsql;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto19
{
    class DalRightsCommit : ProtocolCommit
    {
        public IEnumerable<RightsGenerator.DR> FutureDalRights { get; protected set; }

        public DalRightsCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, Cycle futureCycle, Dictionary<int, long> selectedStakes)
        {
            if (futureCycle == null) return;
            if (block.Cycle == block.Protocol.FirstCycle)
            {
                var prevProto = await Cache.Protocols.GetAsync(block.Protocol.Code - 1);
                if (prevProto.ConsensusRightsDelay != block.Protocol.ConsensusRightsDelay)
                    return;
            }

            var sampler = GetSampler(
                selectedStakes.Where(x => x.Value > 0).Select(x => (x.Key, x.Value)),
                block.Level == block.Protocol.FirstCycleLevel);

            #region temporary diagnostics
            await sampler.Validate(Proto, block.Level, futureCycle.Index);
            #endregion

            FutureDalRights = await RightsGenerator.GetDalRightsAsync(sampler, block.Protocol, futureCycle);

            var conn = Db.Database.GetDbConnection() as NpgsqlConnection;
            using var writer = conn.BeginBinaryImport(@"
                COPY ""DalRights"" (""Cycle"", ""Level"", ""DelegateId"", ""Shards"")
                FROM STDIN (FORMAT BINARY)");

            foreach (var dr in FutureDalRights)
            {
                writer.StartRow();
                writer.Write(block.Protocol.GetCycle(dr.Level + 1), NpgsqlTypes.NpgsqlDbType.Integer); // level + 1 (shifted)
                writer.Write(dr.Level + 1, NpgsqlTypes.NpgsqlDbType.Integer);                          // level + 1 (shifted)
                writer.Write(dr.Delegate, NpgsqlTypes.NpgsqlDbType.Integer);
                writer.Write(dr.Shards, NpgsqlTypes.NpgsqlDbType.Integer);
            }

            writer.Complete();
        }

        public virtual async Task Revert(Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleBegin))
                return;

            block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);

            if (block.Cycle == block.Protocol.FirstCycle)
            {
                var prevProto = await Cache.Protocols.GetAsync(block.Protocol.Code - 1);
                if (prevProto.ConsensusRightsDelay != block.Protocol.ConsensusRightsDelay)
                    return;
            }

            block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
            await Db.Database.ExecuteSqlRawAsync($@"
                DELETE FROM ""DalRights""
                WHERE   ""Cycle"" = {block.Cycle + block.Protocol.ConsensusRightsDelay}
                OR      ""Level"" > {block.Protocol.GetCycleStart(block.Cycle + block.Protocol.ConsensusRightsDelay)}");
        }

        protected virtual Sampler GetSampler(IEnumerable<(int id, long stake)> selection, bool forceBase)
        {
            var sorted = selection.OrderByDescending(x =>
            {
                var @delegate = Cache.Accounts.GetDelegate(x.id);
                return new byte[] { (byte)@delegate.PublicKey[0] }.Concat(Base58.Parse(@delegate.Address));
            }, new BytesComparer());

            return new Sampler(sorted.Select(x => x.id).ToArray(), sorted.Select(x => x.stake).ToArray());
        }
    }
}
