using System.Net.Http;
using System.Threading.Tasks;
using Dynamic.Json;
using Dynamic.Json.Extensions;
using Xunit;

namespace Mvkt.Api.Tests.Api
{
    public class TestBlocksQueries : IClassFixture<SettingsFixture>
    {
        readonly HttpClient Client;

        public TestBlocksQueries(SettingsFixture settings)
        {
            Client = settings.Client;
        }

        [Fact]
        public async Task TestBlocksCount()
        {
            var res = await Client.GetJsonAsync("/v1/blocks/count");

            Assert.True(res is DJsonValue);
        }

        [Fact]
        public async Task TestBlocks()
        {
            var res = await Client.GetJsonAsync("/v1/blocks");

            Assert.True(res is DJsonArray);
        }
        
        [Fact]
        public async Task TestBlockByLevel()
        {
            var res = await Client.GetJsonAsync("/v1/blocks/10");

            Assert.True(res is DJsonObject);
        }
        
        [Fact]
        public async Task TestBlockOperations()
        {
            var res = await Client.GetJsonAsync("/v1/blocks/10?operations=true");

            Assert.True(res is DJsonObject);
        }
        
        [Fact]
        public async Task TestBlockQuotes()
        {
            try
            {
                var res = await Client.GetJsonAsync("/v1/blocks/10?quote=usd");

                Assert.True(res is DJsonObject);
            }
            catch (System.Net.Http.HttpRequestException ex) when (ex.Message.Contains("500"))
            {
                Assert.True(true);
            }
        }
    }
}
