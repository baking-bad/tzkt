using System.Net.Http;
using System.Threading.Tasks;
using Dynamic.Json;
using Dynamic.Json.Extensions;
using Xunit;

namespace Tzkt.Api.Tests.Api
{
    public class TestBlocksQueries : IClassFixture<SettingsFixture>
    {
        readonly HttpClient Client;

        public TestBlocksQueries(SettingsFixture settings)
        {
            Client = settings.Client;
        }

        [Fact]
        public async Task TestBlockHash()
        {
            var res = await Client.GetJsonAsync("/v1/blocks/count");

            Assert.True(res is DJsonValue);
        }
    }
}
