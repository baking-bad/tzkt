using System.Collections.Generic;
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

        public async Task<ActionResult<HomeData>> Get(Symbols quote = Symbols.None)
        {
            await Home.UpdateStats();
            return Ok(HomeRepository.GetStatistic(quote));
        }

        [HttpGet("blocks")]
        public ActionResult<object> GetBlocks()
        {
            return Ok(new SelectionResponse
            {
                Cols = Home.BlockFields,
                Rows = (object[][]) HomeRepository.GetTab(TabTypes.Blocks)
            });
        }

        [HttpGet("accounts")]
        public ActionResult<object> GetAccounts()
        {
            return Ok(new SelectionResponse
            {
                Cols = Home.AccountFields,
                Rows = (object[][]) HomeRepository.GetTab(TabTypes.Accounts)
            });
        }

        [HttpGet("bakers")]
        public ActionResult<object> GetBakers()
        {
            return Ok(HomeRepository.GetTab(TabTypes.Bakers));
        }

        [HttpGet("assets")]
        public ActionResult<object> GetAssets()
        {
            return Ok(new SelectionResponse
            {
                Cols = Home.AssetFields,
                Rows = (object[][]) HomeRepository.GetTab(TabTypes.Assets)
            });
        }
    }
}