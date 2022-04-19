using System.Net.Http;
using System.Threading.Tasks;
using Dynamic.Json;
using Dynamic.Json.Extensions;
using Xunit;

namespace Tzkt.Api.Tests.Api
{
    public class TestOperationsQueries : IClassFixture<SettingsFixture>
    {
        readonly HttpClient Client;

        public TestOperationsQueries(SettingsFixture settings)
        {
            Client = settings.Client;
        }

        [Fact]
        public async Task TestEndorsements()
        {
            var res = await Client.GetJsonAsync("/v1/operations/endorsements");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestEndorsementsCount()
        {
            var res = await Client.GetJsonAsync("/v1/operations/endorsements/count");

            Assert.True(res is DJsonValue);
        }

        [Fact]
        public async Task TestBallots()
        {
            var res = await Client.GetJsonAsync("/v1/operations/ballots");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestBallotsCount()
        {
            var res = await Client.GetJsonAsync("/v1/operations/ballots/count");

            Assert.True(res is DJsonValue);
        }

        [Fact]
        public async Task TestProposals()
        {
            var res = await Client.GetJsonAsync("/v1/operations/proposals");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestProposalsCount()
        {
            var res = await Client.GetJsonAsync("/v1/operations/proposals/count");

            Assert.True(res is DJsonValue);
        }

        [Fact]
        public async Task TestActivations()
        {
            var res = await Client.GetJsonAsync("/v1/operations/activations");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestActivationsCount()
        {
            var res = await Client.GetJsonAsync("/v1/operations/activations/count");

            Assert.True(res is DJsonValue);
        }

        [Fact]
        public async Task TestDoubleBaking()
        {
            var res = await Client.GetJsonAsync("/v1/operations/double_baking");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestDoubleBakingCount()
        {
            var res = await Client.GetJsonAsync("/v1/operations/double_baking/count");

            Assert.True(res is DJsonValue);
        }

        [Fact]
        public async Task TestDoubleEndorsing()
        {
            var res = await Client.GetJsonAsync("/v1/operations/double_endorsing");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestDoubleEndorsingCount()
        {
            var res = await Client.GetJsonAsync("/v1/operations/double_endorsing/count");

            Assert.True(res is DJsonValue);
        }

        [Fact]
        public async Task TestNonceRevelations()
        {
            var res = await Client.GetJsonAsync("/v1/operations/nonce_revelations");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestNonceRevelationsCount()
        {
            var res = await Client.GetJsonAsync("/v1/operations/nonce_revelations/count");

            Assert.True(res is DJsonValue);
        }

        [Fact]
        public async Task TestDelegations()
        {
            var res = await Client.GetJsonAsync("/v1/operations/delegations");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestDelegationsCount()
        {
            var res = await Client.GetJsonAsync("/v1/operations/delegations/count");

            Assert.True(res is DJsonValue);
        }

        [Fact]
        public async Task TestOriginations()
        {
            var res = await Client.GetJsonAsync("/v1/operations/originations");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestOriginationsCount()
        {
            var res = await Client.GetJsonAsync("/v1/operations/originations/count");

            Assert.True(res is DJsonValue);
        }

        [Fact]
        public async Task TestTransactions()
        {
            var res = await Client.GetJsonAsync("/v1/operations/transactions");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestTransactionsCount()
        {
            var res = await Client.GetJsonAsync("/v1/operations/transactions/count");

            Assert.True(res is DJsonValue);
        }

        [Fact]
        public async Task TestReveals()
        {
            var res = await Client.GetJsonAsync("/v1/operations/reveals");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestRevealsCount()
        {
            var res = await Client.GetJsonAsync("/v1/operations/reveals/count");

            Assert.True(res is DJsonValue);
        }

        [Fact]
        public async Task TestRegisterConstants()
        {
            var res = await Client.GetJsonAsync("/v1/operations/register_constants");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestSetDepositsLimitsCount()
        {
            var res = await Client.GetJsonAsync("/v1/operations/set_deposits_limits/count");

            Assert.True(res is DJsonValue);
        }

        [Fact]
        public async Task TestSetDepositsLimits()
        {
            var res = await Client.GetJsonAsync("/v1/operations/set_deposits_limits");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestRegisterConstantsCount()
        {
            var res = await Client.GetJsonAsync("/v1/operations/register_constants/count");

            Assert.True(res is DJsonValue);
        }

        [Fact]
        public async Task TestMigrations()
        {
            var res = await Client.GetJsonAsync("/v1/operations/migrations");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestMigrationsCount()
        {
            var res = await Client.GetJsonAsync("/v1/operations/migrations/count");

            Assert.True(res is DJsonValue);
        }

        [Fact]
        public async Task TestRevelationPenalties()
        {
            var res = await Client.GetJsonAsync("/v1/operations/revelation_penalties");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestRevelationPenaltiesCount()
        {
            var res = await Client.GetJsonAsync("/v1/operations/revelation_penalties/count");

            Assert.True(res is DJsonValue);
        }

        [Fact]
        public async Task TestBaking()
        {
            var res = await Client.GetJsonAsync("/v1/operations/baking");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestBakingCount()
        {
            var res = await Client.GetJsonAsync("/v1/operations/baking/count");

            Assert.True(res is DJsonValue);
        }
    }
}
