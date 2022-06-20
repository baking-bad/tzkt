using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tzkt.Api.Services
{
    public class ResponseCacheService
    {
        readonly JsonSerializerOptions Options;
        readonly ILogger Logger;
        readonly Dictionary<string, byte[]> Cache;
        readonly long CacheSize;
        long Used = 0;
        int Hits = 0;
        int Misses = 0;

        public ResponseCacheService(IConfiguration configuration, IOptions<JsonOptions> options, ILogger<ResponseCacheService> logger)
        {
            Options = options.Value.JsonSerializerOptions;
            Logger = logger;
            CacheSize = configuration.GetOutputCacheConfig().CacheSize * 1024 * 1024;
            Cache = new Dictionary<string, byte[]>(4096);
        }

        public bool TryGet(string key, out byte[] response)
        {
            lock (Cache)
            {
                if (Cache.TryGetValue(key, out response))
                {
                    Hits++;
                    return true;
                }
                else
                {
                    Misses++;
                    return false;
                }
            }
        }

        public byte[] Set(string key, object obj, bool isSerialized = false)
        {
            var bytes = obj == null 
                ? null
                : isSerialized 
                    ? Encoding.UTF8.GetBytes((obj as string)!)
                    : JsonSerializer.SerializeToUtf8Bytes(obj, Options);
            var size = (bytes?.Length ?? 0) + key.Length + 20; // up to 4 bytes str len, 8 bytes key ptr, 8 bytes value ptr

            if (size > CacheSize)
            {
                if (CacheSize != 0)
                    Logger.LogWarning("Response size {response} exceeds cache size {cache}", size, CacheSize);
                return bytes;
            }
            
            lock (Cache)
            {
                if (Used + size >= CacheSize)
                {
                    Logger.LogInformation("Cache size limit reached");
                    Clear(); // TODO: do not clear everything, but the oldest entries
                }

                Used += size;
                Cache[key] = bytes;
            }

            return bytes;
        }

        public void Clear()
        {
            lock (Cache)
            {
                Logger.LogDebug("Cache used: {used} of {limit}", Used, CacheSize);
                Logger.LogDebug("Cache hits/misses: {hits}/{misses}", Hits, Misses);
                Cache.Clear();
                Used = 0;
                Hits = 0;
                Misses = 0;
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