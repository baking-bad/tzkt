using System.Net.Http;
using System.Threading.Tasks;
using Dynamic.Json;
using Dynamic.Json.Extensions;
using Xunit;

namespace Tzkt.Api.Tests.Api
{
    public class TestRightsQueries : IClassFixture<SettingsFixture>
    {
        readonly HttpClient Client;

        public TestRightsQueries(SettingsFixture settings)
        {
            Client = settings.Client;
        }

        [Fact]
        public async Task TestRightsCount()
        {
            var res = await Client.GetJsonAsync("/v1/rights/count");

            Assert.True(res is DJsonValue);
        }

        [Fact]
        public async Task TestRights()
        {
            var res = await Client.GetJsonAsync("/v1/rights");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestBakingRights()
        {
            var res = await Client.GetJsonAsync("/v1/rights?type=baking");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestEndorsingRights()
        {
            var res = await Client.GetJsonAsync("/v1/rights?type=endorsing");

            Assert.True(res is DJsonArray);
        }
    }
}
