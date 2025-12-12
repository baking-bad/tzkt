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
            var res = await Client.GetJsonAsync("/v1/home");

            // Can be null if LastUpdate <= 0, or DJsonObject if data is available
            Assert.True(res is DJsonObject || res == null);
        }

        [Fact]
        public async Task TestHomeStatsWithUsdQuote()
        {
            var res = await Client.GetJsonAsync("/v1/home?quote=usd");

            Assert.True(res is DJsonObject || res == null);
        }

        [Fact]
        public async Task TestHomeStatsWithBtcQuote()
        {
            var res = await Client.GetJsonAsync("/v1/home?quote=btc");

            Assert.True(res is DJsonObject || res == null);
        }

        [Fact]
        public async Task TestHomeStatsWithEurQuote()
        {
            var res = await Client.GetJsonAsync("/v1/home?quote=eur");

            Assert.True(res is DJsonObject || res == null);
        }

        [Fact]
        public async Task TestHomeStatsWithCnyQuote()
        {
            var res = await Client.GetJsonAsync("/v1/home?quote=cny");

            Assert.True(res is DJsonObject || res == null);
        }

        [Fact]
        public async Task TestHomeStatsWithJpyQuote()
        {
            var res = await Client.GetJsonAsync("/v1/home?quote=jpy");

            Assert.True(res is DJsonObject || res == null);
        }

        [Fact]
        public async Task TestHomeStatsWithKrwQuote()
        {
            var res = await Client.GetJsonAsync("/v1/home?quote=krw");

            Assert.True(res is DJsonObject || res == null);
        }

        [Fact]
        public async Task TestHomeStatsWithEthQuote()
        {
            var res = await Client.GetJsonAsync("/v1/home?quote=eth");

            Assert.True(res is DJsonObject || res == null);
        }

        [Fact]
        public async Task TestHomeStatsWithGbpQuote()
        {
            var res = await Client.GetJsonAsync("/v1/home?quote=gbp");

            Assert.True(res is DJsonObject || res == null);
        }

        [Fact]
        public async Task TestHomeBlocks()
        {
            var res = await Client.GetJsonAsync("/v1/home/blocks");

            Assert.True(res is DJsonObject);
            var obj = res as DJsonObject;
            Assert.NotNull(obj);
        }

        [Fact]
        public async Task TestHomeAccounts()
        {
            var res = await Client.GetJsonAsync("/v1/home/accounts");

            Assert.True(res is DJsonObject);
            var obj = res as DJsonObject;
            Assert.NotNull(obj);
        }

        [Fact]
        public async Task TestHomeBakers()
        {
            var res = await Client.GetJsonAsync("/v1/home/bakers");

            Assert.True(res is DJsonObject);
            var obj = res as DJsonObject;
            Assert.NotNull(obj);
        }

        [Fact]
        public async Task TestHomeAssets()
        {
            var res = await Client.GetJsonAsync("/v1/home/assets");

            Assert.True(res is DJsonObject);
            var obj = res as DJsonObject;
            Assert.NotNull(obj);
        }
    }
}

