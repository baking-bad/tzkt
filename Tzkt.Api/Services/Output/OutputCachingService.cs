using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Tzkt.Api.Services.Output
{
    public class OutputCachingService
    {
        readonly ILogger<OutputCachingService> Logger;

        readonly long CacheSizeLimit;
        readonly Dictionary<string, OutputCacheEntity> Cache;

        public OutputCachingService(IConfiguration configuration, ILogger<OutputCachingService> logger)
        {
            Logger = logger;
            CacheSizeLimit = configuration.GetOutputCacheConfig().CacheSize * 1_000_000;
            Cache = new Dictionary<string, OutputCacheEntity>();
        }

        //TODO Add method to decompress if the gzip header is missing
        public bool TryGetFromCache(HttpContext context, string key, out OutputCacheEntity response)
        {
            if (!Cache.TryGetValue(key, out var cacheEntity))
            {
                response = null;
                return false;
            }
            
            //TODO Use CompressionLimit. There's no need to decompress small objects (if it's not compressed)

            cacheEntity.LastAccess = DateTime.UtcNow;
            
            var acceptEncodingHeaders = context.Request.Headers["accept-encoding"].ToString();
            
            if (!string.IsNullOrEmpty(acceptEncodingHeaders) && acceptEncodingHeaders.Contains("gzip"))
            {
                context.Response.Headers.Add("Content-encoding", "gzip");
                response = cacheEntity;
                return true;
            }

            using (var memoryStream = new MemoryStream(cacheEntity.Cache))
            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
            using (var memoryStreamOutput = new MemoryStream())
            {
                gZipStream.CopyTo(memoryStreamOutput);
                response = new OutputCacheEntity
                {
                    Cache = memoryStreamOutput.ToArray(),
                    LastAccess = DateTime.UtcNow
                };
            }

            return true;

        }

        public void Set(string key, object res)
        {
            //TODO Use CompressionLimit. There's no need to compress small objects
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

                if (bytesToBeCached.Length > CacheSizeLimit)
                {
                    Logger.LogWarning($"{key} too big to be cached. Cache size: {CacheSizeLimit} bytes. Response size: {bytesToBeCached.Length} bytes");
                    return;
                }
                
                while (Cache.Any() && Cache.Sum(x => x.Value.Cache.Length) + bytesToBeCached.Length >= CacheSizeLimit)
                {
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
            Cache.Clear();
        }
    }
}