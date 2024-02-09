using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using Mvkt.Api.Models;
using Mvkt.Api.Services;

namespace Mvkt.Api.Controllers
{
    [OpenApiIgnore]
    [ApiController]
    [Route("v1/home")]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public HomeStats Get(Symbols quote = Symbols.Usd)
        {
            return HomeService.GetCurrentStats(quote);
        }
        
        [HttpGet("blocks")]
        public object GetBlocks()
        {
            return new SelectionResponse
            {
                Cols = HomeService.BlockFields,
                Rows = HomeService.BlocksTab
            };
        }
        
        [HttpGet("accounts")]
        public object GetAccounts()
        {
            return new SelectionResponse
            {
                Cols = HomeService.AccountFields,
                Rows = HomeService.AccountsTab
            };
        }
        
        [HttpGet("bakers")]
        public object GetBakers()
        {
            return new SelectionResponse
            {
                Cols = HomeService.BakerFields,
                Rows = HomeService.BakersTab
            };
        }

        [HttpGet("assets")]
        public object GetAssets()
        {
            return new SelectionResponse
            {
                Cols = HomeService.AssetFields,
                Rows = HomeService.AssetsTab
            };
        }
    }
}
