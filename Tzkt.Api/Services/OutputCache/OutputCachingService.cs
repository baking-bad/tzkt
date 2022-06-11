using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Tzkt.Api.Services.Output
{
    public class OutputCachingService
    {
        readonly ILogger<OutputCachingService> Logger;

        readonly long CacheSizeLimit;
        readonly Dictionary<string, byte[]> Cache;
        
        long CacheUsed = 0;

        public OutputCachingService(IConfiguration configuration, ILogger<OutputCachingService> logger)
        {
            Logger = logger;
            CacheSizeLimit = configuration.GetOutputCacheConfig().CacheSize * 1_000_000;
            Cache = new Dictionary<string, byte[]>();
        }

        public bool TryGetFromCache(string key, out byte[] response)
        {
            lock (Cache)
            {
                return Cache.TryGetValue(key, out response);
            }
        }

        public byte[] Set(string key, object res)
        {
            var bytesToBeCached = JsonSerializer.SerializeToUtf8Bytes(res);
            
            if (bytesToBeCached.Length > CacheSizeLimit)
            {
                Logger.LogWarning("{Key} too big to be cached. Cache size: {CacheSizeLimit} bytes. Response size: {BytesToBeCached} bytes", key, CacheSizeLimit, bytesToBeCached.Length);
                return bytesToBeCached;
            }
            
            lock (Cache)
            {
                if (Cache.Any() && CacheUsed + bytesToBeCached.Length >= CacheSizeLimit)
                {
                    Clear();
                }
                
                CacheUsed += bytesToBeCached.Length;
                Cache[key] = bytesToBeCached;
            }

            return bytesToBeCached;
        }

        public void Clear()
        {
            lock (Cache)
            {
                Cache.Clear();
                CacheUsed = 0;
            }
        }
    }
}