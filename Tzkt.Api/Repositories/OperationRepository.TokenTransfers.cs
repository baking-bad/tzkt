using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public partial class OperationRepository : DbConnection
    {
        public async Task<IEnumerable<TokenTransferOperation>> GetTokenTransfers(
            AnyOfParameter anyof,
            AccountParameter from,
            AccountParameter to,
            Int32Parameter level,
            DateTimeParameter timestamp,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            Symbols quote)
        {
            var _timestamp = timestamp == null ? null : new TimestampParameter
            {
                Eq = timestamp.Eq == null ? null : Times.FindLevel(timestamp.Eq.Value, SearchMode.Exact),
                Ne = timestamp.Ne == null ? null : Times.FindLevel(timestamp.Ne.Value, SearchMode.Exact),
                Gt = timestamp.Gt == null ? null : Times.FindLevel(timestamp.Gt.Value, SearchMode.ExactOrLower),
                Ge = timestamp.Ge == null ? null : Times.FindLevel(timestamp.Ge.Value, SearchMode.ExactOrHigher),
                Lt = timestamp.Lt == null ? null : Times.FindLevel(timestamp.Lt.Value, SearchMode.ExactOrHigher),
                Le = timestamp.Le == null ? null : Times.FindLevel(timestamp.Le.Value, SearchMode.ExactOrLower),
                In = timestamp.In?.Select(x => Times.FindLevel(x, SearchMode.Exact)).ToList(),
                Ni = timestamp.Ni?.Select(x => Times.FindLevel(x, SearchMode.Exact)).ToList()
            };

            var sql = new SqlBuilder(@"
                SELECT  tr.*,
                        t.""ContractId"" as ""tContractId"",
                        t.""TokenId"" as ""tTokenId"",
                        t.""Metadata"" ->> 'name' as ""tName"",
                        t.""Metadata"" ->> 'symbol' as ""tSymbol"",
                        t.""Metadata"" ->> 'decimals' as ""tDecimals"",
                        t.""Metadata"" ->> 'shouldPreferSymbol' as ""tShouldPreferSymbol"",
                        t.""Metadata"" ->> 'isBooleanAmount' as ""tIsBooleanAmount""
                FROM ""TokenTransfers"" AS tr
                INNER JOIN ""Tokens"" AS t ON t.""Id"" = tr.""TokenId""")
                .FilterA(anyof, x => x == "from" ? @"tr.""FromId""" : @"tr.""ToId""")
                .FilterA(@"tr.""FromId""", from)
                .FilterA(@"tr.""ToId""", to)
                .FilterA(@"tr.""Level""", level)
                .FilterA(@"tr.""Level""", _timestamp)
                .Take(sort, offset, limit, x => x == "level" ? ("Level", "Level") : ("Id", "Id"), "tr");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new TokenTransferOperation
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = Times[row.Level],
                Contract = Accounts.GetAlias(row.tContractId),
                TokenId = row.tTokenId,
                From = row.FromId == null ? null : Accounts.GetAlias(row.FromId),
                To = row.ToId == null ? null : Accounts.GetAlias(row.ToId),
                Amount = row.Amount,
                Name = row.tName,
                Symbol = row.tSymbol,
                Decimals = row.tDecimals,
                IsBooleanAmount = row.tIsBooleanAmount,
                ShouldPreferSymbol = row.tShouldPreferSymbol,
                TransactionId = row.TransactionId,
                OriginationId = row.OriginationId,
                MigrationId = row.MigrationId,
                Quote = Quotes.Get(quote, row.Level)
            });
        }
    }
}
