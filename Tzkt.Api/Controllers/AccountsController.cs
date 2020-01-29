using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

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
        private readonly ReportRepository Reports;

        public AccountsController(AccountRepository accounts, ReportRepository reports)
        {
            Accounts = accounts;
            Reports = reports;
        }

        /// <summary>
        /// Get accounts
        /// </summary>
        /// <remarks>
        /// Returns a list of accounts.
        /// </remarks>
        /// <param name="p">Page offset (pagination)</param>
        /// <param name="n">Number of items to return</param>
        /// <returns></returns>
        [HttpGet]
        public Task<IEnumerable<Account>> Get([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Accounts.Get(n, p * n);
        }

        /// <summary>
        /// Get account by address
        /// </summary>
        /// <remarks>
        /// Returns an account with the specified address.
        /// </remarks>
        /// <param name="address">Account address (starting with tz or KT)</param>
        /// <returns></returns>
        [HttpGet("{address}")]
        public Task<Account> GetByAddress([Address] string address)
        {
            return Accounts.Get(address);
        }

        /// <summary>
        /// Get account contracts
        /// </summary>
        /// <remarks>
        /// Returns a list of contracts created by (or related to) the specified account.
        /// </remarks>
        /// <param name="address">Account address (starting with tz or KT)</param>
        /// <param name="p">Page offset (pagination)</param>
        /// <param name="n">Number of items to return</param>
        /// <returns></returns>
        [HttpGet("{address}/contracts")]
        public Task<IEnumerable<RelatedContract>> GetContracts([Address] string address, [Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Accounts.GetRelatedContracts(address, n, p * n);
        }

        /// <summary>
        /// Get account delegators
        /// </summary>
        /// <remarks>
        /// Returns a list of accounts delegated to the specified account.
        /// </remarks>
        /// <param name="address">Account address (starting with tz or KT)</param>
        /// <param name="p">Page offset (pagination)</param>
        /// <param name="n">Number of items to return</param>
        /// <returns></returns>
        [HttpGet("{address}/delegators")]
        public Task<IEnumerable<Delegator>> GetDelegators([Address] string address, [Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Accounts.GetDelegators(address, n, p * n);
        }

        /// <summary>
        /// Get account operations
        /// </summary>
        /// <remarks>
        /// Returns a list of operations related to the specified account.
        /// </remarks>
        /// <param name="address">Account address (starting with tz or KT)</param>
        /// <param name="from">Start of the datetime range to filter by (ISO 8601, e.g. 2019-11-31)</param>
        /// <param name="to">End of the datetime range to filter by (ISO 8601, e.g. 2019-12-31)</param>
        /// <param name="type">Comma separated list of operation types to return (endorsement, ballot, proposal, activation, double_baking, double_endorsing, nonce_revelation, delegation, origination, transaction, reveal, migration, revelation_penalty, baking)</param>
        /// <param name="lastId">Id of the last operation received, which is used as an offset for pagination</param>
        /// <param name="limit">Number of items to return</param>
        /// <param name="sort">Sort mode (0 - ascending, 1 - descending)</param>
        /// <returns></returns>
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

        /// <summary>
        /// Get account metadata
        /// </summary>
        /// <remarks>
        /// Returns metadata of the specified account (alias, logo, website, contacts, etc).
        /// </remarks>
        /// <param name="address">Account address (starting with tz or KT)</param>
        /// <returns></returns>
        [HttpGet("{address}/metadata")]
        public Task<AccountMetadata> GetMetadata([Address] string address)
        {
            return Accounts.GetMetadata(address);
        }

        [OpenApiIgnore]
        [HttpGet("{address}/profile")]
        public Task<Account> GetProfile([Address] string address, string type, [Range(0, 1000)] int n = 20, SortMode sort = SortMode.Descending)
        {
            var types = type != null ? new HashSet<string>(type.Split(',')) : OpTypes.DefaultSet;

            return Accounts.GetProfile(address, types, sort, n);
        }

        /// <summary>
        /// Get account report
        /// </summary>
        /// <remarks>
        /// Exports account balance report in .csv format
        /// </remarks>
        /// <param name="address">Account address (starting with tz or KT)</param>
        /// <param name="from">Start of the datetime range to filter by (ISO 8601, e.g. 2019-11-31)</param>
        /// <param name="to">End of the datetime range to filter by (ISO 8601, e.g. 2019-12-31)</param>
        /// <returns></returns>
        [HttpGet("{address}/report")]
        public async Task<FileStreamResult> GetBalanceReport(
            [Address] string address,
            DateTime? from,
            DateTime? to)
        {
            var _from = from ?? DateTime.MinValue;
            var _to = to ?? DateTime.MaxValue;

            var stream = new MemoryStream();
            var csv = new StreamWriter(stream);

            await Reports.Write(csv, address, _from, _to, 257_000);

            stream.Seek(0, SeekOrigin.Begin);
            return new FileStreamResult(stream, "text/csv")
            {
                FileDownloadName = $"{address[..9]}..{address[^6..]}_{_from.ToShortDateString()}-{_to.ToShortDateString()}.csv"
            };
        }
    }
}
