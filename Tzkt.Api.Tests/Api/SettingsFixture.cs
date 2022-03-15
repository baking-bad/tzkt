using System;
using System.Net.Http;
using Dynamic.Json;
using Netezos.Rpc;

namespace Tzkt.Api.Tests.Api
{
    public class SettingsFixture : IDisposable
    {
        static readonly object Crit = new object();

        public HttpClient Client { get; }

        public SettingsFixture()
        {
            lock (Crit)
            {
                var settings = DJson.Read("../../../Api/settings.json");

                Client = new HttpClient()
                {
                    BaseAddress = new Uri(settings.Url) 
                };
            }
        }

        public void Dispose() => Client.Dispose();
    }
}
