using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services.Metadata;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly AccountRepository Accounts;
        public AccountsController(AccountRepository accounts)
        {
            Accounts = accounts;
        }

        [HttpGet]
        public Task<IEnumerable<Account>> Get([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Accounts.Get(n, p * n);
        }

        [HttpGet("{address}")]
        public Task<Account> Get([Address] string address)
        {
            return Accounts.Get(address);
        }

        [HttpGet("{address}/profile")]
        public Task<Account> GetProfile([Address] string address, string type, [Range(0, 1000)] int n = 20, SortMode sort = SortMode.Descending)
        {
            var types = type != null ? new HashSet<string>(type.Split(',')) : OpTypes.DefaultSet;

            return Accounts.GetProfile(address, types, sort, n);
        }

        [HttpGet("{address}/contracts")]
        public Task<IEnumerable<RelatedContract>> GetContracts([Address] string address, [Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Accounts.GetContracts(address, n, p * n);
        }

        [HttpGet("{address}/delegators")]
        public Task<IEnumerable<Delegator>> GetDelegators([Address] string address, [Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Accounts.GetDelegators(address, n, p * n);
        }

        [HttpGet("{address}/operations")]
        public Task<IEnumerable<Operation>> GetOperations(
            [Address] string address,
            DateTime? from,
            DateTime? to,
            string type,
            [Min(0)] int lastId = 0,
            [Range(0, 1000)] int limit = 100,
            SortMode sort = SortMode.Descending)
        {
            var types = type != null ? new HashSet<string>(type.Split(',')) : OpTypes.DefaultSet;

            return from != null || to != null
                ? Accounts.GetOperations(address, from ?? DateTime.MinValue, to ?? DateTime.MaxValue, types, sort, lastId, limit)
                : Accounts.GetOperations(address, types, sort, lastId, limit);
        }

        [HttpGet("{address}/metadata")]
        public Task<AccountMetadata> GetMetadata([Address] string address)
        {
            return Accounts.GetMetadata(address);
        }
    }
}
