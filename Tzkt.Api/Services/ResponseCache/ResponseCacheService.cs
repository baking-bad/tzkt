using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using App.Metrics;
using Tzkt.Api.Utils;

namespace Tzkt.Api.Services
{
    public class ResponseCacheService
    {
        readonly JsonSerializerOptions Options;
        readonly ILogger Logger;
        readonly IMetrics Metrics;
        readonly Dictionary<string, byte[]> Cache;
        readonly long CacheSize;
        long CacheUsed = 0;

        public ResponseCacheService(IConfiguration configuration, IOptions<JsonOptions> options, ILogger<ResponseCacheService> logger, IMetrics metrics)
        {
            Options = options.Value.JsonSerializerOptions;
            Logger = logger;
            Metrics = metrics;
            CacheSize = configuration.GetOutputCacheConfig().CacheSize * 1024 * 1024;
            Cache = new Dictionary<string, byte[]>(4096);
        }

        public bool TryGet(string key, out byte[] response)
        {
            lock (Cache)
            {
                if (Cache.TryGetValue(key, out response))
                {
                    Metrics.Measure.Counter.Increment(MetricsRegistry.ResponseCacheCalls, MetricsRegistry.ResponseCacheHit);
                    return true;
                }
                else
                {
                    Metrics.Measure.Counter.Increment(MetricsRegistry.ResponseCacheCalls, MetricsRegistry.ResponseCacheMiss);
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
                if (CacheUsed + size >= CacheSize)
                {
                    Logger.LogWarning("Cache size limit reached");
                    Clear(); // TODO: do not clear everything, but the oldest entries
                }

                CacheUsed += size;
                Cache[key] = bytes;
                Metrics.Measure.Gauge.SetValue(MetricsRegistry.ResponseCacheSize, CacheUsed);
            }

            return bytes;
        }

        public void Clear()
        {
            lock (Cache)
            {
                Cache.Clear();
                CacheUsed = 0;
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