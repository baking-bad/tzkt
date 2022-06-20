using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace Tzkt.Sync
{
    public sealed class TzktClient : IDisposable
    {
        readonly Uri BaseAddress;
        readonly TimeSpan RequestTimeout;

        DateTime _Expiration;
        HttpClient _HttpClient;
        
        HttpClient HttpClient
        {
            get
            {
                lock (BaseAddress)
                {
                    if (DateTime.UtcNow > _Expiration)
                    {
                        _HttpClient?.Dispose();
                        _HttpClient = new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip });

                        _HttpClient.BaseAddress = BaseAddress;
                        _HttpClient.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/json"));
                        _HttpClient.DefaultRequestHeaders.UserAgent.Add(
                            new ProductInfoHeaderValue("TzKT-Indexer", Assembly.GetExecutingAssembly().GetName().Version.ToString()));
                        _HttpClient.Timeout = RequestTimeout;

                        _Expiration = DateTime.UtcNow.AddMinutes(120);
                    }
                }

                return _HttpClient;
            }
        }

        public TzktClient(string baseUri, int timeoutSec = 10)
        {
            if (string.IsNullOrEmpty(baseUri))
                throw new ArgumentNullException(nameof(baseUri));

            if (!Uri.IsWellFormedUriString(baseUri, UriKind.Absolute))
                throw new ArgumentException("Invalid URI");

            BaseAddress = new Uri($"{baseUri.TrimEnd('/')}/");
            RequestTimeout = TimeSpan.FromSeconds(timeoutSec);
        }

        public Task<Stream> GetStreamAsync(string path)
            => HttpClient.GetStreamAsync(path);

        public async Task<T> GetObjectAsync<T>(string path)
        {
            using var stream = await HttpClient.GetStreamAsync(path);
            return await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions.Default);
        }

        public void Dispose() => _HttpClient?.Dispose();
    }
}
