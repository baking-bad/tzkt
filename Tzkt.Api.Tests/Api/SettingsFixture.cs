using System;
using System.Net.Http;
using Dynamic.Json;

namespace Tzkt.Api.Tests.Api
{
    public class SettingsFixture : IDisposable
    {
        static readonly object Crit = new();

        public HttpClient Client { get; }
        public string Baker { get; }
        public string Delegator { get; }
        public string Originator { get; }
        public int Cycle { get; }

        public SettingsFixture()
        {
            lock (Crit)
            {
                var settings = DJson.Read("../../../Api/settings.json");

                Client = new HttpClient()
                {
                    BaseAddress = new Uri(settings.Url)
                };

                Baker = settings.Baker;
                Delegator = settings.Delegator;
                Originator = settings.Originator;
                Cycle = settings.Cycle;
            }
        }

        public void Dispose()
        {
            Client.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
