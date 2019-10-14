using System.Threading.Tasks;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto1
{
    class CounterPreprocessor : IPreprocessor
    {
        readonly RawBlock Block;
        readonly CacheService Cache;

        public CounterPreprocessor(ProtocolHandler protocol, RawBlock block)
        {
            Block = block;
            Cache = protocol.Cache;
        }

        public async Task Run()
        {
            var globalCounter = (await Cache.GetAppStateAsync()).Counter;

            foreach (var op in Block.Operations[3])
                foreach (var content in op.Contents)
                    (content as IManagerOperationContent).GlobalCounter = ++globalCounter;
        }
    }
}
