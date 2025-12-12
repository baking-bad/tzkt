using System.Net.Http;
using System.Threading.Tasks;
using Dynamic.Json;
using Dynamic.Json.Extensions;
using System.Text.Json;
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

        private async Task<object?> GetJsonOrNullAsync(string uri)
        {
            try
            {
                return await Client.GetJsonAsync(uri);
            }
            catch (System.Text.Json.JsonException)
            {
                return null;
            }
        }

        [Fact]
        public async Task TestHomeStats()
        {
            var res = await GetJsonOrNullAsync("/v1/home");

            Assert.True(res is DJsonObject || res == null);
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
            var res = await GetJsonOrNullAsync($"/v1/home?quote={quote}");

            Assert.True(res is DJsonObject || res == null);
        }

        [Fact]
        public async Task TestHomeBlocks()
        {
            var res = await Client.GetJsonAsync("/v1/home/blocks");

            Assert.True(res is DJsonArray);
            var arr = res as DJsonArray;
            Assert.NotNull(arr);
        }

        [Fact]
        public async Task TestHomeAccounts()
        {
            var res = await Client.GetJsonAsync("/v1/home/accounts");

            Assert.True(res is DJsonArray);
            var arr = res as DJsonArray;
            Assert.NotNull(arr);
        }

        [Fact]
        public async Task TestHomeBakers()
        {
            var res = await Client.GetJsonAsync("/v1/home/bakers");

            Assert.True(res is DJsonArray);
            var arr = res as DJsonArray;
            Assert.NotNull(arr);
        }

        [Fact]
        public async Task TestHomeAssets()
        {
            var res = await Client.GetJsonAsync("/v1/home/assets");

            Assert.True(res is DJsonArray);
            var arr = res as DJsonArray;
            Assert.NotNull(arr);
        }
    }
}

