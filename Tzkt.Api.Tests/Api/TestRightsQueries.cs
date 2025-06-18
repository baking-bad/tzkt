using Dynamic.Json;
using Dynamic.Json.Extensions;
using Xunit;

namespace Tzkt.Api.Tests.Api
{
    public class TestRightsQueries(SettingsFixture settings) : IClassFixture<SettingsFixture>
    {
        readonly HttpClient Client = settings.Client;

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
        public async Task TestAttestationRights()
        {
            var res = await Client.GetJsonAsync("/v1/rights?type=attestation");

            Assert.True(res is DJsonArray);
        }
    }
}
