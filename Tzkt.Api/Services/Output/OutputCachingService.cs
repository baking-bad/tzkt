using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Tzkt.Api.Services.Output
{
    public class OutputCachingService
    {
        readonly ILogger<OutputCachingService> Logger;

        //TODO Check the Cache size
        readonly OutputCacheConfig Configuration;
        readonly Dictionary<string, OutputCacheEntity> Cache;

        public OutputCachingService(IConfiguration configuration, ILogger<OutputCachingService> logger)
        {
            Logger = logger;
            Configuration = configuration.GetOutputCacheConfig();
            Cache = new Dictionary<string, OutputCacheEntity>();
        }

        //TODO Add method to decompress if the gzip header is missing
        public bool TryGetFromCache(HttpContext context, string key, out OutputCacheEntity response)
        {
            if (!Cache.TryGetValue(key, out response)) return false;

            response.LastAccess = DateTime.UtcNow;
            
            var a = context.Request.Headers["accept-encoding"].ToString();
            var b = a.Contains("gzip");
            
            if (!string.IsNullOrEmpty(a) &&
                a.Contains("gzip"))
            {
                context.Response.Headers.Add("Content-encoding", "gzip");
                return true;
            }

            // TODO Decompress and send
            Console.WriteLine($"Decompressing");
            return true;

        }

        public void Set(string key, object res)
        {
            using(var memStream = new MemoryStream())
            {
                using (var zipStream = new GZipStream(memStream, CompressionMode.Compress, true))
                {
                    using (var jsonWriter = new Utf8JsonWriter(zipStream))
                    {
                        JsonSerializer.Serialize(jsonWriter, res);
                    }
                }

                var bytesToBeCached = memStream.ToArray();

                var cacheSize = Cache.Sum(x => x.Value.Cache.Length);
                while (cacheSize + bytesToBeCached.Length >= Configuration.CacheSize * 1_000_000)
                {
                    var a = Cache.FirstOrDefault(x => x.Value.LastAccess == Cache.Min(y => y.Value.LastAccess)).Key;
                    Cache.Remove(Cache.FirstOrDefault(x => x.Value.LastAccess == Cache.Min(y => y.Value.LastAccess)).Key);
                }

                Cache[key] = new OutputCacheEntity
                {
                    Cache = bytesToBeCached,
                    LastAccess = DateTime.UtcNow
                };
            }
        }

        public void Invalidate()
        {
            //TODO Invalidate every block
            // Cache.Clear();
        }
    }
}