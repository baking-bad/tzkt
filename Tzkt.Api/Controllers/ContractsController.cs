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
using Tzkt.Api.Utils;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/contracts")]
    public class ContractsController : ControllerBase
    {
        private readonly AccountRepository Accounts;
        private readonly BigMapsRepository BigMaps;

        public ContractsController(AccountRepository accounts, BigMapsRepository bigMaps)
        {
            Accounts = accounts;
            BigMaps = bigMaps;
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
        /// <param name="typeHash">Filters contracts by 32-bit hash of contract parameter and storage types (helpful for searching similar contracts)</param>
        /// <param name="codeHash">Filters contracts by 32-bit hash of contract code (helpful for searching same contracts)</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts contracts by specified field. Supported fields: `id` (default), `balance`, `firstActivity`, `lastActivity`, `numTransactions`.</param>
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
            Int32Parameter typeHash,
            Int32Parameter codeHash,
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
                return Ok(await Accounts.GetContracts(kind, creator, manager, @delegate, lastActivity, typeHash, codeHash, sort, offset, limit, includeStorage));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Accounts.GetContracts(kind, creator, manager, @delegate, lastActivity, typeHash, codeHash, sort, offset, limit, select.Values[0], includeStorage));
                else
                    return Ok(await Accounts.GetContracts(kind, creator, manager, @delegate, lastActivity, typeHash, codeHash, sort, offset, limit, select.Values, includeStorage));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Accounts.GetContracts(kind, creator, manager, @delegate, lastActivity, typeHash, codeHash, sort, offset, limit, select.Fields[0], includeStorage));
                else
                {
                    return Ok(new SelectionResponse
                    {
                         Cols = select.Fields,
                         Rows = await Accounts.GetContracts(kind, creator, manager, @delegate, lastActivity, typeHash, codeHash, sort, offset, limit, select.Fields, includeStorage)
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
        public Task<Contract> GetByAddress([Required][KTAddress] string address)
        {
            return Accounts.GetContract(address);
        }

        /// <summary>
        /// Get same contracts
        /// </summary>
        /// <remarks>
        /// Returns contracts which have the same script as the specified one.
        /// Note, contract scripts are compared by 32-bit hash, so in very rare cases there may be collisions.
        /// </remarks>
        /// <param name="address">Contract address (starting with KT)</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts contracts by specified field. Supported fields: `id` (default), `balance`, `firstActivity`, `lastActivity`, `numTransactions`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="includeStorage">Specifies whether to include contract storage value in response.</param>
        /// <returns></returns>
        [HttpGet("{address}/same")]
        public async Task<ActionResult<IEnumerable<Contract>>> GetSame(
            [Required][KTAddress] string address,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            bool includeStorage = false)
        {
            #region validates
            if (sort != null && !sort.Validate("id", "balance", "firstActivity", "lastActivity", "numTransactions"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var rawAcc = await Accounts.GetRawAsync(address);
            if (rawAcc is not Services.Cache.RawContract contract)
                return Ok(Enumerable.Empty<Contract>());

            var codeHash = new Int32Parameter { Eq = contract.CodeHash };

            if (select == null)
                return Ok(await Accounts.GetContracts(null, null, null, null, null, null, codeHash, sort, offset, limit, includeStorage));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Accounts.GetContracts(null, null, null, null, null, null, codeHash, sort, offset, limit, select.Values[0], includeStorage));
                else
                    return Ok(await Accounts.GetContracts(null, null, null, null, null, null, codeHash, sort, offset, limit, select.Values, includeStorage));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Accounts.GetContracts(null, null, null, null, null, null, codeHash, sort, offset, limit, select.Fields[0], includeStorage));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Accounts.GetContracts(null, null, null, null, null, null, codeHash, sort, offset, limit, select.Fields, includeStorage)
                    });
                }
            }
        }

        /// <summary>
        /// Get similar contracts
        /// </summary>
        /// <remarks>
        /// Returns contracts which have the same interface (parameter and storage types) as the specified one.
        /// Note, contract parameter and storage types are compared by 32-bit hash, so in very rare cases there may be collisions.
        /// </remarks>
        /// <param name="address">Contract address (starting with KT)</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts contracts by specified field. Supported fields: `id` (default), `balance`, `firstActivity`, `lastActivity`, `numTransactions`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="includeStorage">Specifies whether to include contract storage value in response.</param>
        /// <returns></returns>
        [HttpGet("{address}/similar")]
        public async Task<ActionResult<IEnumerable<Contract>>> GetSimilar(
            [Required][KTAddress] string address,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            bool includeStorage = false)
        {
            #region validates
            if (sort != null && !sort.Validate("id", "balance", "firstActivity", "lastActivity", "numTransactions"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var rawAcc = await Accounts.GetRawAsync(address);
            if (rawAcc is not Services.Cache.RawContract contract)
                return Ok(Enumerable.Empty<Contract>());

            var typeHash = new Int32Parameter { Eq = contract.TypeHash };

            if (select == null)
                return Ok(await Accounts.GetContracts(null, null, null, null, null, typeHash, null, sort, offset, limit, includeStorage));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Accounts.GetContracts(null, null, null, null, null, typeHash, null, sort, offset, limit, select.Values[0], includeStorage));
                else
                    return Ok(await Accounts.GetContracts(null, null, null, null, null, typeHash, null, sort, offset, limit, select.Values, includeStorage));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Accounts.GetContracts(null, null, null, null, null, typeHash, null, sort, offset, limit, select.Fields[0], includeStorage));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Accounts.GetContracts(null, null, null, null, null, typeHash, null, sort, offset, limit, select.Fields, includeStorage)
                    });
                }
            }
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
        public async Task<object> GetCode([Required][KTAddress] string address, [Range(0, 2)] int format = 0)
        {
            if (format == 0)
                return await Accounts.GetMichelineCode(address);
            else if (format == 1)
                return await Accounts.GetMichelsonCode(address);
            return await Accounts.GetByteCode(address);
        }

        /// <summary>
        /// Get JSON Schema [2020-12] interface for the contract
        /// </summary>
        /// <remarks>
        /// Returns standard JSON Schema for contract storage, entrypoints, and Big_map entries.
        /// </remarks>
        /// <param name="address">Contract address</param>
        /// <returns></returns>
        [HttpGet("{address}/interface")]
        public Task<ContractInterface> GetInterface([Required][KTAddress] string address)
        {
            return Accounts.GetContractInterface(address);
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
        public Task<IEnumerable<Entrypoint>> GetEntrypoints([Required][KTAddress] string address, bool all = false, bool json = true, bool micheline = false, bool michelson = false)
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
        public Task<Entrypoint> GetEntrypointByName([Required][KTAddress] string address, [Required] string name, bool json = true, bool micheline = false, bool michelson = false)
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
        public async Task<ActionResult> BuildEntrypointParameters([Required][KTAddress] string address, [Required] string name, string value)
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
        public async Task<ActionResult> BuildEntrypointParameters([Required][KTAddress] string address, [Required] string name, [FromBody] object value)
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
        public async Task<ActionResult> GetStorage([Required][KTAddress] string address, [Min(0)] int level = 0, string path = null)
        {
            #region safe path
            JsonPath[] jsonPath = null;
            if (path != null)
            {
                if (!JsonPath.TryParse(path, out jsonPath))
                    return new BadRequest(nameof(path),
                        $"Path contains invalid item: {jsonPath.First(x => x.Type == JsonPathType.None).Value}");

                if (jsonPath.Any(x => x.Type == JsonPathType.Any))
                    return new BadRequest(nameof(path),
                        "Path contains invalid item: [*]");
            }
            #endregion

            if (level == 0)
                return this.Json(await Accounts.GetStorageValue(address, jsonPath));
            return this.Json(await Accounts.GetStorageValue(address, jsonPath, level));
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
        public async Task<ActionResult> GetStorageSchema([Required][KTAddress] string address, [Min(0)] int level = 0)
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
        public Task<IEnumerable<StorageRecord>> GetStorageHistory([Required][KTAddress] string address, [Min(0)] int lastId = 0, [Range(0, 1000)] int limit = 10)
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
        public Task<IMicheline> GetRawStorage([Required][KTAddress] string address, [Min(0)] int level = 0)
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
        public Task<IMicheline> GetRawStorageSchema([Required][KTAddress] string address, [Min(0)] int level = 0)
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
        public Task<IEnumerable<StorageRecord>> GetRawStorageHistory([Required][KTAddress] string address, [Min(0)] int lastId = 0, [Range(0, 1000)] int limit = 10)
        {
            return Accounts.GetRawStorageHistory(address, lastId, limit);
        }

        /// <summary>
        /// Get contract bigmaps
        /// </summary>
        /// <remarks>
        /// Returns all active bigmaps allocated in the contract storage.
        /// </remarks>
        /// <param name="address">Contract address</param>
        /// <param name="tags">Filters bigmaps tags (`token_metadata` - tzip-12, `metadata` - tzip-16).</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object.
        /// If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts bigmaps by specified field. Supported fields: `id` (default), `firstLevel`, `lastLevel`, `totalKeys`, `activeKeys`, `updates`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="micheline">Format of the bigmap key and value: `0` - JSON, `1` - JSON string, `2` - Micheline, `3` - Micheline string</param>
        /// <returns></returns>
        [HttpGet("{address}/bigmaps")]
        public async Task<ActionResult<IEnumerable<BigMap>>> GetBigMaps(
            [Required][KTAddress] string address,
            BigMapTagsParameter tags,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            MichelineFormat micheline = MichelineFormat.Json)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "firstLevel", "lastLevel", "totalKeys", "activeKeys", "updates"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            var acc = await Accounts.GetRawAsync(address);
            if (acc is not Services.Cache.RawContract rawContract)
                return Ok(Enumerable.Empty<BigMap>());

            var contract = new AccountParameter { Eq = rawContract.Id };

            if (select == null)
                return Ok(await BigMaps.Get(contract, null, tags, true, null, sort, offset, limit, micheline));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await BigMaps.Get(contract, null, tags, true, null, sort, offset, limit, select.Values[0], micheline));
                else
                    return Ok(await BigMaps.Get(contract, null, tags, true, null, sort, offset, limit, select.Values, micheline));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await BigMaps.Get(contract, null, tags, true, null, sort, offset, limit, select.Fields[0], micheline));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await BigMaps.Get(contract, null, tags, true, null, sort, offset, limit, select.Fields, micheline)
                    });
                }
            }
        }

        /// <summary>
        /// Get bigmap by name
        /// </summary>
        /// <remarks>
        /// Returns contract bigmap with the specified name or storage path.
        /// </remarks>
        /// <param name="address">Contract address</param>
        /// <param name="name">Bigmap name is the last piece of the bigmap storage path.
        /// For example, if the storage path is `ledger` or `assets.ledger`, then the name is `ledger`.
        /// If there are multiple bigmaps with the same name, for example `assets.ledger` and `tokens.ledger`, you can specify the full path.</param>
        /// <param name="micheline">Format of the bigmap key and value: `0` - JSON, `1` - JSON string, `2` - Micheline, `3` - Micheline string</param>
        /// <returns></returns>
        [HttpGet("{address}/bigmaps/{name}")]
        public async Task<ActionResult<BigMap>> GetBigMapByName(
            [Required][KTAddress] string address,
            [Required] string name,
            MichelineFormat micheline = MichelineFormat.Json)
        {
            var acc = await Accounts.GetRawAsync(address);
            if (acc is not Services.Cache.RawContract contract)
                return Ok(null);

            return Ok(await BigMaps.Get(contract.Id, name, micheline));
        }

        /// <summary>
        /// Get bigmap keys
        /// </summary>
        /// <remarks>
        /// Returns keys of a contract bigmap with the specified name.
        /// </remarks>
        /// <param name="address">Contract address</param>
        /// <param name="name">Bigmap name is the last piece of the bigmap storage path.
        /// For example, if the storage path is `ledger` or `assets.ledger`, then the name is `ledger`.
        /// If there are multiple bigmaps with the same name, for example `assets.ledger` and `tokens.ledger`, you can specify the full path.</param>
        /// <param name="active">Filters keys by status: `true` - active, `false` - removed.</param>
        /// <param name="key">Filters keys by JSON key. Note, this query parameter supports the following format: `?key{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by, for example: `?key.token_id=...`.</param>
        /// <param name="value">Filters keys by JSON value. Note, this query parameter supports the following format: `?value{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by, for example: `?value.balance.gt=...`.</param>
        /// <param name="lastLevel">Filters bigmap keys by the last update level.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts bigmap keys by specified field. Supported fields: `id` (default), `firstLevel`, `lastLevel`, `updates`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="micheline">Format of the bigmap key and value: `0` - JSON, `1` - JSON string, `2` - Micheline, `3` - Micheline string</param>
        /// <returns></returns>
        [HttpGet("{address}/bigmaps/{name}/keys")]
        public async Task<ActionResult<IEnumerable<BigMapKey>>> GetBigMapByNameKeys(
            [Required][KTAddress] string address,
            [Required] string name,
            bool? active,
            JsonParameter key,
            JsonParameter value,
            Int32Parameter lastLevel,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            MichelineFormat micheline = MichelineFormat.Json)
        {
            var acc = await Accounts.GetRawAsync(address);
            if (acc is not Services.Cache.RawContract contract)
                return Ok(Enumerable.Empty<BigMapKey>());

            var ptr = await BigMaps.GetPtr(contract.Id, name);
            if (ptr == null)
                return Ok(Enumerable.Empty<BigMapKey>());

            #region validate
            if (sort != null && !sort.Validate("id", "firstLevel", "lastLevel", "updates"))
                return new BadRequest(nameof(sort), "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await BigMaps.GetKeys((int)ptr, active, key, value, lastLevel, sort, offset, limit, micheline));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await BigMaps.GetKeys((int)ptr, active, key, value, lastLevel, sort, offset, limit, select.Values[0], micheline));
                else
                    return Ok(await BigMaps.GetKeys((int)ptr, active, key, value, lastLevel, sort, offset, limit, select.Values, micheline));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await BigMaps.GetKeys((int)ptr, active, key, value, lastLevel, sort, offset, limit, select.Fields[0], micheline));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await BigMaps.GetKeys((int)ptr, active, key, value, lastLevel, sort, offset, limit, select.Fields, micheline)
                    });
                }
            }
        }

        /// <summary>
        /// Get bigmap key
        /// </summary>
        /// <remarks>
        /// Returns the specified bigmap key.
        /// </remarks>
        /// <param name="address">Contract address</param>
        /// <param name="name">Bigmap name is the last piece of the bigmap storage path.
        /// For example, if the storage path is `ledger` or `assets.ledger`, then the name is `ledger`.
        /// If there are multiple bigmaps with the same name, for example `assets.ledger` and `tokens.ledger`, you can specify the full path.</param>
        /// <param name="key">Either a key hash (`expr123...`) or a plain value (`abcde...`).
        /// Even if the key is complex (an object or an array), you can specify it as is, for example, `/keys/{"address":"tz123","token":123}`.</param>
        /// <param name="micheline">Format of the bigmap key and value: `0` - JSON, `1` - JSON string, `2` - Micheline, `3` - Micheline string</param>
        /// <returns></returns>
        [HttpGet("{address}/bigmaps/{name}/keys/{key}")]
        public async Task<ActionResult<BigMapKey>> GetKey(
            [Required][KTAddress] string address,
            [Required] string name,
            [Required] string key,
            MichelineFormat micheline = MichelineFormat.Json)
        {
            var acc = await Accounts.GetRawAsync(address);
            if (acc is not Services.Cache.RawContract contract)
                return Ok(null);

            var ptr = await BigMaps.GetPtr(contract.Id, name);
            if (ptr == null)
                return Ok(null);

            try
            {
                if (Regex.IsMatch(key, @"^expr[0-9A-z]{50}$"))
                    return Ok(await BigMaps.GetKeyByHash((int)ptr, key, micheline));

                using var doc = JsonDocument.Parse(WrapKey(key));
                return Ok(await BigMaps.GetKey((int)ptr, doc.RootElement.GetRawText(), micheline));
            }
            catch (JsonException)
            {
                return new BadRequest(nameof(key), "invalid json value");
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Get bigmap key updates
        /// </summary>
        /// <remarks>
        /// Returns updates history for the specified bigmap key.
        /// </remarks>
        /// <param name="address">Contract address</param>
        /// <param name="name">Bigmap name is the last piece of the bigmap storage path.
        /// For example, if the storage path is `ledger` or `assets.ledger`, then the name is `ledger`.
        /// If there are multiple bigmaps with the same name, for example `assets.ledger` and `tokens.ledger`, you can specify the full path.</param>
        /// <param name="key">Either a key hash (`expr123...`) or a plain value (`abcde...`).
        /// Even if the key is complex (an object or an array), you can specify it as is, for example, `/keys/{"address":"tz123","token":123}`.</param>
        /// <param name="sort">Sorts bigmap updates by specified field. Supported fields: `id` (default).</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="micheline">Format of the key value: `0` - JSON, `1` - JSON string, `2` - Micheline, `3` - Micheline string</param>
        /// <returns></returns>
        [HttpGet("{address}/bigmaps/{name}/keys/{key}/updates")]
        public async Task<ActionResult<IEnumerable<BigMapKeyUpdate>>> GetKeyUpdates(
            [Required][KTAddress] string address,
            [Required] string name,
            [Required] string key,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            MichelineFormat micheline = MichelineFormat.Json)
        {
            var acc = await Accounts.GetRawAsync(address);
            if (acc is not Services.Cache.RawContract contract)
                return Ok(Enumerable.Empty<BigMapKeyUpdate>());

            var ptr = await BigMaps.GetPtr(contract.Id, name);
            if (ptr == null)
                return Ok(Enumerable.Empty<BigMapKeyUpdate>());

            #region validate
            if (sort != null && !sort.Validate("id"))
                return new BadRequest(nameof(sort), "Sorting by the specified field is not allowed.");
            #endregion

            try
            {
                if (Regex.IsMatch(key, @"^expr[0-9A-z]{50}$"))
                    return Ok(await BigMaps.GetKeyByHashUpdates((int)ptr, key, sort, offset, limit, micheline));

                using var doc = JsonDocument.Parse(WrapKey(key));
                return Ok(await BigMaps.GetKeyUpdates((int)ptr, doc.RootElement.GetRawText(), sort, offset, limit, micheline));
            }
            catch (JsonException)
            {
                return new BadRequest(nameof(key), "invalid json value");
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Get historical keys
        /// </summary>
        /// <remarks>
        /// Returns a list of bigmap keys at the specific block.
        /// </remarks>
        /// <param name="address">Contract address</param>
        /// <param name="name">Bigmap name is the last piece of the bigmap storage path.
        /// For example, if the storage path is `ledger` or `assets.ledger`, then the name is `ledger`.
        /// If there are multiple bigmaps with the same name, for example `assets.ledger` and `tokens.ledger`, you can specify the full path.</param>
        /// <param name="level">Level of the block at which you want to get bigmap keys</param>
        /// <param name="active">Filters keys by status: `true` - active, `false` - removed.</param>
        /// <param name="key">Filters keys by JSON key. Note, this query parameter supports the following format: `?key{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by, for example: `?key.token_id=...`.</param>
        /// <param name="value">Filters keys by JSON value. Note, this query parameter supports the following format: `?value{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by, for example: `?value.balance.gt=...`.</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts bigmap keys by specified field. Supported fields: `id` (default).</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <param name="micheline">Format of the bigmap key and value: `0` - JSON, `1` - JSON string, `2` - Micheline, `3` - Micheline string</param>
        /// <returns></returns>
        [HttpGet("{address}/bigmaps/{name}/historical_keys/{level:int}")]
        public async Task<ActionResult<IEnumerable<BigMapKeyHistorical>>> GetHistoricalKeys(
            [Required][KTAddress] string address,
            [Required] string name,
            [Min(0)] int level,
            bool? active,
            JsonParameter key,
            JsonParameter value,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100,
            MichelineFormat micheline = MichelineFormat.Json)
        {
            var acc = await Accounts.GetRawAsync(address);
            if (acc is not Services.Cache.RawContract contract)
                return Ok(Enumerable.Empty<BigMapKeyHistorical>());

            var ptr = await BigMaps.GetPtr(contract.Id, name);
            if (ptr == null)
                return Ok(Enumerable.Empty<BigMapKeyHistorical>());

            #region validate
            if (sort != null && !sort.Validate("id"))
                return new BadRequest(nameof(sort), "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await BigMaps.GetHistoricalKeys((int)ptr, level, active, key, value, sort, offset, limit, micheline));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await BigMaps.GetHistoricalKeys((int)ptr, level, active, key, value, sort, offset, limit, select.Values[0], micheline));
                else
                    return Ok(await BigMaps.GetHistoricalKeys((int)ptr, level, active, key, value, sort, offset, limit, select.Values, micheline));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await BigMaps.GetHistoricalKeys((int)ptr, level, active, key, value, sort, offset, limit, select.Fields[0], micheline));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await BigMaps.GetHistoricalKeys((int)ptr, level, active, key, value, sort, offset, limit, select.Fields, micheline)
                    });
                }
            }
        }

        /// <summary>
        /// Get historical key
        /// </summary>
        /// <remarks>
        /// Returns the specified bigmap key at the specific block.
        /// </remarks>
        /// <param name="address">Contract address</param>
        /// <param name="name">Bigmap name is the last piece of the bigmap storage path.
        /// For example, if the storage path is `ledger` or `assets.ledger`, then the name is `ledger`.
        /// If there are multiple bigmaps with the same name, for example `assets.ledger` and `tokens.ledger`, you can specify the full path.</param>
        /// <param name="level">Level of the block at which you want to get bigmap key</param>
        /// <param name="key">Either a key hash (`expr123...`) or a plain value (`abcde...`).
        /// Even if the key is complex (an object or an array), you can specify it as is, for example, `/keys/{"address":"tz123","token":123}`.</param>
        /// <param name="micheline">Format of the bigmap key and value: `0` - JSON, `1` - JSON string, `2` - Micheline, `3` - Micheline string</param>
        /// <returns></returns>
        [HttpGet("{address}/bigmaps/{name}/historical_keys/{level:int}/{key}")]
        public async Task<ActionResult<BigMapKeyHistorical>> GetKey(
            [Required][KTAddress] string address,
            [Required] string name,
            [Min(0)] int level,
            [Required] string key,
            MichelineFormat micheline = MichelineFormat.Json)
        {
            var acc = await Accounts.GetRawAsync(address);
            if (acc is not Services.Cache.RawContract contract)
                return Ok(null);

            var ptr = await BigMaps.GetPtr(contract.Id, name);
            if (ptr == null)
                return Ok(null);

            try
            {
                if (Regex.IsMatch(key, @"^expr[0-9A-z]{50}$"))
                    return Ok(await BigMaps.GetHistoricalKeyByHash((int)ptr, level, key, micheline));

                using var doc = JsonDocument.Parse(WrapKey(key));
                return Ok(await BigMaps.GetHistoricalKey((int)ptr, level, doc.RootElement.GetRawText(), micheline));
            }
            catch (JsonException)
            {
                return new BadRequest(nameof(key), "invalid json value");
            }
            catch
            {
                throw;
            }
        }

        string WrapKey(string key)
        {
            switch (key[0])
            {
                case '{':
                case '[':
                case '"':
                case 't' when key == "true":
                case 'f' when key == "false":
                case 'n' when key == "null":
                    return key;
                default:
                    return $"\"{key}\"";
            }
        }
    }
}
