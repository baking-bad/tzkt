﻿using System.Data;
using System.Numerics;
using Dapper;
using Npgsql;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class TokensRepository
    {
        readonly NpgsqlDataSource DataSource;
        readonly AccountsCache Accounts;
        readonly TimeCache Times;

        public TokensRepository(NpgsqlDataSource dataSource, AccountsCache accounts, TimeCache times)
        {
            DataSource = dataSource;
            Accounts = accounts;
            Times = times;
        }

        #region tokens
        async Task<IEnumerable<dynamic>> QueryTokensAsync(TokenFilter filter, Pagination pagination, List<SelectionField>? fields = null)
        {
            var select = @"
                ""Id"",
                ""ContractId"",
                ""BalancesCount"",
                ""HoldersCount"",
                ""FirstMinterId"",
                ""FirstLevel"",
                ""LastLevel"",
                ""Tags"",
                ""TokenId"",
                ""TotalBurned"",
                ""TotalMinted"",
                ""TotalSupply"",
                ""TransfersCount"",
                ""Metadata""";

            if (fields != null)
            {
                var counter = 0;
                var columns = new HashSet<string>(fields.Count);
                foreach (var field in fields)
                {
                    switch (field.Field)
                    {
                        case "id": columns.Add(@"""Id"""); break;
                        case "contract": columns.Add(@"""ContractId"""); break;
                        case "balancesCount": columns.Add(@"""BalancesCount"""); break;
                        case "holdersCount": columns.Add(@"""HoldersCount"""); break;
                        case "firstMinter": columns.Add(@"""FirstMinterId"""); break;
                        case "firstLevel": columns.Add(@"""FirstLevel"""); break;
                        case "firstTime": columns.Add(@"""FirstLevel"""); break;
                        case "lastLevel": columns.Add(@"""LastLevel"""); break;
                        case "lastTime": columns.Add(@"""LastLevel"""); break;
                        case "standard": columns.Add(@"""Tags"""); break;
                        case "tokenId": columns.Add(@"""TokenId"""); break;
                        case "totalBurned": columns.Add(@"""TotalBurned"""); break;
                        case "totalMinted": columns.Add(@"""TotalMinted"""); break;
                        case "totalSupply": columns.Add(@"""TotalSupply"""); break;
                        case "transfersCount": columns.Add(@"""TransfersCount"""); break;
                        case "metadata":
                            if (field.Path == null)
                            {
                                columns.Add(@"""Metadata""");
                            }
                            else
                            {
                                field.Column = $"c{counter++}";
                                columns.Add($@"""Metadata"" #> '{{{field.PathString}}}' as {field.Column}");
                            }
                            break;
                    }
                }

                if (columns.Count == 0)
                    return [];

                select = string.Join(',', columns);
            }

            static (string, string) TryMetaSort(string field)
            {
                if (field.StartsWith("metadata.") && Regexes.FieldPath().IsMatch(field))
                {
                    var col = $@"""Metadata""#>'{{{field[9..].Replace('.', ',')}}}'";
                    return (col, col);
                }
                return (@"""Id""", @"""Id""");
            }

            var sql = new SqlBuilder($@"SELECT {select} FROM ""Tokens""")
                .Filter("Id", filter.id)
                .Filter("ContractId", filter.contract)
                .Filter("TokenId", filter.tokenId)
                .FilterA(@"(""ContractId"", ""TokenId"")", filter.globalId)
                .Filter("Tags", filter.standard)
                .Filter("TotalMinted", filter.totalMinted)
                .Filter("TotalBurned", filter.totalBurned)
                .Filter("TotalSupply", filter.totalSupply)
                .Filter("FirstMinterId", filter.firstMinter)
                .Filter("FirstLevel", filter.firstLevel)
                .Filter("FirstLevel", filter.firstTime)
                .Filter("LastLevel", filter.lastLevel)
                .Filter("LastLevel", filter.lastTime)
                .Filter("IndexedAt", filter.indexedAt)
                .Filter("Metadata", filter.metadata)
                .Take(pagination, x => x switch
                {
                    "id" => (@"""Id""", @"""Id"""),
                    "tokenId" => (@"""TokenId""", @"""TokenId"""),
                    "transfersCount" => (@"""TransfersCount""", @"""TransfersCount"""),
                    "holdersCount" => (@"""HoldersCount""", @"""HoldersCount"""),
                    "balancesCount" => (@"""BalancesCount""", @"""BalancesCount"""),
                    "firstLevel" => (@"""Id""", @"""FirstLevel"""),
                    "lastLevel" => (@"""LastLevel""", @"""LastLevel"""),
                    "metadata" => (@"""Metadata""", @"""Metadata"""),
                    _ => TryMetaSort(x)
                }, @"""Id""", 100);

            await using var db = await DataSource.OpenConnectionAsync();
            return (await db.QueryAsync(sql.Query, sql.Params)).Take(pagination.limit);
        }

        public async Task<int> GetTokensCount(TokenFilter filter)
        {
            var sql = new SqlBuilder(@"SELECT COUNT(*) FROM ""Tokens""")
                .Filter("Id", filter.id)
                .Filter("ContractId", filter.contract)
                .Filter("TokenId", filter.tokenId)
                .FilterA(@"(""ContractId"", ""TokenId"")", filter.globalId)
                .Filter("Tags", filter.standard)
                .Filter("TotalMinted", filter.totalMinted)
                .Filter("TotalBurned", filter.totalBurned)
                .Filter("TotalSupply", filter.totalSupply)
                .Filter("FirstMinterId", filter.firstMinter)
                .Filter("FirstLevel", filter.firstLevel)
                .Filter("FirstLevel", filter.firstTime)
                .Filter("LastLevel", filter.lastLevel)
                .Filter("LastLevel", filter.lastTime)
                .Filter("IndexedAt", filter.indexedAt)
                .Filter("Metadata", filter.metadata);

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<Token>> GetTokens(TokenFilter filter, Pagination pagination)
        {
            var rows = await QueryTokensAsync(filter, pagination);
            return rows.Select(row => new Token
            {
                Contract = Accounts.GetAlias(row.ContractId),
                Id = row.Id,
                BalancesCount = row.BalancesCount,
                FirstMinter = Accounts.GetAlias(row.FirstMinterId),
                FirstLevel = row.FirstLevel,
                FirstTime = Times[row.FirstLevel],
                HoldersCount = row.HoldersCount,
                LastLevel = row.LastLevel,
                LastTime = Times[row.LastLevel],
                Standard = TokenStandards.ToString(row.Tags),
                TokenId = row.TokenId,
                TotalBurned = row.TotalBurned,
                TotalMinted = row.TotalMinted,
                TotalSupply = row.TotalSupply,
                TransfersCount = row.TransfersCount,
                Metadata = row.Metadata
            });
        }

        public async Task<object?[][]> GetTokens(TokenFilter filter, Pagination pagination, List<SelectionField> fields)
        {
            var rows = await QueryTokensAsync(filter, pagination, fields);

            var result = new object?[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object?[fields.Count];

            for (int i = 0, j = 0; i < fields.Count; j = 0, i++)
            {
                switch (fields[i].Full)
                {
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "contract":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.ContractId);
                        break;
                    case "contract.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.ContractId).Name;
                        break;
                    case "contract.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.ContractId).Address;
                        break;
                    case "balancesCount":
                        foreach (var row in rows)
                            result[j++][i] = row.BalancesCount;
                        break;
                    case "holdersCount":
                        foreach (var row in rows)
                            result[j++][i] = row.HoldersCount;
                        break;
                    case "firstMinter":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.FirstMinterId);
                        break;
                    case "firstMinter.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.FirstMinterId).Name;
                        break;
                    case "firstMinter.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.FirstMinterId).Address;
                        break;
                    case "firstLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.FirstLevel;
                        break;
                    case "firstTime":
                        foreach (var row in rows)
                            result[j++][i] = Times[row.FirstLevel];
                        break;
                    case "lastLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.LastLevel;
                        break;
                    case "lastTime":
                        foreach (var row in rows)
                            result[j++][i] = Times[row.LastLevel];
                        break;
                    case "standard":
                        foreach (var row in rows)
                            result[j++][i] = TokenStandards.ToString(row.Tags);
                        break;
                    case "tokenId":
                        foreach (var row in rows)
                            result[j++][i] = row.TokenId;
                        break;
                    case "totalBurned":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalBurned;
                        break;
                    case "totalMinted":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalMinted;
                        break;
                    case "totalSupply":
                        foreach (var row in rows)
                            result[j++][i] = row.TotalSupply;
                        break;
                    case "transfersCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TransfersCount;
                        break;
                    case "metadata":
                        foreach (var row in rows)
                            result[j++][i] = (RawJson?)row.Metadata;
                        break;
                    default:
                        if (fields[i].Field == "metadata")
                            foreach (var row in rows)
                                result[j++][i] = (RawJson?)((row as IDictionary<string, object>)![fields[i].Column!] as string);
                        break;
                }
            }

            return result;
        }
        #endregion

        #region token balances
        async Task<IEnumerable<dynamic>> QueryTokenBalancesAsync(TokenBalanceFilter filter, Pagination pagination, List<SelectionField>? fields = null)
        {
            var select = @"
                tb.""Id"",
                tb.""AccountId"",
                tb.""Balance"",
                (tb.""Balance"" * t.""Value"")::numeric(1000,0) as ""BalanceValue"",
                tb.""FirstLevel"",
                tb.""LastLevel"",
                tb.""TransfersCount"",
                tb.""TokenId"" as ""tId"",
                tb.""ContractId"" as ""tContractId"",
                t.""TokenId"" as ""tTokenId"",
                t.""Tags"" as ""tTags"",
                t.""TotalSupply"" as ""tTotalSupply"",
                t.""Metadata"" as ""tMetadata""";
            if (fields != null)
            {
                var counter = 0;
                var columns = new HashSet<string>(fields.Count);
                foreach (var field in fields)
                {
                    switch (field.Field)
                    {
                        case "id": columns.Add(@"tb.""Id"""); break;
                        case "account": columns.Add(@"tb.""AccountId"""); break;
                        case "balance": columns.Add(@"tb.""Balance"""); break;
                        case "balanceValue": columns.Add(@"(tb.""Balance"" * t.""Value"")::numeric(1000,0) as ""BalanceValue"""); break;
                        case "firstLevel": columns.Add(@"tb.""FirstLevel"""); break;
                        case "firstTime": columns.Add(@"tb.""FirstLevel"""); break;
                        case "lastLevel": columns.Add(@"tb.""LastLevel"""); break;
                        case "lastTime": columns.Add(@"tb.""LastLevel"""); break;
                        case "transfersCount": columns.Add(@"tb.""TransfersCount"""); break;
                        case "token":
                            if (field.Path == null)
                            {
                                columns.Add(@"tb.""TokenId"" as ""tId""");
                                columns.Add(@"tb.""ContractId"" as ""tContractId""");
                                columns.Add(@"t.""TokenId"" as ""tTokenId""");
                                columns.Add(@"t.""Tags"" as ""tTags""");
                                columns.Add(@"t.""TotalSupply"" as ""tTotalSupply""");
                                columns.Add(@"t.""Metadata"" as ""tMetadata""");
                            }
                            else
                            {
                                var subField = field.SubField()!;
                                switch (subField.Field)
                                {
                                    case "id": columns.Add(@"tb.""TokenId"" as ""tId"""); break;
                                    case "contract": columns.Add(@"tb.""ContractId"" as ""tContractId"""); break;
                                    case "tokenId": columns.Add(@"t.""TokenId"" as ""tTokenId"""); break;
                                    case "standard": columns.Add(@"t.""Tags"" as ""tTags"""); break;
                                    case "totalSupply": columns.Add(@"t.""TotalSupply"" as ""tTotalSupply"""); break;
                                    case "metadata":
                                        if (subField.Path == null)
                                        {
                                            columns.Add(@"t.""Metadata"" as ""tMetadata""");
                                        }
                                        else
                                        {
                                            field.Column = $"c{counter++}";
                                            columns.Add($@"t.""Metadata"" #> '{{{subField.PathString}}}' as {field.Column}");
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                }

                if (columns.Count == 0)
                    return [];

                select = string.Join(',', columns);
            }

            static (string[], string) TryMetaSort(string field)
            {
                if (field.StartsWith("token.metadata.") && Regexes.FieldPath().IsMatch(field))
                {
                    var col = $@"t.""Metadata""#>'{{{field[15..].Replace('.', ',')}}}'";
                    return (new string[1] { col }, col);
                }
                return ([@"tb.""Id"""], @"tb.""Id""");
            }

            await using var db = await DataSource.OpenConnectionAsync();

            #region optimizations
            if (filter.balance?.Gt == "0" && filter.balance.Ne == null)
            {
                filter.balance.Gt = null;
                filter.balance.Ne = "0";
            }
            if (filter.token.contract?.Eq != null && filter.token.tokenId?.Eq != null && filter.token.id?.Eq == null)
            {
                var row = await db.QueryFirstOrDefaultAsync("""
                    SELECT "Id"
                    FROM "Tokens"
                    WHERE "ContractId" = @contractId
                    AND "TokenId" = @tokenId::numeric
                    LIMIT 1
                    """, new { contractId = filter.token.contract.Eq.Value, tokenId = filter.token.tokenId.Eq });

                if (row == null)
                    return [];

                filter.token.contract.Eq = null;
                filter.token.tokenId.Eq = null;

                filter.token.id ??= new();
                filter.token.id.Eq = (long)row.Id;
            }
            #endregion

            var sql = new SqlBuilder($@"
                SELECT {select} FROM ""TokenBalances"" as tb
                INNER JOIN ""Tokens"" AS t ON t.""Id"" = tb.""TokenId""")
                .FilterA(@"tb.""Id""", filter.id)
                .FilterA(@"tb.""AccountId""", filter.account)
                .FilterA(@"tb.""Balance""", filter.balance)
                .FilterA(@"tb.""FirstLevel""", filter.firstLevel)
                .FilterA(@"tb.""FirstLevel""", filter.firstTime)
                .FilterA(@"tb.""LastLevel""", filter.lastLevel)
                .FilterA(@"tb.""LastLevel""", filter.lastTime)
                .FilterA(@"tb.""IndexedAt""", filter.indexedAt)
                .FilterA(@"tb.""TokenId""", filter.token.id)
                .FilterA(@"tb.""ContractId""", filter.token.contract)
                .FilterA(@"t.""TokenId""", filter.token.tokenId)
                .FilterA(@"t.""Tags""", filter.token.standard)
                .FilterA(@"t.""Metadata""", filter.token.metadata)
                .Take(pagination, x => x switch
                {
                    "id" => ([@"tb.""Id"""], @"tb.""Id"""),
                    "balance" => ([@"tb.""Balance"""], @"tb.""Balance"""),
                    "balanceValue" => ([@"""BalanceValue""", @"tb.""Balance"""], @"""BalanceValue"""),
                    "transfersCount" => ([@"tb.""TransfersCount"""], @"tb.""TransfersCount"""),
                    "firstLevel" => ([@"tb.""Id"""], @"tb.""FirstLevel"""),
                    "lastLevel" => ([@"tb.""LastLevel"""], @"tb.""LastLevel"""),
                    "token.metadata" => ([@"t.""Metadata"""], @"t.""Metadata"""),
                    _ => TryMetaSort(x)
                }, @"tb.""Id""", 100);

            return (await db.QueryAsync(sql.Query, sql.Params)).Take(pagination.limit);
        }

        public async Task<int> GetTokenBalancesCount(TokenBalanceFilter filter)
        {
            var sql = new SqlBuilder(@"
                SELECT COUNT(*) FROM ""TokenBalances"" as tb
                INNER JOIN ""Tokens"" AS t ON t.""Id"" = tb.""TokenId""")
                .FilterA(@"tb.""Id""", filter.id)
                .FilterA(@"tb.""AccountId""", filter.account)
                .FilterA(@"tb.""Balance""", filter.balance)
                .FilterA(@"tb.""FirstLevel""", filter.firstLevel)
                .FilterA(@"tb.""FirstLevel""", filter.firstTime)
                .FilterA(@"tb.""LastLevel""", filter.lastLevel)
                .FilterA(@"tb.""LastLevel""", filter.lastTime)
                .FilterA(@"tb.""IndexedAt""", filter.indexedAt)
                .FilterA(@"tb.""TokenId""", filter.token.id)
                .FilterA(@"tb.""ContractId""", filter.token.contract)
                .FilterA(@"t.""TokenId""", filter.token.tokenId)
                .FilterA(@"t.""Tags""", filter.token.standard)
                .FilterA(@"t.""Metadata""", filter.token.metadata);

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<TokenBalance>> GetTokenBalances(TokenBalanceFilter filter, Pagination pagination)
        {
            var rows = await QueryTokenBalancesAsync(filter, pagination);
            return rows.Select(row => new TokenBalance
            {
                Id = row.Id,
                Account = Accounts.GetAlias(row.AccountId),
                Balance = row.Balance,
                BalanceValue = row.BalanceValue == BigInteger.Zero ? null : row.BalanceValue,
                FirstLevel = row.FirstLevel,
                FirstTime = Times[row.FirstLevel],
                LastLevel = row.LastLevel,
                LastTime = Times[row.LastLevel],
                TransfersCount = row.TransfersCount,
                Token = new TokenInfo
                {
                    Id = row.tId,
                    Contract = Accounts.GetAlias(row.tContractId),
                    TokenId = row.tTokenId,
                    Standard = TokenStandards.ToString(row.tTags),
                    TotalSupply = row.tTotalSupply,
                    Metadata = (RawJson?)row.tMetadata
                }
            });
        }

        public async Task<object?[][]> GetTokenBalances(TokenBalanceFilter filter, Pagination pagination, List<SelectionField> fields)
        {
            var rows = await QueryTokenBalancesAsync(filter, pagination, fields);

            var result = new object?[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object?[fields.Count];

            for (int i = 0, j = 0; i < fields.Count; j = 0, i++)
            {
                switch (fields[i].Full)
                {
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "account":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.AccountId);
                        break;
                    case "account.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.AccountId).Name;
                        break;
                    case "account.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.AccountId).Address;
                        break;
                    case "balance":
                        foreach (var row in rows)
                            result[j++][i] = row.Balance;
                        break;
                    case "balanceValue":
                        foreach (var row in rows)
                            result[j++][i] = row.BalanceValue == BigInteger.Zero ? null : row.BalanceValue;
                        break;
                    case "firstLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.FirstLevel;
                        break;
                    case "firstTime":
                        foreach (var row in rows)
                            result[j++][i] = Times[row.FirstLevel];
                        break;
                    case "lastLevel":
                        foreach (var row in rows)
                            result[j++][i] = row.LastLevel;
                        break;
                    case "lastTime":
                        foreach (var row in rows)
                            result[j++][i] = Times[row.LastLevel];
                        break;
                    case "transfersCount":
                        foreach (var row in rows)
                            result[j++][i] = row.TransfersCount;
                        break;
                    case "token":
                        foreach (var row in rows)
                            result[j++][i] = new TokenInfo
                            {
                                Id = row.tId,
                                Contract = Accounts.GetAlias(row.tContractId),
                                TokenId = row.tTokenId,
                                Standard = TokenStandards.ToString(row.tTags),
                                TotalSupply = row.tTotalSupply,
                                Metadata = (RawJson?)row.tMetadata
                            };
                        break;
                    case "token.id":
                        foreach (var row in rows)
                            result[j++][i] = row.tId;
                        break;
                    case "token.contract":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.tContractId);
                        break;
                    case "token.contract.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.tContractId).Alias;
                        break;
                    case "token.contract.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.tContractId).Address;
                        break;
                    case "token.tokenId":
                        foreach (var row in rows)
                            result[j++][i] = row.tTokenId;
                        break;
                    case "token.standard":
                        foreach (var row in rows)
                            result[j++][i] = TokenStandards.ToString(row.tTags);
                        break;
                    case "token.totalSupply":
                        foreach (var row in rows)
                            result[j++][i] = row.tTotalSupply;
                        break;
                    case "token.metadata":
                        foreach (var row in rows)
                            result[j++][i] = (RawJson?)row.tMetadata;
                        break;
                    default:
                        if (fields[i].Full.StartsWith("token.metadata."))
                            foreach (var row in rows)
                                result[j++][i] = (RawJson?)((row as IDictionary<string, object>)![fields[i].Column!] as string);
                        break;
                }
            }

            return result;
        }
        #endregion

        #region token transfers
        async Task<IEnumerable<dynamic>> QueryTokenTransfersAsync(TokenTransferFilter filter, Pagination pagination, List<SelectionField>? fields = null)
        {
            var select = @"
                tr.""Id"",
                tr.""Level"",
                tr.""FromId"",
                tr.""ToId"",
                tr.""Amount"",
                tr.""TransactionId"",
                tr.""OriginationId"",
                tr.""MigrationId"",
                tr.""TokenId"" as ""tId"",
                tr.""ContractId"" as ""tContractId"",
                t.""TokenId"" as ""tTokenId"",
                t.""Tags"" as ""tTags"",
                t.""TotalSupply"" as ""tTotalSupply"",
                t.""Metadata"" as ""tMetadata""";

            if (fields != null)
            {
                var counter = 0;
                var columns = new HashSet<string>(fields.Count);
                foreach (var field in fields)
                {
                    switch (field.Field)
                    {
                        case "id": columns.Add(@"tr.""Id"""); break;
                        case "level": columns.Add(@"tr.""Level"""); break;
                        case "timestamp": columns.Add(@"tr.""Level"""); break;
                        case "from": columns.Add(@"tr.""FromId"""); break;
                        case "to": columns.Add(@"tr.""ToId"""); break;
                        case "amount": columns.Add(@"tr.""Amount"""); break;
                        case "transactionId": columns.Add(@"tr.""TransactionId"""); break;
                        case "originationId": columns.Add(@"tr.""OriginationId"""); break;
                        case "migrationId": columns.Add(@"tr.""MigrationId"""); break;
                        case "token":
                            if (field.Path == null)
                            {
                                columns.Add(@"tr.""TokenId"" as ""tId""");
                                columns.Add(@"tr.""ContractId"" as ""tContractId""");
                                columns.Add(@"t.""TokenId"" as ""tTokenId""");
                                columns.Add(@"t.""Tags"" as ""tTags""");
                                columns.Add(@"t.""TotalSupply"" as ""tTotalSupply""");
                                columns.Add(@"t.""Metadata"" as ""tMetadata""");
                            }
                            else
                            {
                                var subField = field.SubField()!;
                                switch (subField.Field)
                                {
                                    case "id": columns.Add(@"tr.""TokenId"" as ""tId"""); break;
                                    case "contract": columns.Add(@"tr.""ContractId"" as ""tContractId"""); break;
                                    case "tokenId": columns.Add(@"t.""TokenId"" as ""tTokenId"""); break;
                                    case "standard": columns.Add(@"t.""Tags"" as ""tTags"""); break;
                                    case "totalSupply": columns.Add(@"t.""TotalSupply"" as ""tTotalSupply"""); break;
                                    case "metadata":
                                        if (subField.Path == null)
                                        {
                                            columns.Add(@"t.""Metadata"" as ""tMetadata""");
                                        }
                                        else
                                        {
                                            field.Column = $"c{counter++}";
                                            columns.Add($@"t.""Metadata"" #> '{{{subField.PathString}}}' as {field.Column}");
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                }

                if (columns.Count == 0)
                    return [];

                select = string.Join(',', columns);
            }

            static (string, string) TryMetaSort(string field)
            {
                if (field.StartsWith("token.metadata.") && Regexes.FieldPath().IsMatch(field))
                {
                    var col = $@"t.""Metadata""#>'{{{field[15..].Replace('.', ',')}}}'";
                    return (col, col);
                }
                return (@"tr.""Id""", @"tr.""Id""");
            }

            var sql = new SqlBuilder($@"
                SELECT {select} FROM ""TokenTransfers"" as tr
                INNER JOIN ""Tokens"" AS t ON t.""Id"" = tr.""TokenId""")
                .FilterA(filter.or)
                .FilterA(@"tr.""Id""", filter.id)
                .FilterA(@"tr.""Level""", filter.level)
                .FilterA(@"tr.""Level""", filter.timestamp)
                .FilterA(@"tr.""IndexedAt""", filter.indexedAt)
                .FilterA(filter.anyof, x => x == "from" ? @"tr.""FromId""" : @"tr.""ToId""")
                .FilterA(@"tr.""FromId""", filter.from)
                .FilterA(@"tr.""ToId""", filter.to)
                .FilterA(@"tr.""Amount""", filter.amount)
                .FilterA(@"tr.""TransactionId""", filter.transactionId)
                .FilterA(@"tr.""OriginationId""", filter.originationId)
                .FilterA(@"tr.""MigrationId""", filter.migrationId)
                .FilterA(@"tr.""TokenId""", filter.token.id)
                .FilterA(@"tr.""ContractId""", filter.token.contract)
                .FilterA(@"t.""TokenId""", filter.token.tokenId)
                .FilterA(@"t.""Tags""", filter.token.standard)
                .FilterA(@"t.""Metadata""", filter.token.metadata)
                .Take(pagination, x => x switch
                {
                    "id" => (@"tr.""Id""", @"tr.""Id"""),
                    "level" => (@"tr.""Level""", @"tr.""Level"""),
                    "amount" => (@"tr.""Amount""", @"tr.""Amount"""),
                    "token.metadata" => (@"t.""Metadata""", @"t.""Metadata"""),
                    _ => TryMetaSort(x)
                }, @"tr.""Id""", 100);

            await using var db = await DataSource.OpenConnectionAsync();
            return (await db.QueryAsync(sql.Query, sql.Params)).Take(pagination.limit);
        }

        public async Task<IEnumerable<Activity>> GetTokenTransfersActivity(
            List<RawAccount> accounts,
            ActivityRole roles,
            TimestampParameter? timestamp,
            Pagination pagination)
        {
            List<int>? fromIds = null;
            List<int>? toIds = null;
            List<int>? contractIds = null;

            foreach (var account in accounts)
            {
                if (account.TokenTransfersCount != 0)
                {
                    if ((roles & ActivityRole.Sender) != 0)
                    {
                        fromIds ??= new(accounts.Count);
                        fromIds.Add(account.Id);
                    }

                    if ((roles & ActivityRole.Target) != 0)
                    {
                        toIds ??= new(accounts.Count);
                        toIds.Add(account.Id);
                    }
                }

                if (account is RawContract contract && contract.TokensCount != 0)
                {
                    if ((roles & ActivityRole.Mention) != 0)
                    {
                        contractIds ??= new(accounts.Count);
                        contractIds.Add(account.Id);
                    }
                }
            }

            if (fromIds == null && toIds == null && contractIds == null)
                return [];

            var or = new OrParameter(
                (@"tr.""FromId""", fromIds),
                (@"tr.""ToId""", toIds),
                (@"tr.""ContractId""", contractIds));

            var rows = await QueryTokenTransfersAsync(new() { or = or, timestamp = timestamp }, pagination);
            return rows.Select(row => new TokenTransferActivity
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = Times[row.Level],
                From = row.FromId == null ? null : Accounts.GetAlias(row.FromId),
                To = row.ToId == null ? null : Accounts.GetAlias(row.ToId),
                Amount = row.Amount,
                TransactionId = row.TransactionId,
                OriginationId = row.OriginationId,
                MigrationId = row.MigrationId,
                Token = new TokenInfo
                {
                    Id = row.tId,
                    Contract = Accounts.GetAlias(row.tContractId),
                    TokenId = row.tTokenId,
                    Standard = TokenStandards.ToString(row.tTags),
                    TotalSupply = row.tTotalSupply,
                    Metadata = (RawJson?)row.tMetadata
                }
            });
        }

        public async Task<int> GetTokenTransfersCount(TokenTransferFilter filter)
        {
            var sql = new SqlBuilder(@"
                SELECT COUNT(*) FROM ""TokenTransfers"" as tr
                INNER JOIN ""Tokens"" AS t ON t.""Id"" = tr.""TokenId""")
                .FilterA(filter.or)
                .FilterA(@"tr.""Id""", filter.id)
                .FilterA(@"tr.""Level""", filter.level)
                .FilterA(@"tr.""Level""", filter.timestamp)
                .FilterA(@"tr.""IndexedAt""", filter.indexedAt)
                .FilterA(filter.anyof, x => x == "from" ? @"tr.""FromId""" : @"tr.""ToId""")
                .FilterA(@"tr.""FromId""", filter.from)
                .FilterA(@"tr.""ToId""", filter.to)
                .FilterA(@"tr.""Amount""", filter.amount)
                .FilterA(@"tr.""TransactionId""", filter.transactionId)
                .FilterA(@"tr.""OriginationId""", filter.originationId)
                .FilterA(@"tr.""MigrationId""", filter.migrationId)
                .FilterA(@"tr.""TokenId""", filter.token.id)
                .FilterA(@"tr.""ContractId""", filter.token.contract)
                .FilterA(@"t.""TokenId""", filter.token.tokenId)
                .FilterA(@"t.""Tags""", filter.token.standard)
                .FilterA(@"t.""Metadata""", filter.token.metadata);

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql.Query, sql.Params);
        }

        public async Task<IEnumerable<TokenTransfer>> GetTokenTransfers(TokenTransferFilter filter, Pagination pagination)
        {
            var rows = await QueryTokenTransfersAsync(filter, pagination);
            return rows.Select(row => new TokenTransfer
            {
                Id = row.Id,
                Level = row.Level,
                Timestamp = Times[row.Level],
                From = row.FromId == null ? null : Accounts.GetAlias(row.FromId),
                To = row.ToId == null ? null : Accounts.GetAlias(row.ToId),
                Amount = row.Amount,
                TransactionId = row.TransactionId,
                OriginationId = row.OriginationId,
                MigrationId = row.MigrationId,
                Token = new TokenInfo
                {
                    Id = row.tId,
                    Contract = Accounts.GetAlias(row.tContractId),
                    TokenId = row.tTokenId,
                    Standard = TokenStandards.ToString(row.tTags),
                    TotalSupply = row.tTotalSupply,
                    Metadata = (RawJson?)row.tMetadata
                }
            });
        }

        public async Task<object?[][]> GetTokenTransfers(TokenTransferFilter filter, Pagination pagination, List<SelectionField> fields)
        {
            var rows = await QueryTokenTransfersAsync(filter, pagination, fields);

            var result = new object?[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object?[fields.Count];

            for (int i = 0, j = 0; i < fields.Count; j = 0, i++)
            {
                switch (fields[i].Full)
                {
                    case "id":
                        foreach (var row in rows)
                            result[j++][i] = row.Id;
                        break;
                    case "level":
                        foreach (var row in rows)
                            result[j++][i] = row.Level;
                        break;
                    case "timestamp":
                        foreach (var row in rows)
                            result[j++][i] = Times[row.Level];
                        break;
                    case "from":
                        foreach (var row in rows)
                            result[j++][i] = row.FromId == null ? null : Accounts.GetAlias(row.FromId);
                        break;
                    case "from.alias":
                        foreach (var row in rows)
                            result[j++][i] = row.FromId == null ? null : Accounts.GetAlias(row.FromId).Name;
                        break;
                    case "from.address":
                        foreach (var row in rows)
                            result[j++][i] = row.FromId == null ? null : Accounts.GetAlias(row.FromId).Address;
                        break;
                    case "to":
                        foreach (var row in rows)
                            result[j++][i] = row.ToId == null ? null : Accounts.GetAlias(row.ToId);
                        break;
                    case "to.alias":
                        foreach (var row in rows)
                            result[j++][i] = row.ToId == null ? null : Accounts.GetAlias(row.ToId).Name;
                        break;
                    case "to.address":
                        foreach (var row in rows)
                            result[j++][i] = row.ToId == null ? null : Accounts.GetAlias(row.ToId).Address;
                        break;
                    case "amount":
                        foreach (var row in rows)
                            result[j++][i] = row.Amount;
                        break;
                    case "transactionId":
                        foreach (var row in rows)
                            result[j++][i] = row.TransactionId;
                        break;
                    case "originationId":
                        foreach (var row in rows)
                            result[j++][i] = row.OriginationId;
                        break;
                    case "migrationId":
                        foreach (var row in rows)
                            result[j++][i] = row.MigrationId;
                        break;
                    case "token":
                        foreach (var row in rows)
                            result[j++][i] = new TokenInfo
                            {
                                Id = row.tId,
                                Contract = Accounts.GetAlias(row.tContractId),
                                TokenId = row.tTokenId,
                                Standard = TokenStandards.ToString(row.tTags),
                                TotalSupply = row.tTotalSupply,
                                Metadata = (RawJson?)row.tMetadata
                            };
                        break;
                    case "token.id":
                        foreach (var row in rows)
                            result[j++][i] = row.tId;
                        break;
                    case "token.contract":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.tContractId);
                        break;
                    case "token.contract.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.tContractId).Alias;
                        break;
                    case "token.contract.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.tContractId).Address;
                        break;
                    case "token.tokenId":
                        foreach (var row in rows)
                            result[j++][i] = row.tTokenId;
                        break;
                    case "token.standard":
                        foreach (var row in rows)
                            result[j++][i] = TokenStandards.ToString(row.tTags);
                        break;
                    case "token.totalSupply":
                        foreach (var row in rows)
                            result[j++][i] = row.tTotalSupply;
                        break;
                    case "token.metadata":
                        foreach (var row in rows)
                            result[j++][i] = (RawJson?)row.tMetadata;
                        break;
                    default:
                        if (fields[i].Full.StartsWith("token.metadata."))
                            foreach (var row in rows)
                                result[j++][i] = (RawJson?)((row as IDictionary<string, object>)![fields[i].Column!] as string);
                        break;
                }
            }

            return result;
        }
        #endregion

        #region historical balances
        async Task<IEnumerable<dynamic>> QueryHistoricalTokenBalancesAsync(int level, TokenBalanceShortFilter filter, Pagination pagination, List<SelectionField>? fields = null)
        {
            var select = @"
                tb.""AccountId"",
                tb.""Balance"",
                tb.""TokenId"" as ""tId"",
                t.""ContractId"" as ""tContractId"",
                t.""TokenId"" as ""tTokenId"",
                t.""Tags"" as ""tTags"",
                t.""Metadata"" as ""tMetadata""";
            if (fields != null)
            {
                var counter = 0;
                var columns = new HashSet<string>(fields.Count);
                foreach (var field in fields)
                {
                    switch (field.Field)
                    {
                        case "account": columns.Add(@"tb.""AccountId"""); break;
                        case "balance": columns.Add(@"tb.""Balance"""); break;
                        case "token":
                            if (field.Path == null)
                            {
                                columns.Add(@"tb.""TokenId"" as ""tId""");
                                columns.Add(@"t.""ContractId"" as ""tContractId""");
                                columns.Add(@"t.""TokenId"" as ""tTokenId""");
                                columns.Add(@"t.""Tags"" as ""tTags""");
                                columns.Add(@"t.""Metadata"" as ""tMetadata""");
                            }
                            else
                            {
                                var subField = field.SubField()!;
                                switch (subField.Field)
                                {
                                    case "id": columns.Add(@"tb.""TokenId"" as ""tId"""); break;
                                    case "contract": columns.Add(@"t.""ContractId"" as ""tContractId"""); break;
                                    case "tokenId": columns.Add(@"t.""TokenId"" as ""tTokenId"""); break;
                                    case "standard": columns.Add(@"t.""Tags"" as ""tTags"""); break;
                                    case "metadata":
                                        if (subField.Path == null)
                                        {
                                            columns.Add(@"t.""Metadata"" as ""tMetadata""");
                                        }
                                        else
                                        {
                                            field.Column = $"c{counter++}";
                                            columns.Add($@"t.""Metadata"" #> '{{{subField.PathString}}}' as {field.Column}");
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                }

                if (columns.Count == 0)
                    return [];

                select = string.Join(',', columns);
            }

            static (string, string) TryMetaSort(string field)
            {
                if (field.StartsWith("token.metadata.") && Regexes.FieldPath().IsMatch(field))
                {
                    var col = $@"t.""Metadata""#>'{{{field[15..].Replace('.', ',')}}}'";
                    return (col, col);
                }
                return (@"tb.""Id""", @"tb.""Id""");
            }

            var sql = new SqlBuilder()
                .Append($@"SELECT {select} FROM (")
                    .Append(@"SELECT ROW_NUMBER() over (ORDER BY ""TokenId"", ""AccountId"") as ""Id"", ""TokenId"", ""AccountId"", SUM(""Amount"") AS ""Balance"" FROM (")
                        
                        .Append(@"SELECT tr.""TokenId"", tr.""FromId"" AS ""AccountId"", -tr.""Amount"" AS ""Amount"" FROM ""TokenTransfers"" as tr")
                        .Append(@"INNER JOIN ""Tokens"" AS t ON t.""Id"" = tr.""TokenId""")
                        .Filter($@"tr.""Level"" <= {level}")
                        .Filter($@"tr.""FromId"" IS NOT NULL")
                        .FilterA(@"tr.""FromId""", filter.account)
                        .FilterA(@"tr.""TokenId""", filter.token.id)
                        .FilterA(@"t.""ContractId""", filter.token.contract)
                        .FilterA(@"t.""TokenId""", filter.token.tokenId)
                        .FilterA(@"t.""Tags""", filter.token.standard)
                        .FilterA(@"t.""Metadata""", filter.token.metadata)
                        .ResetFilters()

                        .Append("UNION ALL")

                        .Append(@"SELECT tr.""TokenId"", tr.""ToId"" AS ""AccountId"", tr.""Amount"" AS ""Amount"" FROM ""TokenTransfers"" as tr")
                        .Append(@"INNER JOIN ""Tokens"" AS t ON t.""Id"" = tr.""TokenId""")
                        .Filter($@"tr.""Level"" <= {level}")
                        .Filter($@"tr.""ToId"" IS NOT NULL")
                        .FilterA(@"tr.""ToId""", filter.account)
                        .FilterA(@"tr.""TokenId""", filter.token.id)
                        .FilterA(@"t.""ContractId""", filter.token.contract)
                        .FilterA(@"t.""TokenId""", filter.token.tokenId)
                        .FilterA(@"t.""Tags""", filter.token.standard)
                        .FilterA(@"t.""Metadata""", filter.token.metadata)
                        .ResetFilters()

                    .Append(") as tb")
                    .Append(@"GROUP BY tb.""TokenId"", tb.""AccountId""")
                .Append(") as tb")
                .Append(@"INNER JOIN ""Tokens"" AS t ON t.""Id"" = tb.""TokenId""")
                .FilterA(@"tb.""Balance""", filter.balance)
                .Take(pagination, x => x switch
                {
                    "balance" => (@"""Balance""", @"""Balance"""),
                    "token.metadata" => (@"t.""Metadata""", @"t.""Metadata"""),
                    _ => TryMetaSort(x)
                }, @"tb.""Id""", 100);

            await using var db = await DataSource.OpenConnectionAsync();
            return (await db.QueryAsync(sql.Query, sql.Params)).Take(pagination.limit);
        }

        public async Task<IEnumerable<TokenBalanceShort>> GetHistoricalTokenBalances(int level, TokenBalanceShortFilter filter, Pagination pagination)
        {
            var rows = await QueryHistoricalTokenBalancesAsync(level, filter, pagination);
            return rows.Select(row => new TokenBalanceShort
            {
                Account = Accounts.GetAlias(row.AccountId),
                Balance = row.Balance,
                Token = new TokenInfoShort
                {
                    Id = row.tId,
                    Contract = Accounts.GetAlias(row.tContractId),
                    TokenId = row.tTokenId,
                    Standard = TokenStandards.ToString(row.tTags),
                    Metadata = (RawJson?)row.tMetadata
                }
            });
        }

        public async Task<object?[][]> GetHistoricalTokenBalances(int level, TokenBalanceShortFilter filter, Pagination pagination, List<SelectionField> fields)
        {
            var rows = await QueryHistoricalTokenBalancesAsync(level, filter, pagination, fields);

            var result = new object?[rows.Count()][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new object?[fields.Count];

            for (int i = 0, j = 0; i < fields.Count; j = 0, i++)
            {
                switch (fields[i].Full)
                {
                    case "account":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.AccountId);
                        break;
                    case "account.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.AccountId).Name;
                        break;
                    case "account.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.AccountId).Address;
                        break;
                    case "balance":
                        foreach (var row in rows)
                            result[j++][i] = row.Balance;
                        break;
                    case "token":
                        foreach (var row in rows)
                            result[j++][i] = new TokenInfoShort
                            {
                                Id = row.tId,
                                Contract = Accounts.GetAlias(row.tContractId),
                                TokenId = row.tTokenId,
                                Standard = TokenStandards.ToString(row.tTags),
                                Metadata = (RawJson?)row.tMetadata
                            };
                        break;
                    case "token.id":
                        foreach (var row in rows)
                            result[j++][i] = row.tId;
                        break;
                    case "token.contract":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.tContractId);
                        break;
                    case "token.contract.alias":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.tContractId).Alias;
                        break;
                    case "token.contract.address":
                        foreach (var row in rows)
                            result[j++][i] = Accounts.GetAlias(row.tContractId).Address;
                        break;
                    case "token.tokenId":
                        foreach (var row in rows)
                            result[j++][i] = row.tTokenId;
                        break;
                    case "token.standard":
                        foreach (var row in rows)
                            result[j++][i] = TokenStandards.ToString(row.tTags);
                        break;
                    case "token.metadata":
                        foreach (var row in rows)
                            result[j++][i] = (RawJson?)row.tMetadata;
                        break;
                    default:
                        if (fields[i].Full.StartsWith("token.metadata."))
                            foreach (var row in rows)
                                result[j++][i] = (RawJson?)((row as IDictionary<string, object>)![fields[i].Column!] as string);
                        break;
                }
            }

            return result;
        }
        #endregion
    }
}
