using System.Net.Http;
using System.Threading.Tasks;
using Dynamic.Json;
using Dynamic.Json.Extensions;
using Xunit;

namespace Mvkt.Api.Tests.Api
{
    public class TestHomeQueries : IClassFixture<SettingsFixture>
    {
        readonly HttpClient Client;

        public TestHomeQueries(SettingsFixture settings)
        {
            Client = settings.Client;
        }

        [Fact]
        public async Task TestHomeStats()
        {
            var res = await Client.GetAsync("/v1/home");
            res.EnsureSuccessStatusCode();
        }

        [Theory]
        [InlineData("usd")]
        [InlineData("btc")]
        [InlineData("eur")]
        [InlineData("cny")]
        [InlineData("jpy")]
        [InlineData("krw")]
        [InlineData("eth")]
        [InlineData("gbp")]
        public async Task TestHomeStatsWithQuote(string quote)
        {
            var res = await Client.GetAsync($"/v1/home?quote={quote}");
            res.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task TestHomeBlocks()
        {
            var res = await Client.GetJsonAsync("/v1/home/blocks");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestHomeAccounts()
        {
            var res = await Client.GetJsonAsync("/v1/home/accounts");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestHomeBakers()
        {
            var res = await Client.GetJsonAsync("/v1/home/bakers");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestHomeAssets()
        {
            var res = await Client.GetJsonAsync("/v1/home/assets");

            Assert.True(res is DJsonArray);
        }
    }
}

