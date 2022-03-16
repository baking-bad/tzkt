using System.Net.Http;
using System.Threading.Tasks;
using Dynamic.Json;
using Dynamic.Json.Extensions;
using Xunit;

namespace Tzkt.Api.Tests.Api
{
    public class TestRewardsQueries : IClassFixture<SettingsFixture>
    {
        readonly HttpClient Client;

        public TestRewardsQueries(SettingsFixture settings)
        {
            Client = settings.Client;
        }

        [Fact]
        public async Task TestBakerRewardsCount()
        {
            var res = await Client.GetJsonAsync("/v1/rewards/bakers/tz3RDC3Jdn4j15J7bBHZd29EUee9gVB1CxD9/count");

            Assert.True(res is DJsonValue);
        }

        [Fact]
        public async Task TestBakerRewards()
        {
            var res = await Client.GetJsonAsync("/v1/rewards/bakers/tz3RDC3Jdn4j15J7bBHZd29EUee9gVB1CxD9");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestBakerRewardsByCycle()
        {
            var res = await Client.GetJsonAsync("/v1/rewards/bakers/tz3RDC3Jdn4j15J7bBHZd29EUee9gVB1CxD9/10");

            Assert.True(res is DJsonObject);
        }

        [Fact]
        public async Task TestDelegatorRewardsCount()
        {
            var res = await Client.GetJsonAsync("/v1/rewards/delegators/tz3RDC3Jdn4j15J7bBHZd29EUee9gVB1CxD9/count");

            Assert.True(res is DJsonValue);
        }

        [Fact]
        public async Task TestDelegatorRewards()
        {
            var res = await Client.GetJsonAsync("/v1/rewards/delegators/KT1QuofAgnsWffHzLA7D78rxytJruGHDe7XG");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestDelegatorRewardsByCycle()
        {
            var res = await Client.GetJsonAsync($"/v1/rewards/delegators/KT1QuofAgnsWffHzLA7D78rxytJruGHDe7XG/10");

            Assert.True(res is DJsonObject);
        }

        [Fact]
        public async Task TestRewardSplit()
        {
            var res = await Client.GetJsonAsync("/v1/rewards/split/tz3RDC3Jdn4j15J7bBHZd29EUee9gVB1CxD9/10");

            Assert.True(res is DJsonObject);
        }

        [Fact]
        public async Task TestRewardSplitDelegator()
        {
            var res = await Client.GetJsonAsync($"/v1/rewards/split/tz3RDC3Jdn4j15J7bBHZd29EUee9gVB1CxD9/10/KT1QuofAgnsWffHzLA7D78rxytJruGHDe7XG");

            Assert.True(res is DJsonObject);
        }
    }
}
