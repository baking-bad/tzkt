using Dapper;
using Npgsql;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class DalRepository
    {
        readonly NpgsqlDataSource DataSource;
        readonly AccountsCache Accounts;
        readonly ProtocolsCache Protocols;
        readonly QuotesCache Quotes;

        public DalRepository(NpgsqlDataSource dataSource, AccountsCache accounts, ProtocolsCache protocols, QuotesCache quotes)
        {
            DataSource = dataSource;
            Accounts = accounts;
            Protocols = protocols;
            Quotes = quotes;
        }

        #region commiments
        public async Task<int> GetCommitmentsCount(
            DalCommitmentHashParameter hash,
            Int32Parameter level,
            Int32Parameter slotIndex,
            AccountParameter publisher)
        {
            var sql = new SqlBuilder($"""
                SELECT COUNT(*) FROM "DalCommitmentStatus" AS dc
                INNER JOIN "DalPublishCommitmentOps" AS op ON dc."PublishmentId" = op."Id"
                """)
                .FilterA(@"op.""Commitment""", hash)
                .FilterA(@"op.""Level""", level)
                .FilterA(@"op.""Slot""", slotIndex)
                .FilterA(@"op.""SenderId""", publisher);

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }
        #endregion
    }
}
