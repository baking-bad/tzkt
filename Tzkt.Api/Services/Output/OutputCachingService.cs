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
        readonly long CompressionLimit;
        readonly Dictionary<string, OutputCacheEntity> Cache;
        
        long CacheUsed = 0;

        public OutputCachingService(IConfiguration configuration, ILogger<OutputCachingService> logger)
        {
            Logger = logger;
            CacheSizeLimit = configuration.GetOutputCacheConfig().CacheSize * 1_000_000;
            CompressionLimit = configuration.GetOutputCacheConfig().CompressionLimit;
            Cache = new Dictionary<string, OutputCacheEntity>();
        }

        public bool TryGetFromCache(HttpContext context, string key, out OutputCacheEntity response)
        {
            OutputCacheEntity cacheEntity;
            lock (Cache)
            {
                if (!Cache.TryGetValue(key, out cacheEntity))
                {
                    response = null;
                    return false;
                }
            }

            lock (cacheEntity)
            {
                cacheEntity.LastAccess = DateTime.UtcNow;

                if (!cacheEntity.IsCompressed)
                {
                    response = cacheEntity;
                    return true;
                }
            
                var acceptEncodingHeaders = context.Request.Headers["accept-encoding"].ToString();
            
                if (!string.IsNullOrEmpty(acceptEncodingHeaders) && acceptEncodingHeaders.Contains("gzip"))
                {
                    context.Response.Headers.Add("Content-encoding", "gzip");
                    response = cacheEntity;
                    return true;
                }

                using (var memoryStream = new MemoryStream(cacheEntity.Bytes))
                using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                using (var memoryStreamOutput = new MemoryStream())
                {
                    gZipStream.CopyTo(memoryStreamOutput);
                    response = new OutputCacheEntity
                    {
                        Bytes = memoryStreamOutput.ToArray(),
                        LastAccess = DateTime.UtcNow,
                        IsCompressed = false
                    };
                }

                return true;
            }
        }

        public void Set(string key, object res)
        {
            var buffer = JsonSerializer.SerializeToUtf8Bytes(res);
            byte[] bytesToBeCached;
            var compressed = false;
            if (buffer.Length < CompressionLimit)
            {
                bytesToBeCached = buffer;
            }
            else
            {
                using (var outStream = new MemoryStream())
                {
                    using (var zipStream = new GZipStream(outStream, CompressionMode.Compress))
                    using (var mStream = new MemoryStream(buffer))
                        mStream.CopyTo(zipStream);

                    bytesToBeCached = outStream.ToArray();
                    compressed = true;
                }
            }
            
            if (bytesToBeCached.Length > CacheSizeLimit)
            {
                Logger.LogWarning("{Key} too big to be cached. Cache size: {CacheSizeLimit} bytes. Response size: {BytesToBeCached} bytes", key, CacheSizeLimit, bytesToBeCached.Length);
                return;
            }

            lock (Cache)
            {
                if (Cache.Any() && CacheUsed + bytesToBeCached.Length >= CacheSizeLimit)
                {
                    var toDelete = Cache.OrderBy(x => x.Value.LastAccess).Select(x => x.Key).ToList();
                
                    while (Cache.Any() && CacheUsed + bytesToBeCached.Length >= CacheSizeLimit)
                    {
                        var keyToDelete = toDelete.FirstOrDefault();
                        CacheUsed -= Cache[keyToDelete].Bytes.Length;
                        Cache.Remove(keyToDelete);
                        toDelete.RemoveAt(0);
                    }
                }
                
                CacheUsed += bytesToBeCached.Length;
                Cache[key] = new OutputCacheEntity
                {
                    Bytes = bytesToBeCached,
                    LastAccess = DateTime.UtcNow,
                    IsCompressed = compressed
                };
            }
        }

        public void Invalidate()
        {
            lock (Cache)
            {
                Cache.Clear();
                CacheUsed = 0;
            }
        }
    }
}