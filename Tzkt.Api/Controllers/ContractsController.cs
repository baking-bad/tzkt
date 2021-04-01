using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Netezos.Encoding;
using Tzkt.Api.Models;
using Tzkt.Api.Repositories;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/contracts")]
    public class ContractsController : ControllerBase
    {
        private readonly AccountRepository Accounts;
        public ContractsController(AccountRepository accounts)
        {
            Accounts = accounts;
        }

        /// <summary>
        /// Get contracts
        /// </summary>
        /// <remarks>
        /// Returns a list of contract accounts.
        /// </remarks>
        /// <param name="kind">Contract kind to filter by (`delegator_contract` or `smart_contract`)</param>
        /// <param name="creator">Filters contracts by creator. Allowed fields for `.eqx` mode: `manager`, `delegate`.</param>
        /// <param name="manager">Filters contracts by manager. Allowed fields for `.eqx` mode: `creator`, `delegate`.</param>
        /// <param name="delegate">Filters contracts by delegate. Allowed fields for `.eqx` mode: `manager`, `creator`.</param>
        /// <param name="lastActivity">Filters contracts by last activity level (where the contract was updated)</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts delegators by specified field. Supported fields: `id` (default), `balance`, `firstActivity`, `lastActivity`, `numTransactions`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="includeStorage">Specifies whether to include contract storage value in response.</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Contract>>> Get(
            ContractKindParameter kind,
            AccountParameter creator,
            AccountParameter manager,
            AccountParameter @delegate,
            Int32Parameter lastActivity,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            bool includeStorage = false)
        {
            #region validates
            if (creator != null)
            {
                if (creator.Eqx != null && creator.Eqx != "manager" && creator.Eqx != "delegate")
                    return new BadRequest($"{nameof(creator)}.eqx", "The 'creator' field can be compared with the 'manager' or 'delegate' field only.");

                if (creator.Nex != null && creator.Nex != "manager" && creator.Nex != "delegate")
                    return new BadRequest($"{nameof(creator)}.nex", "The 'creator' field can be compared with the 'manager' or 'delegate' field only.");

                if (creator.Eq == -1 || creator.In?.Count == 0)
                    return Ok(Enumerable.Empty<Contract>());
            }

            if (manager != null)
            {
                if (manager.Eqx != null && manager.Eqx != "creator" && manager.Eqx != "delegate")
                    return new BadRequest($"{nameof(manager)}.eqx", "The 'manager' field can be compared with the 'creator' or 'delegate' field only.");

                if (manager.Nex != null && manager.Nex != "creator" && manager.Nex != "delegate")
                    return new BadRequest($"{nameof(manager)}.nex", "The 'manager' field can be compared with the 'creator' or 'delegate' field only.");

                if (manager.Eq == -1 || manager.In?.Count == 0)
                    return Ok(Enumerable.Empty<Contract>());
            }

            if (@delegate != null)
            {
                if (@delegate.Eqx != null && @delegate.Eqx != "creator" && @delegate.Eqx != "manager")
                    return new BadRequest($"{nameof(@delegate)}.eqx", "The 'delegate' field can be compared with the 'creator' or 'manager' field only.");

                if (@delegate.Nex != null && @delegate.Nex != "creator" && @delegate.Nex != "manager")
                    return new BadRequest($"{nameof(@delegate)}.nex", "The 'delegate' field can be compared with the 'creator' or 'manager' field only.");

                if (@delegate.Eq == -1 || @delegate.In?.Count == 0)
                    return Ok(Enumerable.Empty<Contract>());
            }

            if (sort != null && !sort.Validate("id", "balance", "firstActivity", "lastActivity", "numTransactions"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Accounts.GetContracts(kind, creator, manager, @delegate, lastActivity, sort, offset, limit, includeStorage));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Accounts.GetContracts(kind, creator, manager, @delegate, lastActivity, sort, offset, limit, select.Values[0], includeStorage));
                else
                    return Ok(await Accounts.GetContracts(kind, creator, manager, @delegate, lastActivity, sort, offset, limit, select.Values, includeStorage));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Accounts.GetContracts(kind, creator, manager, @delegate, lastActivity, sort, offset, limit, select.Fields[0], includeStorage));
                else
                {
                    return Ok(new SelectionResponse
                    {
                         Cols = select.Fields,
                         Rows = await Accounts.GetContracts(kind, creator, manager, @delegate, lastActivity, sort, offset, limit, select.Fields, includeStorage)
                    });
                }
            }
        }

        /// <summary>
        /// Get contracts count
        /// </summary>
        /// <remarks>
        /// Returns a number of contract accounts.
        /// </remarks>
        /// <param name="kind">Contract kind to filter by (`delegator_contract` or `smart_contract`)</param>
        /// <returns></returns>
        [HttpGet("count")]
        public Task<int> GetCount(ContractKindParameter kind)
        {
            return Accounts.GetContractsCount(kind);
        }

        /// <summary>
        /// Get contract by address
        /// </summary>
        /// <remarks>
        /// Returns a contract account with the specified address.
        /// </remarks>
        /// <param name="address">Contract address (starting with KT)</param>
        /// <returns></returns>
        [HttpGet("{address}")]
        public Task<Contract> GetByAddress([Address] string address)
        {
            return Accounts.GetContract(address);
        }

        /// <summary>
        /// Get contract code
        /// </summary>
        /// <remarks>
        /// Returns a code of the specified contract.
        /// </remarks>
        /// <param name="address">Contract address (starting with KT)</param>
        /// <param name="format">Code format (`0` - micheline, `1` - michelson, `2` - bytes (base64))</param>
        /// <returns></returns>
        [HttpGet("{address}/code")]
        public async Task<object> GetCode([Address] string address, [Range(0, 2)] int format = 0)
        {
            if (format == 0)
                return await Accounts.GetMichelineCode(address);
            else if (format == 1)
                return await Accounts.GetMichelsonCode(address);
            return await Accounts.GetByteCode(address);
        }

        /// <summary>
        /// Get contract entrypoints
        /// </summary>
        /// <remarks>
        /// Returns entrypoints of the specified contract.
        /// </remarks>
        /// <param name="address">Contract address (starting with KT)</param>
        /// <param name="all">If true, returns all entrypoints, including unused ones.
        /// Unused means that the entrypoint can be normalized to a more specific one.
        /// For example here `(or %entry1 (unit %entry2) (nat %entry3))` the `%entry1` is unused entrypoint
        /// because it can be normalized to `%entry2` or `%entry3`</param>
        /// <param name="json">Include parameters schema in human-readable JSON format</param>
        /// <param name="micheline">Include parameters schema in micheline format</param>
        /// <param name="michelson">Include parameters schema in michelson format</param>
        /// <returns></returns>
        [HttpGet("{address}/entrypoints")]
        public Task<IEnumerable<Entrypoint>> GetEntrypoints([Address] string address, bool all = false, bool json = true, bool micheline = false, bool michelson = false)
        {
            return Accounts.GetEntrypoints(address, all, json, micheline, michelson);
        }

        /// <summary>
        /// Get entrypoint by name
        /// </summary>
        /// <remarks>
        /// Returns contract's entrypoint with specified name.
        /// </remarks>
        /// <param name="address">Contract address (starting with KT)</param>
        /// <param name="name">Entrypoint name</param>
        /// <param name="json">Include parameters schema in human-readable JSON format</param>
        /// <param name="micheline">Include parameters schema in micheline format</param>
        /// <param name="michelson">Include parameters schema in michelson format</param>
        /// <returns></returns>
        [HttpGet("{address}/entrypoints/{name}")]
        public Task<Entrypoint> GetEntrypointByName([Address] string address, string name, bool json = true, bool micheline = false, bool michelson = false)
        {
            return Accounts.GetEntrypoint(address, name, json, micheline, michelson);
        }

        /// <summary>
        /// Build entrypoint parameters
        /// </summary>
        /// <remarks>
        /// Returns micheline parameters converted from its JSON representation.
        /// </remarks>
        /// <param name="address">Contract address</param>
        /// <param name="name">Entrypoint name</param>
        /// <param name="value">Json parameters</param>
        /// <returns></returns>
        [HttpGet("{address}/entrypoints/{name}/build")]
        public async Task<ActionResult> BuildEntrypointParameters([Address] string address, string name, string value)
        {
            try
            {
                using var doc = JsonDocument.Parse(value);
                return Ok(await Accounts.BuildEntrypointParameters(address, name, doc.RootElement));
            }
            catch (Exception ex)
            {
                return new BadRequest(nameof(value), ex.Message);
            }
        }

        /// <summary>
        /// Build entrypoint parameters
        /// </summary>
        /// <remarks>
        /// Returns micheline parameters converted from its JSON representation.
        /// </remarks>
        /// <param name="address">Contract address</param>
        /// <param name="name">Entrypoint name</param>
        /// <param name="value">Json parameters</param>
        /// <returns></returns>
        [HttpPost("{address}/entrypoints/{name}/build")]
        public async Task<ActionResult> BuildEntrypointParameters([Address] string address, string name, [FromBody] object value)
        {
            try
            {
                return Ok(await Accounts.BuildEntrypointParameters(address, name, value));
            }
            catch (Exception ex)
            {
                return new BadRequest(nameof(value), ex.Message);
            }
        }

        /// <summary>
        /// Get contract storage
        /// </summary>
        /// <remarks>
        /// Returns contract storage value in JSON format.
        /// </remarks>
        /// <param name="address">Contract address</param>
        /// <param name="level">Level at which storage value should be taken. If `0` or not specified, the current value will be returned.</param>
        /// <param name="path">Path in the JSON value (point-separated list of field names, e.g. `path=settings.refund_time` to return</param>
        /// <returns></returns>
        [HttpGet("{address}/storage")]
        public async Task<ActionResult> GetStorage([Address] string address, [Min(0)] int level = 0, string path = null)
        {
            #region safe path
            string[] safePath = null;
            if (path != null)
            {
                var arr = path.Replace("..", "*").Split(".", StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < arr.Length; i++)
                {
                    arr[i] = arr[i].Replace("*", ".");

                    if (!Regex.IsMatch(arr[i], "^[0-9A-z_.%@]+$"))
                        return new BadRequest(nameof(path), $"Invalid path value '{arr[i]}'");
                }

                if (arr.Length > 0)
                    safePath = arr;
            }
            #endregion

            if (level == 0)
                return this.Json(await Accounts.GetStorageValue(address, safePath));
            return this.Json(await Accounts.GetStorageValue(address, safePath, level));
        }

        /// <summary>
        /// Get contract storage schema
        /// </summary>
        /// <remarks>
        /// Returns JSON schema of the contract storage.
        /// </remarks>
        /// <param name="address">Contract address</param>
        /// <param name="level">Level at which storage schema should be taken. If `0` or not specified, the current schema will be returned.</param>
        /// <returns></returns>
        [HttpGet("{address}/storage/schema")]
        public async Task<ActionResult> GetStorageSchema([Address] string address, [Min(0)] int level = 0)
        {
            if (level == 0)
                return this.Json(await Accounts.GetStorageSchema(address));
            return this.Json(await Accounts.GetStorageSchema(address, level));
        }

        /// <summary>
        /// Get contract storage history
        /// </summary>
        /// <remarks>
        /// Returns contract storage historical values.
        /// </remarks>
        /// <param name="address">Contract address</param>
        /// <param name="lastId">Id of the last item received (for pagination)</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet("{address}/storage/history")]
        public Task<IEnumerable<StorageRecord>> GetStorageHistory([Address] string address, [Min(0)] int lastId = 0, [Range(0, 1000)] int limit = 10)
        {
            return Accounts.GetStorageHistory(address, lastId, limit);
        }

        /// <summary>
        /// Get raw contract storage
        /// </summary>
        /// <remarks>
        /// Returns raw contract storage value in micheline format.
        /// </remarks>
        /// <param name="address">Contract address</param>
        /// <param name="level">Level at which storage value should be taken. If `0` or not specified, the current value will be returned.</param>
        /// <returns></returns>
        [HttpGet("{address}/storage/raw")]
        public Task<IMicheline> GetRawStorage([Address] string address, [Min(0)] int level = 0)
        {
            if (level == 0)
                return Accounts.GetRawStorageValue(address);
            return Accounts.GetRawStorageValue(address, level);
        }

        /// <summary>
        /// Get raw contract storage schema
        /// </summary>
        /// <remarks>
        /// Returns micheline schema (type) of the contract storage.
        /// </remarks>
        /// <param name="address">Contract address</param>
        /// <param name="level">Level at which storage schema should be taken. If `0` or not specified, the current schema will be returned.</param>
        /// <returns></returns>
        [HttpGet("{address}/storage/raw/schema")]
        public Task<IMicheline> GetRawStorageSchema([Address] string address, [Min(0)] int level = 0)
        {
            if (level == 0)
                return Accounts.GetRawStorageSchema(address);
            return Accounts.GetRawStorageSchema(address, level);
        }

        /// <summary>
        /// Get raw contract storage history
        /// </summary>
        /// <remarks>
        /// Returns raw contract storage historical values.
        /// </remarks>
        /// <param name="address">Contract address</param>
        /// <param name="lastId">Id of the last item received (for pagination)</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet("{address}/storage/raw/history")]
        public Task<IEnumerable<StorageRecord>> GetRawStorageHistory([Address] string address, [Min(0)] int lastId = 0, [Range(0, 1000)] int limit = 10)
        {
            return Accounts.GetRawStorageHistory(address, lastId, limit);
        }
    }
}
