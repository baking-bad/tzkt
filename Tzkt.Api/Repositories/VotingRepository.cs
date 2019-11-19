using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;

using Tzkt.Api.Models;
using Tzkt.Api.Services;

namespace Tzkt.Api.Repositories
{
    public class VotingRepository : DbConnection
    {
        public VotingRepository(IConfiguration config) : base(config)
        {
        }

        #region proposals
        public async Task<Proposal> GetProposal(string hash)
        {
            var sql = @"
                SELECT  ""Hash""
                FROM    ""Proposals""
                WHERE   ""Hash"" = @hash::character(51)
                LIMIT   1";

            using var db = GetConnection();
            var row = await db.QueryFirstOrDefaultAsync(sql, new { hash });
            if (row == null) return null;

            return new Proposal
            {
                Hash = hash
            };
        }

        public async Task<IEnumerable<Proposal>> GetProposals(int limit = 100, int offset = 0)
        {

            var sql = @"
                SELECT  ""Hash""
                FROM    ""Proposals""
                ORDER BY ""Id""
                OFFSET   @offset
                LIMIT    @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new Proposal
            {
                Hash = row.Hash
            });
        }
        #endregion

        #region periods
        public async Task<IEnumerable<VotingPeriod>> GetPeriods(int limit = 100, int offset = 0)
        {

            var sql = @"
                SELECT  ""Kind"", ""StartLevel"", ""EndLevel""
                FROM    ""VotingPeriods""
                ORDER BY ""Id""
                OFFSET   @offset
                LIMIT    @limit";

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql, new { limit, offset });

            return rows.Select(row => new VotingPeriod
            {
                Kind = KindToString(row.Kind),
                FirstLevel = row.StartLevel,
                LastLevel = row.EndLevel
            });
        }
        #endregion

        string KindToString(int kind)
        {
            return kind switch
            {
                0 => "proposal",
                1 => "exploration",
                2 => "testing",
                3 => "promotion",
                _ => "unknown"
            };
        }
    }
}
