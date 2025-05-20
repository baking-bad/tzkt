using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace Tzkt.Sync
{
    public sealed class TzktClient : IDisposable
    {
        readonly Uri BaseAddress;
        readonly TimeSpan RequestTimeout;

        DateTime _Expiration;
        HttpClient? _HttpClient;
        
        HttpClient HttpClient
        {
            get
            {
                lock (BaseAddress)
                {
                    if (DateTime.UtcNow > _Expiration)
                    {
                        _HttpClient?.Dispose();
                        _HttpClient = new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip })
                        {
                            BaseAddress = BaseAddress,
                            Timeout = RequestTimeout,
                        };
                        _HttpClient.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/json"));
                        _HttpClient.DefaultRequestHeaders.UserAgent.Add(
                            new ProductInfoHeaderValue("TzKT-Indexer", Assembly.GetExecutingAssembly().GetName().Version?.ToString()));

                        _Expiration = DateTime.UtcNow.AddMinutes(120);
                    }
                }
                // TODO: use native factory
                return _HttpClient!;
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

        public async Task<T?> GetObjectAsync<T>(string path)
        {
            using var stream = await HttpClient.GetStreamAsync(path);
            return await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions.Default);
        }
        
        public async Task<T?> PostAsync<T>(string path, string content)
        {
            var response = await HttpClient.PostAsync(path, new JsonContent(content));
            
            using var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions.Default);
        }

        public void Dispose() => _HttpClient?.Dispose();
    }
}
