using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Tzkt.Api.Services.Output
{
    public class ResponseCacheService
    {
        readonly ILogger Logger;
        readonly Dictionary<string, byte[]> Cache;
        readonly long CacheSize;
        long Used = 0;

        public ResponseCacheService(IConfiguration configuration, ILogger<ResponseCacheService> logger)
        {
            Logger = logger;
            CacheSize = configuration.GetOutputCacheConfig().CacheSize * 1024 * 1024;
            Cache = new Dictionary<string, byte[]>(4096);
        }

        public bool TryGet(string key, out byte[] response)
        {
            lock (Cache)
            {
                return Cache.TryGetValue(key, out response);
            }
        }

        public byte[] Set(string key, object obj)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(obj);
            var size = bytes.Length + key.Length + 20; // up to 4 bytes str len, 8 bytes key ptr, 8 bytes value ptr

            if (size > CacheSize)
            {
                Logger.LogWarning("Response size {response} exceeds cache size {cache}", size, CacheSize);
                return bytes;
            }
            
            lock (Cache)
            {
                if (Used + size >= CacheSize)
                    Clear(); // TODO: do not clear everything, but the oldest entries

                Used += size;
                Cache[key] = bytes;
            }

            return bytes;
        }

        public void Clear()
        {
            lock (Cache)
            {
                Cache.Clear();
                Used = 0;
            }
        }

        public static string BuildKey(string path, params (string, object)[] query)
        {
            var sb = new StringBuilder(path);

            foreach (var (name, value) in query)
                if (value != null)
                    sb.Append(value is INormalizable normalizable ? normalizable.Normalize(name) : $"{name}={value}");

            return sb.ToString();
        }
    }
}