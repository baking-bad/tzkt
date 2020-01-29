using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;

using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public class ReportRepository : DbConnection
    {
        readonly AccountsCache Accounts;

        public ReportRepository(AccountsCache accounts, IConfiguration config) : base(config)
        {
            Accounts = accounts;
        }

        public async Task Test(string address)
        {
            var account = await Accounts.GetAsync(address);
            if (account == null) return;

            var sql = new StringBuilder();

            if (account.DelegationsCount > 0) UnionDelegations(sql);
            if (account.OriginationsCount > 0) UnionOriginations(sql);
            if (account.TransactionsCount > 0) UnionTransactions(sql);
            if (account.RevealsCount > 0) UnionReveals(sql);
            if (account.SystemOpsCount > 0) UnionSystem(sql);

            if (account is RawUser user)
            {
                if (user.Activated == true) UnionActivations(sql);
            }

            if (account is RawDelegate delegat)
            {
                if (delegat.BlocksCount > 0) UnionBaking(sql);
                if (delegat.EndorsementsCount > 0) UnionEndorsements(sql);
                if (delegat.DoubleBakingCount > 0) UnionDoubleBaking(sql);
                if (delegat.DoubleEndorsingCount > 0) UnionDoubleEndorsing(sql);
                if (delegat.NonceRevelationsCount > 0) UnionNonceRevelations(sql);
                if (delegat.RevelationPenaltiesCount > 0) UnionRevelationPenalties(sql);
            }

            if (sql.Length == 0) return;

            sql.AppendLine(@"ORDER BY ""Id""");
            sql.AppendLine(@"LIMIT @limit");

            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.ToString(), new
            {
                account = account.Id,
                from = DateTime.MinValue,
                to = DateTime.MaxValue,
                limit = 10000000
            });

            var balance = 0m;

            #region write rows
            foreach (var row in rows)
            {
                if (row.Reward + row.Loss + row.Received + row.Sent + row.Fee == 0)
                    throw new Exception($"{account.Address}: {row.Id} is empty");

                balance += row.Reward / 1000000m;
                balance -= row.Loss / 1000000m;
                balance += row.Received / 1000000m;
                balance -= row.Sent / 1000000m;
                balance -= row.Fee / 1000000m;
            }
            #endregion

            if (balance * 1000000 != account.Balance)
            {
                Console.WriteLine($"{account.Address}: {account.Balance} != {balance}");
                throw new Exception($"{account.Address}: {account.Balance} != {balance}");
            }
        }

        public async Task Write(StreamWriter csv, string address, DateTime from, DateTime to, int limit)
        {
            var account = await Accounts.GetAsync(address);
            if (account == null) return;

            var sql = new StringBuilder();

            if (account.DelegationsCount > 0) UnionDelegations(sql);
            if (account.OriginationsCount > 0) UnionOriginations(sql);
            if (account.TransactionsCount > 0) UnionTransactions(sql);
            if (account.RevealsCount > 0) UnionReveals(sql);
            if (account.SystemOpsCount > 0) UnionSystem(sql);

            if (account is RawUser user)
            {
                if (user.Activated == true) UnionActivations(sql);
            }

            if (account is RawDelegate delegat)
            {
                if (delegat.BlocksCount > 0) UnionBaking(sql);
                if (delegat.EndorsementsCount > 0) UnionEndorsements(sql);
                if (delegat.DoubleBakingCount > 0) UnionDoubleBaking(sql);
                if (delegat.DoubleEndorsingCount > 0) UnionDoubleEndorsing(sql);
                if (delegat.NonceRevelationsCount > 0) UnionNonceRevelations(sql);
                if (delegat.RevelationPenaltiesCount > 0) UnionRevelationPenalties(sql);
            }

            if (sql.Length == 0) return;

            sql.AppendLine(@"ORDER BY ""Id""");
            sql.AppendLine(@"LIMIT @limit");
            
            using var db = GetConnection();
            var rows = await db.QueryAsync(sql.ToString(), new { account = account.Id, from, to, limit });
            
            #region write header
            csv.Write("Block level;");
            csv.Write("Datetime;");
            csv.Write("Operation;");
            if (account is RawDelegate)
            {
                csv.Write("Reward;");
                csv.Write("Loss;");
            }
            csv.Write("Received;");
            csv.Write("From address;");
            csv.Write("Sent;");
            csv.Write("Fee;");
            csv.Write("To address;");
            csv.Write("Explorer link;\n");
            #endregion

            #region write rows
            foreach (var row in rows)
            {
                csv.Write(row.Level);
                csv.Write(";");
                csv.Write(row.Timestamp);
                csv.Write(";");
                csv.Write(Operations[row.Type]);
                csv.Write(";");
                if (account is RawDelegate)
                {
                    csv.Write(row.Reward == 0 ? "" : row.Reward / 1_000_000m);
                    csv.Write(";");
                    csv.Write(row.Loss == 0 ? "" : -row.Loss / 1_000_000m);
                    csv.Write(";");
                }
                csv.Write(row.Received == 0 ? "" : row.Received / 1_000_000m);
                csv.Write(";");
                csv.Write(row.From == null ? "" : Accounts.Get(row.From).Address);
                csv.Write(";");
                csv.Write(row.Sent == 0 ? "" : -row.Sent / 1_000_000m);
                csv.Write(";");
                csv.Write(row.Fee == 0 ? "" : -row.Fee / 1_000_000m);
                csv.Write(";");
                csv.Write(row.To == null ? "" : Accounts.Get(row.To).Address);
                csv.Write(";");
                csv.Write(row.Nonce != null
                    ? $"https://tzkt.io/{row.OpHash}/{row.Counter}/{row.Nonce}"
                    : row.Counter != null
                        ? $"https://tzkt.io/{row.OpHash}/{row.Counter}"
                        : row.OpHash != null
                            ? $"https://tzkt.io/{row.OpHash}"
                            : "");

                csv.Write("\n");
            }
            #endregion

            csv.Flush();
        }

        void UnionBaking(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"0 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""Hash"" as ""OpHash"", ");
            sql.Append(@"null::integer as ""Counter"", ");
            sql.Append(@"null::integer as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"(""Reward"" + ""Fees"") as ""Reward"", ");
            sql.Append(@"0 as ""Loss"", ");
            sql.Append(@"0 as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"0 as ""Sent"", ");
            sql.Append(@"0 as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""Blocks"" ");
            sql.Append(@"WHERE ""BakerId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND (""Reward"" > 0 OR ""Fees"" > 0) ");

            sql.AppendLine();
        }

        void UnionEndorsements(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"1 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"null::integer as ""Counter"", ");
            sql.Append(@"null::integer as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"""Reward"" as ""Reward"", ");
            sql.Append(@"0 as ""Loss"", ");
            sql.Append(@"0 as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"0 as ""Sent"", ");
            sql.Append(@"0 as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""EndorsementOps"" ");
            sql.Append(@"WHERE ""DelegateId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""Reward"" > 0 ");

            sql.AppendLine();
        }

        void UnionActivations(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"2 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"null::integer as ""Counter"", ");
            sql.Append(@"null::integer as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"0 as ""Reward"", ");
            sql.Append(@"0 as ""Loss"", ");
            sql.Append(@"""Balance"" as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"0 as ""Sent"", ");
            sql.Append(@"0 as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""ActivationOps"" ");
            sql.Append(@"WHERE ""AccountId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""Balance"" > 0 ");

            sql.AppendLine();
        }

        void UnionDoubleBaking(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region accuser
            sql.Append(@"3 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"null::integer as ""Counter"", ");
            sql.Append(@"null::integer as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"""AccuserReward"" as ""Reward"", ");
            sql.Append(@"0 as ""Loss"", ");
            sql.Append(@"0 as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"0 as ""Sent"", ");
            sql.Append(@"0 as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""DoubleBakingOps"" ");
            sql.Append(@"WHERE ""AccuserId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region offender
            sql.Append(@"3 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"null::integer as ""Counter"", ");
            sql.Append(@"null::integer as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"0 as ""Reward"", ");
            sql.Append(@"(""OffenderLostDeposit"" + ""OffenderLostReward"" + ""OffenderLostFee"") as ""Loss"", ");
            sql.Append(@"0 as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"0 as ""Sent"", ");
            sql.Append(@"0 as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""DoubleBakingOps"" ");
            sql.Append(@"WHERE ""OffenderId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");

            sql.AppendLine();
            #endregion
        }

        void UnionDoubleEndorsing(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region accuser
            sql.Append(@"4 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"null::integer as ""Counter"", ");
            sql.Append(@"null::integer as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"""AccuserReward"" as ""Reward"", ");
            sql.Append(@"0 as ""Loss"", ");
            sql.Append(@"0 as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"0 as ""Sent"", ");
            sql.Append(@"0 as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""DoubleEndorsingOps"" ");
            sql.Append(@"WHERE ""AccuserId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region offender
            sql.Append(@"4 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"null::integer as ""Counter"", ");
            sql.Append(@"null::integer as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"0 as ""Reward"", ");
            sql.Append(@"(""OffenderLostDeposit"" + ""OffenderLostReward"" + ""OffenderLostFee"") as ""Loss"", ");
            sql.Append(@"0 as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"0 as ""Sent"", ");
            sql.Append(@"0 as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""DoubleEndorsingOps"" ");
            sql.Append(@"WHERE ""OffenderId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");

            sql.AppendLine();
            #endregion
        }

        void UnionNonceRevelations(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"5 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"null::integer as ""Counter"", ");
            sql.Append(@"null::integer as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"125000 as ""Reward"", "); // TODO: get reward amount from protocol constants
            sql.Append(@"0 as ""Loss"", ");
            sql.Append(@"0 as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"0 as ""Sent"", ");
            sql.Append(@"0 as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""NonceRevelationOps"" ");
            sql.Append(@"WHERE ""BakerId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");

            sql.AppendLine();
        }

        void UnionDelegations(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"6 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"""Counter"" as ""Counter"", ");
            sql.Append(@"""Nonce"" as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"0 as ""Reward"", ");
            sql.Append(@"0 as ""Loss"", ");
            sql.Append(@"0 as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"0 as ""Sent"", ");
            sql.Append(@"""BakerFee"" as ""Fee"", ");
            sql.Append(@"""DelegateId"" as ""To"" ");

            sql.Append(@"FROM ""DelegationOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account AND ""Nonce"" IS NULL ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""BakerFee"" > 0 ");

            sql.AppendLine();
        }

        void UnionOriginations(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region originated
            sql.Append(@"7 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"""Counter"" as ""Counter"", ");
            sql.Append(@"""Nonce"" as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"0 as ""Reward"", ");
            sql.Append(@"0 as ""Loss"", ");
            sql.Append(@"""Balance"" as ""Received"", ");
            sql.Append(@"""SenderId"" as ""From"", ");
            sql.Append(@"0 as ""Sent"", ");
            sql.Append(@"0 as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""OriginationOps"" ");
            sql.Append(@"WHERE ""ContractId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""Balance"" > 0 ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region sent
            sql.Append(@"7 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"""Counter"" as ""Counter"", ");
            sql.Append(@"""Nonce"" as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"0 as ""Reward"", ");
            sql.Append(@"0 as ""Loss"", ");
            sql.Append(@"0 as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"""Balance"" as ""Sent"", ");
            sql.Append(@"CASE WHEN ""Nonce"" is NULL THEN (""BakerFee"" + COALESCE(""StorageFee"", 0) + COALESCE(""AllocationFee"", 0)) ELSE 0 END as ""Fee"", ");
            sql.Append(@"""ContractId"" as ""To"" ");

            sql.Append(@"FROM ""OriginationOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""Status"" = 1 ");
            sql.Append(@"AND (""Balance"" > 0 OR CASE WHEN ""Nonce"" is NULL THEN (""BakerFee"" + COALESCE(""StorageFee"", 0) + COALESCE(""AllocationFee"", 0)) ELSE 0 END > 0) ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region internal
            sql.Append(@"7 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"""Counter"" as ""Counter"", ");
            sql.Append(@"""Nonce"" as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"0 as ""Reward"", ");
            sql.Append(@"0 as ""Loss"", ");
            sql.Append(@"0 as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"0 as ""Sent"", ");
            sql.Append(@"(COALESCE(""StorageFee"", 0) + COALESCE(""AllocationFee"", 0)) as ""Fee"", ");
            sql.Append(@"""ContractId"" as ""To"" ");

            sql.Append(@"FROM ""OriginationOps"" ");
            sql.Append(@"WHERE ""OriginalSenderId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""Status"" = 1 ");
            sql.Append(@"AND (COALESCE(""StorageFee"", 0) + COALESCE(""AllocationFee"", 0)) > 0 ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region failed
            sql.Append(@"7 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"""Counter"" as ""Counter"", ");
            sql.Append(@"""Nonce"" as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"0 as ""Reward"", ");
            sql.Append(@"0 as ""Loss"", ");
            sql.Append(@"0 as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"0 as ""Sent"", ");
            sql.Append(@"""BakerFee"" as ""Fee"", ");
            sql.Append(@"""ContractId"" as ""To"" ");

            sql.Append(@"FROM ""OriginationOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""Status"" != 1 AND ""Nonce"" IS NULL ");
            sql.Append(@"AND ""BakerFee"" > 0 ");

            sql.AppendLine();
            #endregion
        }

        void UnionTransactions(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            #region received
            sql.Append(@"8 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"""Counter"" as ""Counter"", ");
            sql.Append(@"""Nonce"" as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"0 as ""Reward"", ");
            sql.Append(@"0 as ""Loss"", ");
            sql.Append(@"""Amount"" as ""Received"", ");
            sql.Append(@"""SenderId"" as ""From"", ");
            sql.Append(@"0 as ""Sent"", ");
            sql.Append(@"0 as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""TransactionOps"" ");
            sql.Append(@"WHERE ""TargetId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""Status"" = 1 ");
            sql.Append(@"AND ""Amount"" > 0 ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region sent
            sql.Append(@"8 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"""Counter"" as ""Counter"", ");
            sql.Append(@"""Nonce"" as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"0 as ""Reward"", ");
            sql.Append(@"0 as ""Loss"", ");
            sql.Append(@"0 as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"""Amount"" as ""Sent"", ");
            sql.Append(@"CASE WHEN ""Nonce"" is NULL THEN (""BakerFee"" + COALESCE(""StorageFee"", 0) + COALESCE(""AllocationFee"", 0)) ELSE 0 END as ""Fee"", ");
            sql.Append(@"""TargetId"" as ""To"" ");

            sql.Append(@"FROM ""TransactionOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""Status"" = 1 ");
            sql.Append(@"AND (""Amount"" > 0 OR CASE WHEN ""Nonce"" is NULL THEN (""BakerFee"" + COALESCE(""StorageFee"", 0) + COALESCE(""AllocationFee"", 0)) ELSE 0 END > 0) ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region internal
            sql.Append(@"8 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"""Counter"" as ""Counter"", ");
            sql.Append(@"""Nonce"" as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"0 as ""Reward"", ");
            sql.Append(@"0 as ""Loss"", ");
            sql.Append(@"0 as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"0 as ""Sent"", ");
            sql.Append(@"(COALESCE(""StorageFee"", 0) + COALESCE(""AllocationFee"", 0)) as ""Fee"", ");
            sql.Append(@"""TargetId"" as ""To"" ");

            sql.Append(@"FROM ""TransactionOps"" ");
            sql.Append(@"WHERE ""OriginalSenderId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""Status"" = 1 ");
            sql.Append(@"AND (COALESCE(""StorageFee"", 0) + COALESCE(""AllocationFee"", 0)) > 0 ");

            sql.AppendLine();
            #endregion

            sql.Append("UNION ALL SELECT ");

            #region failed
            sql.Append(@"8 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"""Counter"" as ""Counter"", ");
            sql.Append(@"""Nonce"" as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"0 as ""Reward"", ");
            sql.Append(@"0 as ""Loss"", ");
            sql.Append(@"0 as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"0 as ""Sent"", ");
            sql.Append(@"""BakerFee"" as ""Fee"", ");
            sql.Append(@"""TargetId"" as ""To"" ");

            sql.Append(@"FROM ""TransactionOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""Status"" != 1 AND ""Nonce"" is NULL ");
            sql.Append(@"AND ""BakerFee"" > 0 ");

            sql.AppendLine();
            #endregion
        }

        void UnionReveals(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"9 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"""OpHash"" as ""OpHash"", ");
            sql.Append(@"""Counter"" as ""Counter"", ");
            sql.Append(@"null::integer as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"0 as ""Reward"", ");
            sql.Append(@"0 as ""Loss"", ");
            sql.Append(@"0 as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"0 as ""Sent"", ");
            sql.Append(@"""BakerFee"" as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""RevealOps"" ");
            sql.Append(@"WHERE ""SenderId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""BakerFee"" > 0 ");

            sql.AppendLine();
        }

        void UnionRevelationPenalties(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"10 as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"null::character(51) as ""OpHash"", ");
            sql.Append(@"null::integer as ""Counter"", ");
            sql.Append(@"null::integer as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"0 as ""Reward"", "); // TODO: get reward amount from protocol constants
            sql.Append(@"(""LostReward"" + ""LostFees"") as ""Loss"", ");
            sql.Append(@"0 as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"0 as ""Sent"", ");
            sql.Append(@"0 as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""RevelationPenaltyOps"" ");
            sql.Append(@"WHERE ""BakerId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");

            sql.AppendLine();
        }

        void UnionSystem(StringBuilder sql)
        {
            sql.Append(sql.Length == 0 ? "SELECT " : "UNION ALL SELECT ");

            sql.Append(@"(11 + ""Event"") as ""Type"", ");
            sql.Append(@"""Id"" as ""Id"", ");
            sql.Append(@"""Level"" as ""Level"", ");
            sql.Append(@"null::character(51) as ""OpHash"", ");
            sql.Append(@"null::integer as ""Counter"", ");
            sql.Append(@"null::integer as ""Nonce"", ");
            sql.Append(@"""Timestamp"" as ""Timestamp"", ");
            sql.Append(@"0 as ""Reward"", ");
            sql.Append(@"0 as ""Loss"", ");
            sql.Append(@"""BalanceChange"" as ""Received"", ");
            sql.Append(@"null::integer as ""From"", ");
            sql.Append(@"0 as ""Sent"", ");
            sql.Append(@"0 as ""Fee"", ");
            sql.Append(@"null::integer as ""To"" ");

            sql.Append(@"FROM ""SystemOps"" ");
            sql.Append(@"WHERE ""AccountId"" = @account ");
            sql.Append(@"AND ""Timestamp"" >= @from AND ""Timestamp"" < @to ");
            sql.Append(@"AND ""BalanceChange"" > 0 ");

            sql.AppendLine();
        }

        static readonly string[] Operations = new[]
        {
            "baking",               // 0
            "endorsement",          // 1

            "activation",           // 2
            "double baking",        // 3
            "double endorsing",     // 4
            "nonce revelation",     // 5
            
            "delegation",           // 6
            "origination",          // 7
            "transaction",          // 8
            "reveal",               // 9
            
            "revelation penalty",   // 10
            "bootstrap",            // 11
            "activate delegate",    // 12
            "airdrop",              // 13
        };
    }
}
