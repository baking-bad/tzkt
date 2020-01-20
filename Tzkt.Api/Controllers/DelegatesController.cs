using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using Tzkt.Api.Models;
using Tzkt.Api.Repositories;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public class DelegatesController : ControllerBase
    {
        private readonly AccountRepository Accounts;
        public DelegatesController(AccountRepository accounts)
        {
            Accounts = accounts;
        }

        /// <summary>
        /// Get delegates
        /// </summary>
        /// <remarks>
        /// Returns a list of accounts.
        /// </remarks>
        /// <param name="active">Delegate status to filter by (true - only active, false - only deactivated, undefined - all delegates)</param>
        /// <param name="p">Page offset (pagination)</param>
        /// <param name="n">Number of items to return</param>
        /// <returns></returns>
        [HttpGet]
        public Task<IEnumerable<Models.Delegate>> Get(bool? active, [Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Accounts.GetDelegates(active, n, p * n);
        }

        /// <summary>
        /// Get delegate by address
        /// </summary>
        /// <remarks>
        /// Returns a delegate with the specified address.
        /// </remarks>
        /// <param name="address">Delegate address (starting with tz)</param>
        /// <returns></returns>
        [HttpGet("{address}")]
        public Task<Models.Delegate> GetByAddress([Address] string address)
        {
            return Accounts.GetDelegate(address);
        }
    }
}
