using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/accounts")]
    public class AccountsController : ControllerBase
    {
        readonly AccountRepository Accounts;
        readonly BalanceHistoryRepository History;
        readonly ReportRepository Reports;
        readonly StateCache State;
        readonly ResponseCacheService ResponseCache;

        public AccountsController(AccountRepository accounts, BalanceHistoryRepository history, ReportRepository reports, StateCache state, ResponseCacheService responseCache)
        {
            Accounts = accounts;
            History = history;
            Reports = reports;
            State = state;
            ResponseCache = responseCache;
        }

        /// <summary>
        /// Get accounts
        /// </summary>
        /// <remarks>
        /// Returns a list of accounts.
        /// </remarks>
        /// <param name="id">Filters by internal id.</param>
        /// <param name="type">Filters accounts by type (`user`, `delegate`, `contract`, `ghost`).</param>
        /// <param name="kind">Filters accounts by contract kind (`delegator_contract` or `smart_contract`)</param>
        /// <param name="delegate">Filters accounts by delegate. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="balance">Filters accounts by balance</param>
        /// <param name="staked">Filters accounts by participation in staking</param>
        /// <param name="lastActivity">Filters accounts by last activity level (where the account was updated)</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts delegators by specified field. Supported fields: `id` (default), `balance`, `rollupBonds`, `firstActivity`, `lastActivity`, `numTransactions`, `numContracts`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Account>>> Get(
            Int32Parameter id,
            AccountTypeParameter type,
            ContractKindParameter kind,
            AccountParameter @delegate,
            Int64Parameter balance,
            BoolParameter staked,
            Int32Parameter lastActivity,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100)
        {
            #region validate
            if (@delegate != null)
            {
                if (@delegate.Eqx != null)
                    return new BadRequest($"{nameof(@delegate)}.eqx", "This parameter doesn't support .eqx mode.");

                if (@delegate.Nex != null)
                    return new BadRequest($"{nameof(@delegate)}.nex", "This parameter doesn't support .nex mode.");

                if (@delegate.Eq == -1 || @delegate.In?.Count == 0 && !@delegate.InHasNull)
                    return Ok(Enumerable.Empty<Account>());
            }

            if (sort != null && !sort.Validate("id", "balance", "rollupBonds", "firstActivity", "lastActivity", "numTransactions", "numContracts"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            #region optimize
            if (kind?.Eq != null && type == null)
                type = new AccountTypeParameter { Eq = 2 };
            #endregion
            
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("id", id),  ("type", type), ("kind", kind), ("delegate", @delegate), ("balance", balance), ("staked", staked),
                ("lastActivity", lastActivity), ("select", select), ("sort", sort), ("offset", offset), ("limit", limit));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await Accounts.Get(id, type, kind, @delegate, balance, staked, lastActivity, sort, offset, limit);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await Accounts.Get(id, type, kind, @delegate, balance, staked, lastActivity, sort, offset, limit, select.Values[0]);
                else
                    res = await Accounts.Get(id, type, kind, @delegate, balance, staked, lastActivity, sort, offset, limit, select.Values);
            }
            else
            {
                if (select.Fields.Length == 1)
                    res = await Accounts.Get(id, type, kind, @delegate, balance, staked, lastActivity, sort, offset, limit, select.Fields[0]);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Accounts.Get(id, type, kind, @delegate, balance, staked, lastActivity, sort, offset, limit, select.Fields)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get accounts count
        /// </summary>
        /// <remarks>
        /// Returns a number of accounts.
        /// </remarks>
        /// <param name="type">Filters accounts by type (`user`, `delegate`, `contract`, `ghost`).</param>
        /// <param name="kind">Filters accounts by contract kind (`delegator_contract` or `smart_contract`)</param>
        /// <param name="balance">Filters accounts by balance</param>
        /// <param name="staked">Filters accounts by participation in staking</param>
        /// <param name="firstActivity">Filters accounts by first activity level (where the account was created)</param>
        /// <returns></returns>
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetCount(
            AccountTypeParameter type,
            ContractKindParameter kind,
            Int64Parameter balance,
            BoolParameter staked,
            Int32Parameter firstActivity)
        {
            #region optimize
            if (type == null && kind == null && balance == null && staked == null && firstActivity == null)
                return Ok(State.Current.AccountsCount);

            if (kind?.Eq != null && type == null)
                type = new AccountTypeParameter { Eq = 2 };
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("type", type), ("kind", kind), ("balance", balance), ("staked", staked), ("firstActivity", firstActivity));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Accounts.GetCount(type, kind, balance, staked, firstActivity);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get account by address
        /// </summary>
        /// <remarks>
        /// Returns an account with the specified address.
        /// </remarks>
        /// <param name="address">Account address (starting with tz or KT)</param>
        /// <param name="metadata">Include or not account metadata</param>
        /// <returns></returns>
        [HttpGet("{address}")]
        public async Task<ActionResult<Account>> GetByAddress(
            [Required][Address] string address,
            bool metadata = false)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("metadata", metadata));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Accounts.Get(address, metadata);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get account contracts
        /// </summary>
        /// <remarks>
        /// Returns a list of contracts created by (or related to) the specified account.
        /// </remarks>
        /// <param name="address">Account address (starting with tz or KT)</param>
        /// <param name="sort">Sorts contracts by specified field. Supported fields: `id` (default, desc), `balance`, `creationLevel`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet("{address}/contracts")]
        public async Task<ActionResult<IEnumerable<RelatedContract>>> GetContracts(
            [Required][Address] string address,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "balance", "creationLevel"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion
            
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("sort", sort), ("offset", offset), ("limit", limit));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Accounts.GetRelatedContracts(address, sort, offset, limit);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get account delegators
        /// </summary>
        /// <remarks>
        /// Returns a list of accounts delegated to the specified account.
        /// </remarks>
        /// <param name="address">Account address (starting with tz)</param>
        /// <param name="type">Filters delegators by type (`user`, `delegate`, `contract`, `ghost`).</param>
        /// <param name="balance">Filters delegators by balance.</param>
        /// <param name="delegationLevel">Number of items to skip</param>
        /// <param name="sort">Sorts delegators by specified field. Supported fields: `delegationLevel` (default, desc), `balance`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet("{address}/delegators")]
        public async Task<ActionResult<IEnumerable<Delegator>>> GetDelegators(
            [Required][TzAddress] string address,
            AccountTypeParameter type,
            Int64Parameter balance,
            Int32Parameter delegationLevel,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("balance", "delegationLevel"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion
            
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("type", type), ("balance", balance), ("delegationLevel", delegationLevel),
                ("sort", sort), ("offset", offset), ("limit", limit));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Accounts.GetDelegators(address, type, balance, delegationLevel, sort, offset, limit);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get account operations
        /// </summary>
        /// <remarks>
        /// Returns a list of operations related to the specified account.
        /// Note: for better flexibility this endpoint accumulates query parameters (filters) of each `/operations/{type}` endpoint,
        /// so a particular filter may affect several operation types containing this filter.
        /// For example, if you specify an `initiator` it will affect all transactions, delegations and originations,
        /// because all these types have an `initiator` field.  
        /// **NOTE: if you know in advance what operation type you want to get (e.g. transactions), prefer using `/v1/operations/{type}`
        /// (e.g. [/v1/operations/transactions](#operation/Operations_GetTransactions)) instead, because it's much more efficient and way more flexible.**
        /// </remarks>
        /// <param name="address">Account address (starting with tz or KT)</param>
        /// <param name="type">Comma separated list of operation types to return (`endorsement`, `preendorsement`, `ballot`, `proposal`, `activation`, `double_baking`,
        /// `double_endorsing`, `double_preendorsing`, `nonce_revelation`, `vdf_revelation`, `delegation`, `origination`, `transaction`, `reveal`, `register_constant`,
        /// `set_deposits_limit`, `increase_paid_storage`, 'tx_rollup_origination', 'tx_rollup_submit_batch', 'tx_rollup_commit', 'tx_rollup_return_bond',
        /// 'tx_rollup_finalize_commitment', 'tx_rollup_remove_commitment', 'tx_rollup_rejection', 'tx_rollup_dispatch_tickets', 'transfer_ticket', `migration`,
        /// `revelation_penalty`, `baking`, `endorsing_reward`). If not specified then the default set will be returned.</param>
        /// <param name="initiator">Filters transactions, delegations and originations by initiator. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="sender">Filters transactions, delegations, originations, reveals and seed nonce revelations by sender. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="target">Filters transactions by target. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="prevDelegate">Filters delegations by prev delegate. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="newDelegate">Filters delegations by new delegate. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="contractManager">Filters origination operations by manager. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="contractDelegate">Filters origination operations by delegate. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="originatedContract">Filters origination operations by originated contract. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="accuser">Filters double baking and double endorsing by accuser. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="offender">Filters double baking and double endorsing by offender. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="baker">Filters seed nonce revelation operations by baker. Allowed fields for `.eqx` mode: none.</param>
        /// <param name="level">Filters operations by level.</param>
        /// <param name="timestamp">Filters operations by timestamp.</param>
        /// <param name="entrypoint">Filters transactions by entrypoint called on the target contract.</param>
        /// <param name="parameter">Filters transactions by parameter value. Note, this query parameter supports the following format: `?parameter{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by, for example: `?parameter.token_id=...` or `?parameter.sigs.0.ne=...`.</param>
        /// <param name="hasInternals">Filters transactions by presence of internal operations.</param>
        /// <param name="status">Filters transactions, delegations, originations and reveals by operation status (`applied`, `failed`, `backtracked`, `skipped`).</param>
        /// <param name="sort">Sort mode (0 - ascending, 1 - descending), operations of different types can only be sorted by ID.</param>
        /// <param name="lastId">Id of the last operation received, which is used as an offset for pagination</param>
        /// <param name="limit">Number of items to return</param>
        /// <param name="micheline">Format of the parameters, storage and diffs: `0` - JSON, `1` - JSON string, `2` - raw micheline, `3` - raw micheline string</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("{address}/operations")]
        public async Task<ActionResult<IEnumerable<Operation>>> GetOperations(
            [Required][Address] string address,
            string type,
            AccountParameter initiator,
            AccountParameter sender,
            AccountParameter target,
            AccountParameter prevDelegate,
            AccountParameter newDelegate,
            AccountParameter contractManager,
            AccountParameter contractDelegate,
            AccountParameter originatedContract,
            AccountParameter accuser,
            AccountParameter offender,
            AccountParameter baker,
            Int32Parameter level,
            DateTimeParameter timestamp,
            StringParameter entrypoint,
            JsonParameter parameter,
            BoolParameter hasInternals,
            OperationStatusParameter status,
            SortMode sort = SortMode.Descending,
            long? lastId = null,
            [Range(0, 1000)] int limit = 100,
            MichelineFormat micheline = MichelineFormat.Json,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (initiator != null)
            {
                if (initiator.Eqx != null)
                    return new BadRequest($"{nameof(initiator)}.eqx", "This parameter doesn't support .eqx mode.");

                if (initiator.Nex != null)
                    return new BadRequest($"{nameof(initiator)}.eqx", "This parameter doesn't support .eqx mode.");
            }

            if (sender != null)
            {
                if (sender.Eqx != null)
                    return new BadRequest($"{nameof(sender)}.eqx", "This parameter doesn't support .eqx mode.");

                if (sender.Nex != null)
                    return new BadRequest($"{nameof(sender)}.eqx", "This parameter doesn't support .eqx mode.");
            }

            if (target != null)
            {
                if (target.Eqx != null)
                    return new BadRequest($"{nameof(target)}.eqx", "This parameter doesn't support .eqx mode.");

                if (target.Nex != null)
                    return new BadRequest($"{nameof(target)}.eqx", "This parameter doesn't support .eqx mode.");
            }

            if (prevDelegate != null)
            {
                if (prevDelegate.Eqx != null)
                    return new BadRequest($"{nameof(prevDelegate)}.eqx", "This parameter doesn't support .eqx mode.");

                if (prevDelegate.Nex != null)
                    return new BadRequest($"{nameof(prevDelegate)}.nex", "This parameter doesn't support .nex mode.");
            }

            if (newDelegate != null)
            {
                if (newDelegate.Eqx != null)
                    return new BadRequest($"{nameof(newDelegate)}.eqx", "This parameter doesn't support .eqx mode.");

                if (newDelegate.Nex != null)
                    return new BadRequest($"{nameof(newDelegate)}.nex", "This parameter doesn't support .nex mode.");
            }

            if (contractManager != null)
            {
                if (contractManager.Eqx != null)
                    return new BadRequest($"{nameof(contractManager)}.eqx", "This parameter doesn't support .eqx mode.");

                if (contractManager.Nex != null)
                    return new BadRequest($"{nameof(contractManager)}.nex", "This parameter doesn't support .nex mode.");
            }

            if (contractDelegate != null)
            {
                if (contractDelegate.Eqx != null)
                    return new BadRequest($"{nameof(contractDelegate)}.eqx", "This parameter doesn't support .eqx mode.");

                if (contractDelegate.Nex != null)
                    return new BadRequest($"{nameof(contractDelegate)}.nex", "This parameter doesn't support .nex mode.");
            }

            if (originatedContract != null)
            {
                if (originatedContract.Eqx != null)
                    return new BadRequest($"{nameof(originatedContract)}.eqx", "This parameter doesn't support .eqx mode.");

                if (originatedContract.Nex != null)
                    return new BadRequest($"{nameof(originatedContract)}.nex", "This parameter doesn't support .nex mode.");
            }

            if (accuser != null)
            {
                if (accuser.Eqx != null)
                    return new BadRequest($"{nameof(accuser)}.eqx", "This parameter doesn't support .eqx mode.");

                if (accuser.Nex != null)
                    return new BadRequest($"{nameof(accuser)}.nex", "This parameter doesn't support .nex mode.");
            }

            if (offender != null)
            {
                if (offender.Eqx != null)
                    return new BadRequest($"{nameof(offender)}.eqx", "This parameter doesn't support .eqx mode.");

                if (offender.Nex != null)
                    return new BadRequest($"{nameof(offender)}.nex", "This parameter doesn't support .nex mode.");
            }

            if (baker != null)
            {
                if (baker.Eqx != null)
                    return new BadRequest($"{nameof(baker)}.eqx", "This parameter doesn't support .eqx mode.");

                if (baker.Nex != null)
                    return new BadRequest($"{nameof(baker)}.nex", "This parameter doesn't support .nex mode.");
            }
            #endregion

            var types = type != null ? new HashSet<string>(type.Split(',')) : OpTypes.DefaultSet;

            var _sort = sort == SortMode.Ascending
                ? new SortParameter { Asc = "Id" }
                : new SortParameter { Desc = "Id" };

            var _offset = lastId != null
                ? new OffsetParameter { Cr = lastId }
                : null;
            
            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("type", string.Join(",", types.OrderBy(x => x))),
                ("initiator", initiator), ("sender", sender), ("target", target), ("prevDelegate", prevDelegate),
                ("newDelegate", newDelegate), ("contractManager", contractManager), ("contractDelegate", contractDelegate),
                ("originatedContract", originatedContract), ("accuser", accuser), ("offender", offender), ("baker", baker),
                ("level", level), ("timestamp", timestamp), ("entrypoint", entrypoint), ("parameter", parameter), ("hasInternals", hasInternals),
                ("status", status), ("sort", sort), ("lastId", lastId), ("limit", limit), ("micheline", micheline), ("quote", quote));  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Accounts.GetOperations(address, types, initiator, sender, target, prevDelegate, newDelegate, contractManager, contractDelegate, originatedContract, accuser, offender, baker, level, timestamp, entrypoint, parameter, hasInternals, status, _sort, _offset, limit, micheline, quote);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
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
        public async Task<ActionResult<ProfileMetadata>> GetMetadata([Required][Address] string address)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value);  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await Accounts.GetMetadata(address);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get counter
        /// </summary>
        /// <remarks>
        /// Returns account counter
        /// </remarks>
        /// <param name="address">Account address (starting with tz or KT)</param>
        /// <returns></returns>
        [HttpGet("{address}/counter")]
        public async Task<ActionResult<int>> GetCounter([Required][Address] string address)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value);  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);
            
            var rawAccount = await Accounts.GetRawAsync(address);
            var res = rawAccount == null || rawAccount is RawUser && rawAccount.Balance == 0
                ? State.Current.ManagerCounter
                : rawAccount.Counter;
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get balance
        /// </summary>
        /// <remarks>
        /// Returns account balance
        /// </remarks>
        /// <param name="address">Account address (starting with tz or KT)</param>
        /// <returns></returns>
        [HttpGet("{address}/balance")]
        public async Task<ActionResult<long>> GetBalance([Required][Address] string address)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value);  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = (await Accounts.GetRawAsync(address))?.Balance ?? 0;
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get balance at level
        /// </summary>
        /// <remarks>
        /// Returns account balance at the specified block
        /// </remarks>
        /// <param name="address">Account address (starting with tz or KT)</param>
        /// <param name="level">Block height at which you want to know account balance</param>
        /// <returns></returns>
        [HttpGet("{address}/balance_history/{level:int}")]
        public async Task<ActionResult<long>> GetBalanceAtLevel(
            [Required][Address] string address,
            [Min(0)] int level)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value);  

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await History.Get(address, level);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get balance at date
        /// </summary>
        /// <remarks>
        /// Returns account balance at the specified datetime
        /// </remarks>
        /// <param name="address">Account address (starting with tz or KT)</param>
        /// <param name="datetime">Datetime at which you want to know account balance (e.g. `2020-01-01`, or `2019-12-30T23:42:59Z`)</param>
        /// <returns></returns>
        [HttpGet("{address}/balance_history/{datetime:DateTime}")]
        public async Task<ActionResult<long>> GetBalanceAtDate(
            [Required][Address] string address,
            DateTimeOffset datetime)
        {
            var query = ResponseCacheService.BuildKey(Request.Path.Value);

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            var res = await History.Get(address, datetime.DateTime);
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
        }

        /// <summary>
        /// Get balance history
        /// </summary>
        /// <remarks>
        /// Returns time series with historical balances (only changes, without duplicates).
        /// </remarks>
        /// <param name="address">Account address (starting with tz or KT)</param>
        /// <param name="step">Step of the time series, for example if `step = 1000` you will get balances at blocks `1000, 2000, 3000, ...`.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts historical balances by specified field. Supported fields: `level`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="quote">Comma-separated list of ticker symbols to inject historical prices into response</param>
        /// <returns></returns>
        [HttpGet("{address}/balance_history")]
        public async Task<ActionResult<IEnumerable<HistoricalBalance>>> GetBalanceHistory(
            [Required][Address] string address,
            [Min(1)] int? step,
            SelectParameter select,
            SortParameter sort,
            [Min(0)] int offset = 0,
            [Range(0, 10000)] int limit = 100,
            Symbols quote = Symbols.None)
        {
            #region validate
            if (sort != null && !sort.Validate("level"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var query = ResponseCacheService.BuildKey(Request.Path.Value,
                ("step", step), ("select", select), ("sort", sort), ("offset", offset), ("limit", limit), ("quote", quote));

            if (ResponseCache.TryGet(query, out var cached))
                return this.Bytes(cached);

            object res;
            if (select == null)
            {
                res = await History.Get(address, step ?? 1, sort, offset, limit, quote);
            }
            else if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    res = await History.Get(address, step ?? 1, sort, offset, limit, select.Values[0], quote);
                else
                    res = await History.Get(address, step ?? 1, sort, offset, limit, select.Values, quote);
            }
            else
            {
                if (select.Fields.Length == 1)
                    res = await History.Get(address, step ?? 1, sort, offset, limit, select.Fields[0], quote);
                else
                {
                    res = new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await History.Get(address, step ?? 1, sort, offset, limit, select.Fields, quote)
                    };
                }
            }
            cached = ResponseCache.Set(query, res);
            return this.Bytes(cached);
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
        /// <param name="delimiter">Column delimiter (`comma`, `semicolon`)</param>
        /// <param name="separator">Decimal separator (`comma`, `point`)</param>
        /// <param name="currency">Currency to convert amounts to (`btc`, `eur`, `usd`, `cny`, `jpy`, `krw`, `eth`, `gbp`)</param>
        /// <param name="historical">`true` if you want to use historical prices, `false` to use current price</param>
        /// <returns></returns>
        [HttpGet("{address}/report")]
        public async Task<ActionResult> GetBalanceReport(
            [Required][Address] string address,
            DateTimeOffset? from,
            DateTimeOffset? to,
            string currency,
            bool historical = false,
            string delimiter = "comma",
            string separator = "point")
        {
            #region verify delimiter
            if (delimiter == "comma")
            {
                delimiter = ",";
            }
            else if (delimiter == "semicolon")
            {
                delimiter = ";";
            }
            else
            {
                return new BadRequest(nameof(delimiter), "Unsupported value");
            }
            #endregion

            #region verify separator
            if (separator == "comma")
            {
                separator = ",";
            }
            else if (separator == "point")
            {
                separator = ".";
            }
            else
            {
                return new BadRequest(nameof(separator), "Unsupported value");
            }
            #endregion

            #region verify symbol
            var symbol = currency switch
            {
                "btc" => 0,
                "eur" => 1,
                "usd" => 2,
                "cny" => 3,
                "jpy" => 4,
                "krw" => 5,
                "eth" => 6,
                "gbp" => 7,
                _ => -1
            };
            #endregion

            var _from = from?.DateTime ?? DateTime.MinValue;
            var _to = to?.DateTime ?? DateTime.MaxValue;

            var stream = new MemoryStream();
            var csv = new StreamWriter(stream);

            if (symbol == -1)
            {
                await Reports.Write(csv, address, _from, _to, 257_000, delimiter, separator);
            }
            else if (historical)
            {
                await Reports.WriteHistorical(csv, address, _from, _to, 257_000, delimiter, separator, symbol);
            }
            else
            {
                await Reports.Write(csv, address, _from, _to, 257_000, delimiter, separator, symbol);
            }

            stream.Seek(0, SeekOrigin.Begin);
            return new FileStreamResult(stream, "text/csv")
            {
                FileDownloadName = $"{address[..9]}..{address[^6..]}_{_from.ToShortDateString()}-{_to.ToShortDateString()}.csv"
            };
        }
    }
}
