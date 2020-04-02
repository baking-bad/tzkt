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
    [Route("v1/accounts")]
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
        /// <param name="type">Filters accounts by type (`user`, `delegate`, `contract`).</param>
        /// <param name="kind">Filter accounts by contract kind (`delegator_contract` or `smart_contract`)</param>
        /// <param name="sort">Sorts delegators by specified field. Supported fields: `balance`, `firstActivity`, `lastActivity`, `numTransactions`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you use this query parameter, response will be an array of values (if you select single field) or an array of array of values (if you select multiple fields).</param>
        /// <param name="p">Deprecated parameter. Will be removed in the next release.</param>
        /// <param name="n">Deprecated parameter. Will be removed in the next release.</param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(200, Type = typeof(IEnumerable<Account>))]
        public async Task<ActionResult> Get(
            AccountTypeParameter type,
            ContractKindParameter kind,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            string select = null,
            [Min(0)] int p = 0,
            [Range(0, 1000)] int n = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("balance", "firstActivity", "lastActivity", "numTransactions"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not supported.");
            #endregion

            #region optimize
            if (kind?.Eq != null && type == null)
                type = new AccountTypeParameter { Eq = 2 };
            #endregion

            //backward compatibility
            if (p != 0) offset = new OffsetParameter { Pg = p };
            if (n != 100) limit = n;

            if (string.IsNullOrEmpty(select))
                return Ok(await Accounts.Get(type, kind, sort, offset, limit));

            var fields = select.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length == 1)
                return Ok(await Accounts.Get(type, kind, sort, offset, limit, fields[0]));

            return Ok(await Accounts.Get(type, kind, sort, offset, limit, fields));
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
        /// <param name="type">Filters delegators by type (`user`, `delegate`, `contract`).</param>
        /// <param name="balance">Filters delegators by balance.</param>
        /// <param name="delegationLevel">Number of items to skip</param>
        /// <param name="sort">Sorts delegators by specified field. Supported fields: `delegationLevel`, `balance`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="p">Deprecated parameter. Will be removed in the next release.</param>
        /// <param name="n">Deprecated parameter. Will be removed in the next release.</param>
        /// <returns></returns>
        [HttpGet("{address}/delegators")]
        public async Task<ActionResult<IEnumerable<Delegator>>> GetDelegators(
            [Address] string address,
            AccountTypeParameter type,
            Int64Parameter balance,
            Int32Parameter delegationLevel,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            [Min(0)] int p = 0, 
            [Range(0, 1000)] int n = 100)
        {
            #region validate
            if (balance != null)
            {
                if (balance.Eqx != null)
                    return new BadRequest($"{nameof(balance)}.eqx", "This parameter doesn't support .eqx mode.");

                if (balance.Nex != null)
                    return new BadRequest($"{nameof(balance)}.nex", "This parameter doesn't support .nex mode.");
            }

            if (delegationLevel != null)
            {
                if (delegationLevel.Eqx != null)
                    return new BadRequest($"{nameof(delegationLevel)}.eqx", "This parameter doesn't support .eqx mode.");

                if (delegationLevel.Nex != null)
                    return new BadRequest($"{nameof(delegationLevel)}.nex", "This parameter doesn't support .nex mode.");
            }

            if (sort != null && !sort.Validate("balance", "delegationLevel"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not supported.");
            #endregion

            //backward compatibility
            if (p != 0) offset = new OffsetParameter { Pg = p };
            if (n != 100) limit = n;

            return Ok(await Accounts.GetDelegators(address, type, balance, delegationLevel, sort, offset, limit));
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
