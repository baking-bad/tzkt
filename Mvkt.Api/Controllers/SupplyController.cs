using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Mvkt.Api.Services.Cache;
using Mvkt.Data.Models;

namespace Mvkt.Api.Controllers
{
    [ApiController]
    [Route("v1")]
    public class SupplyController : ControllerBase
    {
        readonly NpgsqlDataSource DataSource;
        readonly StateCache State;

        public SupplyController(NpgsqlDataSource dataSource, StateCache state)
        {
            DataSource = dataSource;
            State = state;
        }

        /// <summary>
        /// Get total supply
        /// </summary>
        /// <remarks>
        /// Returns the total supply of tokens in normal decimal format (6 decimal places).
        /// </remarks>
        /// <returns></returns>
        [HttpGet("totalSupply")]
        public async Task<ActionResult<decimal>> GetTotalSupply()
        {
            try
            {
                await using var db = await DataSource.OpenConnectionAsync();
                var row = await db.QueryFirstOrDefaultAsync<Statistics>($@"SELECT * FROM ""Statistics"" WHERE ""Level"" = {State.Current.Level}");
                
                if (row == null)
                    return NotFound("Statistics not found for current level");

                var totalSupplyMicro = row.TotalBootstrapped + row.TotalCommitments + row.TotalCreated
                                     - row.TotalBurned - row.TotalBanished;

                var totalSupply = totalSupplyMicro / 1_000_000m;

                return Ok(totalSupply);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error calculating total supply: {ex.Message}");
            }
        }

        /// <summary>
        /// Get circulating supply
        /// </summary>
        /// <remarks>
        /// Returns the circulating supply of tokens (excluding locked vested funds) in normal decimal format (6 decimal places).
        /// </remarks>
        /// <returns></returns>
        [HttpGet("circulating")]
        public async Task<ActionResult<decimal>> GetCirculating()
        {
            try
            {
                await using var db = await DataSource.OpenConnectionAsync();
                var row = await db.QueryFirstOrDefaultAsync<Statistics>($@"SELECT * FROM ""Statistics"" WHERE ""Level"" = {State.Current.Level}");
                
                if (row == null)
                    return NotFound("Statistics not found for current level");

                var vestingAmount = await GetVestingAmount(db);

                var circulatingSupplyMicro = row.TotalBootstrapped + row.TotalActivated + row.TotalCreated
                                           - row.TotalBurned - row.TotalBanished - row.TotalLost - vestingAmount;

                var circulatingSupply = circulatingSupplyMicro / 1_000_000m;

                return Ok(circulatingSupply);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error calculating circulating supply: {ex.Message}");
            }
        }

        async Task<long> GetVestingAmount(IDbConnection db)
        {
            try
            {
                // Query all vesting contract addresses from the bigmap
                var vestingContracts = await db.QueryAsync<string>($@"
                    SELECT DISTINCT ""JsonValue""->>'vestingContractAddress' as ""VestingContract""
                    FROM ""BigMapKeys""
                    WHERE ""BigMapPtr"" = 34
                    AND ""Active"" = true
                    AND ""JsonValue""->>'vestingContractAddress' IS NOT NULL");

                if (!vestingContracts.Any())
                    return 0;

                // Get the total balance of all vesting contracts
                var vestingAddresses = vestingContracts.Where(x => !string.IsNullOrEmpty(x)).ToList();
                if (!vestingAddresses.Any())
                    return 0;

                var totalVestingBalance = await db.QueryFirstOrDefaultAsync<long>($@"
                    SELECT COALESCE(SUM(""Balance""), 0)::bigint
                    FROM ""Accounts""
                    WHERE ""Address"" = ANY(@addresses)", new { addresses = vestingAddresses });

                return totalVestingBalance;
            }
            catch (Exception)
            {
                // Return 0 if vesting calculation fails
                return 0;
            }
        }
    }
}

