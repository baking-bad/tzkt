using Dapper;
using Npgsql;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;
using Tzkt.Data.Models.Base;

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
            AccountParameter publisher,
            Int32Parameter shardsAttested,
            BoolParameter attested)
        {
            var sql = new SqlBuilder($"""
                SELECT COUNT(*) FROM "DalPublishCommitmentOps" AS op WHERE op."Status"={OperationStatus.Applied}
                """)
                .FilterA(@"op.""Commitment""", hash)
                .FilterA(@"op.""Level""", level)
                .FilterA(@"op.""Slot""", slotIndex)
                .FilterA(@"op.""SenderId""", publisher)
                .FilterA(@"dc.""ShardsAttested""", shardsAttested)
                .FilterA(@"dc.""Attested""", attested);

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<DalCommitment>> GetCommitments(
            DalCommitmentHashParameter hash,
            Int32Parameter level,
            Int32Parameter slotIndex,
            AccountParameter publisher,
            Int32Parameter shardsAttested,
            BoolParameter attested,
            SortParameter sort,
            OffsetParameter offset,
            int limit)
        {
            
            var sql = new SqlBuilder($"""
                                      SELECT op.* FROM  "DalPublishCommitmentOps" AS op WHERE op."Status"={OperationStatus.Applied}
                                      """)
                .FilterA(@"op.""Commitment""", hash)
                .FilterA(@"op.""Level""", level)
                .FilterA(@"op.""Slot""", slotIndex)
                .FilterA(@"op.""SenderId""", publisher)
                .FilterA(@"op.""ShardsAttested""", shardsAttested)
                .FilterA(@"op.""Attested""", attested)
                .Take(new Pagination { sort = sort, offset = offset, limit = limit }, x => x switch
                {
                    "slotIndex" => (@"op.""Slot""", @"op.""Slot"""),
                    "level" or _  => (@"op.""Level""", @"op.""Level""")
                }, @"op.""Level""");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new DalCommitment
            {
                Level = row.Level,
                SlotIndex = row.Slot,
                Hash = row.Commitment,
                Publisher = Accounts.GetAlias(row.SenderId),
                ShardsAttested = row.ShardsAttested,
                Attested = row.Attested
            });
        }

        public async Task<object[][]> GetCommitments(
            DalCommitmentHashParameter hash,
            Int32Parameter level,
            Int32Parameter slotIndex,
            AccountParameter publisher,
            Int32Parameter shardsAttested,
            BoolParameter attested,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields)
        {
            var columns = new HashSet<string>(fields.Length);

            foreach (var field in fields)
            {
                switch (field)
                {
                    case "level":
                        columns.Add(@"op.""Level""");
                        break;
                    case "slotIndex":
                        columns.Add(@"op.""Slot""");
                        break;
                    case "hash":
                        columns.Add(@"op.""Commitment""");
                        break;
                    case "publisher":
                        columns.Add(@"op.""SenderId""");
                        break;
                    case "shardsAttested":
                        columns.Add(@"op.""ShardsAttested""");
                        break;
                    case "attested":
                        columns.Add(@"op.""Attested""");
                        break;
                }
            }

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($"""
                                      SELECT {string.Join(',', columns)}
                                      FROM      "DalPublishCommitmentOps" AS op WHERE op."Status"={OperationStatus.Applied}
                                      """) 
                .FilterA(@"op.""Commitment""", hash)
                .FilterA(@"op.""Level""", level)
                .FilterA(@"op.""Slot""", slotIndex)
                .FilterA(@"op.""SenderId""", publisher)
                .FilterA(@"op.""ShardsAttested""", shardsAttested)
                .FilterA(@"op.""Attested""", attested)
                .Take(new Pagination { sort = sort, offset = offset, limit = limit }, x => x switch
                {
                    "slotIndex" => (@"op.""Slot""", @"op.""Slot"""),
                    "level" or _ => (@"op.""Level""", @"op.""Level""")
                }, @"op.""Level""");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "slotIndex":
                        foreach (var row in rows)
                            result[j++][i] = row.Slot;
                        break;
                    case "hash":
                        foreach (var row in rows)
                            result[j++][i] = row.Commitment;
                        break;
                    case "publisher":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.SenderId);
                        break;
                    case "shardsAttested":
                        foreach (var row in rows)
                            result[j++][i] = row.ShardsAttested;
                        break;
                    case "attested":
                        foreach (var row in rows)
                            result[j++][i] = row.Attested;
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetCommitments(
            DalCommitmentHashParameter hash,
            Int32Parameter level,
            Int32Parameter slotIndex,
            AccountParameter publisher,
            Int32Parameter shardsAttested,
            BoolParameter attested,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field)
        {
            var columns = new HashSet<string>(1);

            switch (field)
            {
                case "level":
                    columns.Add(@"op.""Level""");
                    break;
                case "slotIndex":
                    columns.Add(@"op.""Slot""");
                    break;
                case "hash":
                    columns.Add(@"op.""Commitment""");
                    break;
                case "publisher":
                    columns.Add(@"op.""SenderId""");
                    break;
                case "shardsAttested":
                    columns.Add(@"op.""ShardsAttested""");
                    break;
                case "attested":
                    columns.Add(@"op.""Attested""");
                    break;
            }

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($"""
                                      SELECT {string.Join(',', columns)}
                                      FROM       "DalPublishCommitmentOps" AS op WHERE op."Status"={OperationStatus.Applied}
                                      """)
                .FilterA(@"op.""Commitment""", hash)
                .FilterA(@"op.""Level""", level)
                .FilterA(@"op.""Slot""", slotIndex)
                .FilterA(@"op.""SenderId""", publisher)
                .FilterA(@"op.""ShardsAttested""", shardsAttested)
                .FilterA(@"op.""Attested""", attested)
                .Take(new Pagination { sort = sort, offset = offset, limit = limit }, x => x switch
                {
                    "slotIndex" => (@"op.""Slot""", @"op.""Slot"""),
                    "level" or _ => (@"op.""Level""", @"op.""Level""")
                }, @"op.""Level""");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "level":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "slotIndex":
                    foreach (var row in rows)
                        result[j++] = row.Slot;
                    break;
                case "hash":
                    foreach (var row in rows)
                        result[j++] = row.Commitment;
                    break;
                case "publisher":
                    foreach (var row in rows)
                        result[j++] = Accounts.GetAlias(row.SenderId);
                    break;
                case "shardsAttested":
                    foreach (var row in rows)
                        result[j++] = row.ShardsAttested;
                    break;
                case "attested":
                    foreach (var row in rows)
                        result[j++] = row.Attested;
                    break;
            }

            return result;
        }
        #endregion

        #region attestations
        public async Task<int> GetAttestationsCount(
            DalCommitmentHashParameter commitment,
            Int32Parameter publishLevel,
            Int32Parameter slotIndex,
            AccountParameter attester,
            Int32Parameter shardsCount,
            BoolParameter attested)
        {
            var sql = new SqlBuilder($"""
                SELECT COUNT(*) FROM "DalAttestations" AS da
                LEFT JOIN "DalPublishCommitmentOps" AS dc ON da."DalPublishCommitmentOpsId" = dc."Id"
                LEFT JOIN "EndorsementOps" AS eo ON da."AttestationId" = eo."Id" WHERE dc."Status"={OperationStatus.Applied}
                """)
                .FilterA(@"dc.""Commitment""", commitment)
                .FilterA(@"dc.""Level""", publishLevel)
                .FilterA(@"dc.""Slot""", slotIndex)
                .FilterA(@"eo.""DelegateId""", attester)
                .FilterA(@"da.""ShardsCount""", shardsCount)
                .FilterA(@"da.""Attested""", attested);

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<DalAttestation>> GetAttestations(
            DalCommitmentHashParameter commitment,
            Int32Parameter publishLevel,
            Int32Parameter slotIndex,
            AccountParameter attester,
            Int32Parameter shardsCount,
            BoolParameter attested,
            SortParameter sort,
            OffsetParameter offset,
            int limit)
        {
            var sql = new SqlBuilder($"""
                SELECT    dc."Level", dc."Slot", dc."Commitment", eo."DelegateId", da."ShardsCount", da."Attested"
                FROM      "DalAttestations" AS da
                LEFT JOIN "DalPublishCommitmentOps" AS dc ON da."DalPublishCommitmentOpsId" = dc."Id"
                LEFT JOIN "EndorsementOps" AS eo ON da."AttestationId" = eo."Id" WHERE dc."Status"={OperationStatus.Applied}
                """)
                .FilterA(@"dc.""Commitment""", commitment)
                .FilterA(@"dc.""Level""", publishLevel)
                .FilterA(@"dc.""Slot""", slotIndex)
                .FilterA(@"eo.""DelegateId""", attester)
                .FilterA(@"da.""ShardsCount""", shardsCount)
                .FilterA(@"da.""Attested""", attested)
                .Take(new Pagination { sort = sort, offset = offset, limit = limit }, x => x switch
                {
                    "shardsCount" => (@"da.""ShardsCount""", @"da.""ShardsCount"""),
                    "slotIndex" => (@"dc.""Slot""", @"dc.""Slot"""),
                    "publishLevel" or _ => (@"dc.""Level""", @"dc.""Level""")
                }, @"dc.""Level""");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new DalAttestation
            {
                PublishLevel = row.Level,
                SlotIndex = row.Slot,
                Commitment = row.Commitment,
                Attester = Accounts.GetAlias(row.DelegateId),
                ShardsCount = row.ShardsCount,
                Attested = row.Attested
            });
        }

        public async Task<object[][]> GetAttestations(
            DalCommitmentHashParameter commitment,
            Int32Parameter publishLevel,
            Int32Parameter slotIndex,
            AccountParameter attester,
            Int32Parameter shardsCount,
            BoolParameter attested,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string[] fields)
        {
            var columns = new HashSet<string>(fields.Length);
            var needPublishOp = false;
            var needAttestationOp = false;

            foreach (var field in fields)
            {
                switch (field)
                {
                    case "publishLevel":
                        columns.Add(@"dc.""Level""");
                        needPublishOp = true;
                        break;
                    case "slotIndex":
                        columns.Add(@"dc.""Slot""");
                        needPublishOp = true;
                        break;
                    case "commitment":
                        columns.Add(@"dc.""Commitment""");
                        needPublishOp = true;
                        break;
                    case "attester":
                        columns.Add(@"eo.""DelegateId""");
                        needAttestationOp = true;
                        break;
                    case "shardsCount":
                        columns.Add(@"da.""ShardsCount""");
                        break;
                    case "attested":
                        columns.Add(@"da.""Attested""");
                        break;
                }
            }

            if (sort == null || sort.Validate("publishLevel", "slotIndex"))
                needPublishOp = true;
            if (commitment != null || publishLevel != null || slotIndex != null)
                needPublishOp = true;
            if (attester != null)
                needAttestationOp = true;

            if (columns.Count == 0)
                return Array.Empty<object[]>();

            var sql = new SqlBuilder($"""
                                      SELECT {string.Join(',', columns)}
                                      FROM   "DalAttestations" AS da
                                      {
                                          (needAttestationOp ?
                                              $"""
                                               LEFT JOIN "EndorsementOps" AS eo ON da."AttestationId" = eo."Id"
                                               """
                                              : string.Empty)
                                      }                                     
                                      {
                                          (needPublishOp ?
                                              $"""
                                               LEFT JOIN "DalPublishCommitmentOps" AS dc ON da."DalPublishCommitmentOpsId" = dc."Id"
                                               WHERE dc."Status"={OperationStatus.Applied}
                                               """
                                              : string.Empty)
                                      }
                                      """)
                .FilterA(@"dc.""Commitment""", commitment)
                .FilterA(@"dc.""Level""", publishLevel)
                .FilterA(@"dc.""Slot""", slotIndex)
                .FilterA(@"eo.""DelegateId""", attester)
                .FilterA(@"da.""ShardsCount""", shardsCount)
                .FilterA(@"da.""Attested""", attested)
                .Take(new Pagination { sort = sort, offset = offset, limit = limit }, x => x switch
                {
                    "shardsCount" => (@"da.""ShardsCount""", @"da.""ShardsCount"""),
                    "slotIndex" => (@"dc.""Slot""", @"dc.""Slot"""),
                    "publishLevel" or _ => (@"dc.""Level""", @"dc.""Level""")
                }, @"dc.""Level""");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object[fields.Length];

            for (int i = 0, j = 0; i < fields.Length; j = 0, i++)
            {
                switch (fields[i])
                {
                    case "publishLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "slotIndex":
                        foreach (var row in rows)
                            result[j++][i] = row.Slot;
                        break;
                    case "commitment":
                        foreach (var row in rows)
                            result[j++][i] = row.Commitment;
                        break;
                    case "attester":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.DelegateId);
                        break;
                    case "shardsCount":
                        foreach (var row in rows)
                            result[j++][i] = row.ShardsCount;
                        break;
                    case "attested":
                        foreach (var row in rows)
                            result[j++][i] = row.Attested;
                        break;
                }
            }

            return result;
        }

        public async Task<object[]> GetAttestations(
            DalCommitmentHashParameter commitment,
            Int32Parameter publishLevel,
            Int32Parameter slotIndex,
            AccountParameter attester,
            Int32Parameter shardsCount,
            BoolParameter attested,
            SortParameter sort,
            OffsetParameter offset,
            int limit,
            string field)
        {
            var columns = new HashSet<string>(1);
            var needPublishOp = false;
            var needAttestationOp = false;

            switch (field)
            {
                case "publishLevel":
                    columns.Add(@"dc.""Level""");
                    needPublishOp = true;
                    break;
                case "slotIndex":
                    columns.Add(@"dc.""Slot""");
                    needPublishOp = true;
                    break;
                case "commitment":
                    columns.Add(@"dc.""Commitment""");
                    needPublishOp = true;
                    break;
                case "attester":
                    columns.Add(@"eo.""DelegateId""");
                    needAttestationOp = true;
                    break;
                case "shardsCount":
                    columns.Add(@"da.""ShardsCount""");
                    break;
                case "attested":
                    columns.Add(@"da.""Attested""");
                    break;
            }

            if (sort == null || sort.Validate("publishLevel", "slotIndex"))
                needPublishOp = true;
            if (commitment != null || publishLevel != null || slotIndex != null)
                needPublishOp = true;
            if (attester != null)
                needAttestationOp = true;

            if (columns.Count == 0)
                return Array.Empty<object>();

            var sql = new SqlBuilder($"""
                                      SELECT {string.Join(',', columns)}
                                      FROM   "DalAttestations" AS da
                                      {
                                          (needPublishOp ?
                                              $"""
                                               LEFT JOIN "DalPublishCommitmentOps" AS dc ON da."DalPublishCommitmentOpsId" = dc."Id"
                                               """
                                              : string.Empty)
                                      }
                                      {
                                          (needAttestationOp ?
                                              $"""
                                               LEFT JOIN "EndorsementOps" AS eo ON da."AttestationId" = eo."Id"
                                               """
                                              : string.Empty)
                                      }
                                      """)
                .FilterA(@"dc.""Commitment""", commitment)
                .FilterA(@"dc.""Level""", publishLevel)
                .FilterA(@"dc.""Slot""", slotIndex)
                .FilterA(@"eo.""DelegateId""", attester)
                .FilterA(@"da.""ShardsCount""", shardsCount)
                .FilterA(@"da.""Attested""", attested)
                .Take(new Pagination { sort = sort, offset = offset, limit = limit }, x => x switch
                {
                    "shardsCount" => (@"da.""ShardsCount""", @"da.""ShardsCount"""),
                    "slotIndex" => (@"dc.""Slot""", @"dc.""Slot"""),
                    "publishLevel" or _ => (@"dc.""Level""", @"dc.""Level""")
                }, @"dc.""Level""");

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            var result = new object[rows.Count()];
            var j = 0;

            switch (field)
            {
                case "publishLevel":
                    foreach (var row in rows)
                        result[j++] = row.Level;
                    break;
                case "slotIndex":
                    foreach (var row in rows)
                        result[j++] = row.Slot;
                    break;
                case "commitment":
                    foreach (var row in rows)
                        result[j++] = row.Commitment;
                    break;
                case "attester":
                    foreach (var row in rows)
                        result[j++] = Accounts.GetAlias(row.DelegateId);
                    break;
                case "shardsCount":
                    foreach (var row in rows)
                        result[j++] = row.ShardsCount;
                    break;
                case "attested":
                    foreach (var row in rows)
                        result[j++] = row.Attested;
                    break;
            }

            return result;
        }

        #endregion
    }
}
