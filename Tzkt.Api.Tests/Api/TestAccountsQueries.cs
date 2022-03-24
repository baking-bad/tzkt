using System.Net.Http;
using System.Threading.Tasks;
using Dynamic.Json;
using Dynamic.Json.Extensions;
using Xunit;

namespace Tzkt.Api.Tests.Api
{
    public class TestAccountsQueries : IClassFixture<SettingsFixture>
    {
        readonly SettingsFixture Settings;
        readonly HttpClient Client;

        public TestAccountsQueries(SettingsFixture settings)
        {
            Settings = settings;
            Client = settings.Client;
        }

        [Fact]
        public async Task TestAccountsCount()
        {
            var res = await Client.GetJsonAsync("/v1/accounts/count");

            Assert.True(res is DJsonValue);
        }

        [Fact]
        public async Task TestAccounts()
        {
            var res = await Client.GetJsonAsync("/v1/accounts");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestUsers()
        {
            var res = await Client.GetJsonAsync("/v1/accounts?type=user");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestDelegates()
        {
            var res = await Client.GetJsonAsync("/v1/accounts?type=delegate");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestContracts()
        {
            var res = await Client.GetJsonAsync("/v1/accounts?type=contract");

            Assert.True(res is DJsonArray);
        }

        [Fact]
        public async Task TestGhosts()
        {
            var res = await Client.GetJsonAsync("/v1/accounts?type=ghost");

            Assert.True(res is DJsonArray);
        }
        
        [Fact]
        public async Task TestAccountByAddress()
        {
            var res = await Client.GetJsonAsync($"/v1/accounts/{Settings.Baker}");

            Assert.True(res is DJsonObject);
        }
        
        [Fact]
        public async Task TestAccountContracts()
        {
            var res = await Client.GetJsonAsync($"/v1/accounts/{Settings.Originator}/contracts");

            Assert.True(res is DJsonArray);
        }
        
        [Fact]
        public async Task TestAccountDelegators()
        {
            var res = await Client.GetJsonAsync($"/v1/accounts/{Settings.Baker}/delegators");

            Assert.True(res is DJsonArray);
        }
        
        [Fact]
        public async Task TestAccountOperations()
        {
            var res = await Client.GetJsonAsync($"/v1/accounts/{Settings.Baker}/operations");

            Assert.True(res is DJsonArray);
        }
        
        [Fact]
        public async Task TestCounter()
        {
            var res = await Client.GetJsonAsync($"/v1/accounts/{Settings.Baker}/counter");

            Assert.True(res is DJsonValue);
        }
        
        [Fact]
        public async Task TestBalance()
        {
            var res = await Client.GetJsonAsync($"/v1/accounts/{Settings.Baker}/balance");

            Assert.True(res is DJsonValue);
        }
        
        [Fact]
        public async Task TestBalanceAtLevel()
        {
            var res = await Client.GetJsonAsync($"/v1/accounts/{Settings.Baker}/balance_history/10");

            Assert.True(res is DJsonValue);
        }
        
        [Fact]
        public async Task TestBalanceHistory()
        {
            var res = await Client.GetJsonAsync($"/v1/accounts/{Settings.Baker}/balance_history");

            Assert.True(res is DJsonArray);
        }
    }
}
