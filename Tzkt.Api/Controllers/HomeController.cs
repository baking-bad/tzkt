using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using Tzkt.Api.Models;
using Tzkt.Api.Models.Home;
using Tzkt.Api.Repositories;

namespace Tzkt.Api.Controllers
{
    [OpenApiIgnore]
    [ApiController]
    [Route("v1/home")]
    public class HomeController : ControllerBase
    {
        private readonly HomeRepository Home;

        public HomeController(HomeRepository home)
        {
            Home = home;
        }

        public async Task<HomeData> GetStats(Symbols quote = Symbols.None)
        {
            await Home.UpdateStats();
            return HomeRepository.Stats;
        }

        [HttpGet("blocks")]
        public object GetBlocks()
        {
            return new SelectionResponse
            {
                Cols = HomeRepository.BlockFields,
                Rows = HomeRepository.BlocksTab
            };
        }

        [HttpGet("accounts")]
        public object GetAccounts()
        {
            return new SelectionResponse
            {
                Cols = HomeRepository.AccountFields,
                Rows = HomeRepository.AccountsTab
            };
        }

        [HttpGet("bakers")]
        public object GetBakers()
        {
            return HomeRepository.BakersTab;
        }

        [HttpGet("assets")]
        public object GetAssets()
        {
            return new SelectionResponse
            {
                Cols = HomeRepository.AssetFields,
                Rows = HomeRepository.AssetsTab
            };
        }
    }
}