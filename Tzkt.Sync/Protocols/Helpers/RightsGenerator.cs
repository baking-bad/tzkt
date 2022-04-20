using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols
{
    class RightsGenerator
    {
        readonly Sampler Sampler;
        readonly byte[] Seed;

        RightsGenerator(Sampler sampler, byte[] seed)
        {
            Sampler = sampler;
            Seed = new byte[seed.Length + 8];
            Buffer.BlockCopy(seed, 0, Seed, 0, seed.Length);
        }

        IEnumerable<int> EnumerateBakingRights(int position, int rounds)
        {
            WriteInt32(Seed, 32, position);
            for (var round = 0; round < rounds; round++)
            {
                WriteInt32(Seed, 36, round);
                yield return Sampler.GetBaker(Seed);
            }
        }

        List<int> GetBakingRights(int position, int rounds)
        {
            WriteInt32(Seed, 32, position);
            var result = new List<int>(rounds);
            for (var round = 0; round < rounds; round++)
            {
                WriteInt32(Seed, 36, round);
                result.Add(Sampler.GetBaker(Seed));
            }
            return result;
        }

        Dictionary<int, int> GetEndorsingRights(int position, int slots)
        {
            WriteInt32(Seed, 32, position);
            var result = new Dictionary<int, int>();
            for (var slot = 0; slot < slots; slot++)
            {
                WriteInt32(Seed, 36, slot);
                var baker = Sampler.GetBaker(Seed);
                result.TryGetValue(baker, out var count);
                result[baker] = count + 1;
            }
            return result;
        }

        static void WriteInt32(byte[] bytes, int pos, int value)
        {
            bytes[pos + 3] = (byte)(value & 0xFF);
            value >>= 8;
            bytes[pos + 2] = (byte)(value & 0xFF);
            value >>= 8;
            bytes[pos + 1] = (byte)(value & 0xFF);
            value >>= 8;
            bytes[pos] = (byte)value;
        }

        public static async Task<IEnumerable<BR>> GetBakingRightsAsync(Sampler sampler, Protocol protocol, Cycle cycle)
        {
            var rounds = BakingRight.MaxRound + 1;
            var res = new List<BR>(protocol.BlocksPerCycle * rounds);
            var step = (int)Math.Ceiling((double)protocol.BlocksPerCycle / Environment.ProcessorCount);
            var tasks = new List<Task>();
            for (int i = 0; i < protocol.BlocksPerCycle; i += step)
            {
                var from = i;
                var to = Math.Min(protocol.BlocksPerCycle, i + step);
                tasks.Add(Task.Run(() =>
                {
                    var generator = new RightsGenerator(sampler, cycle.Seed);
                    for (int position = from; position < to; position++)
                    {
                        var rights = generator.GetBakingRights(position, rounds);
                        lock (res)
                        {
                            for (int i = 0; i < rights.Count; i++)
                                res.Add(new() { Level = cycle.FirstLevel + position, Round = i, Baker = rights[i]});
                        }
                    }
                }));
            }
            await Task.WhenAll(tasks);
            return res.OrderBy(x => x.Level).ThenBy(x => x.Round);
        }

        public static async Task<IEnumerable<ER>> GetEndorsingRightsAsync(Sampler sampler, Protocol protocol, Cycle cycle)
        {
            var res = new List<ER>(protocol.BlocksPerCycle * sampler.Length);
            var step = (int)Math.Ceiling((double)protocol.BlocksPerCycle / Environment.ProcessorCount);
            var tasks = new List<Task>();
            for (int i = 0; i < protocol.BlocksPerCycle; i += step)
            {
                var from = i;
                var to = Math.Min(protocol.BlocksPerCycle, i + step);
                tasks.Add(Task.Run(() =>
                {
                    var generator = new RightsGenerator(sampler, cycle.Seed);
                    for (int position = from; position < to; position++)
                    {
                        var rights = generator.GetEndorsingRights(position, protocol.EndorsersPerBlock);
                        lock (res)
                        {
                            foreach (var (baker, slots) in rights)
                                res.Add(new() { Level = cycle.FirstLevel + position, Baker = baker, Slots = slots });
                        }
                    }
                }));
            }
            await Task.WhenAll(tasks);
            return res.OrderBy(x => x.Level).ThenByDescending(x => x.Slots);
        }

        public static IEnumerable<BR> EnumerateBakingRights(Sampler sampler, Cycle cycle, int level, int rounds)
        {
            var round = 0;
            var generator = new RightsGenerator(sampler, cycle.Seed);
            foreach (var bakerId in generator.EnumerateBakingRights(level - cycle.FirstLevel, rounds))
            {
                yield return new BR
                {
                    Baker = bakerId,
                    Level = level,
                    Round = round++
                };
            }
        }

        public static IEnumerable<BR> GetBakingRights(Sampler sampler, Cycle cycle, int level, int rounds = BakingRight.MaxRound + 1)
        {
            var generator = new RightsGenerator(sampler, cycle.Seed);
            var rights = generator.GetBakingRights(level - cycle.FirstLevel, rounds);
            var res = new List<BR>(rights.Count);
            for (int i = 0; i < rights.Count; i++)
                res.Add(new() { Level = level, Round = i, Baker = rights[i] });
            return res;
        }

        public static IEnumerable<ER> GetEndorsingRights(Sampler sampler, Protocol protocol, Cycle cycle, int level)
        {
            var generator = new RightsGenerator(sampler, cycle.Seed);
            var rights = generator.GetEndorsingRights(level - cycle.FirstLevel, protocol.EndorsersPerBlock);
            return rights.Select(kv => new ER
            {
                Level = level,
                Baker = kv.Key,
                Slots = kv.Value
            });
        }

        public class ER
        {
            public int Level { get; init; }
            public int Baker { get; init; }
            public int Slots { get; init; }
        }

        public class BR
        {
            public int Level { get; init; }
            public int Round { get; init; }
            public int Baker { get; init; }
        }
    }
}
