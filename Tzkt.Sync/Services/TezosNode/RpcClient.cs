using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Tzkt.Sync.Services
{
    public sealed class RpcClient : IDisposable
    {
        readonly Uri BaseAddress;
        readonly TimeSpan RequestTimeout;

        DateTime _Expiration;
        HttpClient _HttpClient;
        
        HttpClient HttpClient
        {
            get
            {
                lock (this)
                {
                    if (DateTime.UtcNow > _Expiration)
                    {
                        _HttpClient?.Dispose();
                        _HttpClient = new HttpClient();

                        _HttpClient.BaseAddress = BaseAddress;
                        _HttpClient.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/json"));
                        _HttpClient.Timeout = RequestTimeout;

                        _Expiration = DateTime.UtcNow.AddMinutes(120);
                    }
                }

                return _HttpClient;
            }
        }

        public RpcClient(string baseUri, int timeoutSec = 10)
        {
            if (string.IsNullOrEmpty(baseUri))
                throw new ArgumentNullException(nameof(baseUri));

            if (!Uri.IsWellFormedUriString(baseUri, UriKind.Absolute))
                throw new ArgumentException("Invalid URI");

            BaseAddress = new Uri(baseUri);
            RequestTimeout = TimeSpan.FromSeconds(timeoutSec);
        }

        public Task<byte[]> GetBytesAsync(string path)
            => HttpClient.GetByteArrayAsync(path);

        public Task<string> GetStringAsync(string path)
            => HttpClient.GetStringAsync(path);

        public Task<Stream> GetStreamAsync(string path)
            => HttpClient.GetStreamAsync(path);

        public async Task<T> GetObjectAsync<T>(string path, JsonSerializerOptions options = null)
            => await JsonSerializer.DeserializeAsync<T>(await HttpClient.GetStreamAsync(path), options);

        public void Dispose()
            => _HttpClient?.Dispose();
    }
}
