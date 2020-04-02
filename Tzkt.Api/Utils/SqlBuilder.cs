using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace Tzkt.Api
{
    public class SqlBuilder
    {
        public DynamicParameters Params { get; private set; }
        public string Query => Builder.ToString();
        
        StringBuilder Builder;
        bool Filters;

        public SqlBuilder(string select)
        {
            Params = new DynamicParameters();
            Builder = new StringBuilder(select, select.Length + 80);
            Builder.AppendLine();
        }

        public SqlBuilder Filter(string expression)
        {
            AppendFilter(expression);
            return this;
        }

        public SqlBuilder Filter(string column, int value)
        {
            AppendFilter($@"""{column}"" = {value}");
            return this;
        }

        public SqlBuilder Filter(string column, AccountTypeParameter type)
        {
            if (type == null) return this;

            if (type.Eq != null)
                AppendFilter($@"""{column}"" = {type.Eq}");

            if (type.Ne != null)
                AppendFilter($@"""{column}"" != {type.Ne}");

            return this;
        }

        public SqlBuilder Filter(string column, ContractKindParameter kind)
        {
            if (kind == null) return this;

            if (kind.Eq != null)
                AppendFilter($@"""{column}"" = {kind.Eq}");

            if (kind.Ne != null)
                AppendFilter($@"""{column}"" != {kind.Ne}");

            return this;
        }

        public SqlBuilder Filter(string column, OperationStatusParameter status)
        {
            if (status == null) return this;

            if (status.Eq != null)
                AppendFilter($@"""{column}"" = {status.Eq}");

            if (status.Ne != null)
                AppendFilter($@"""{column}"" != {status.Ne}");

            return this;
        }

        public SqlBuilder Filter(string column, AccountParameter account, Func<string, string> map = null)
        {
            if (account == null) return this;

            if (account.Eq != null)
                AppendFilter($@"""{column}"" = {account.Eq}");

            if (account.Ne != null && account.Ne != -1)
                AppendFilter($@"""{column}"" != {account.Ne}");

            if (account.In != null)
            {
                AppendFilter($@"""{column}"" = ANY (@{column}In)");
                Params.Add($"{column}In", account.In);
            }

            if (account.Ni != null && account.Ni.Count > 0)
            {
                AppendFilter($@"NOT (""{column}"" = ANY (@{column}Ni))");
                Params.Add($"{column}Ni", account.Ni);
            }

            if (account.Eqx != null && map != null)
                AppendFilter($@"""{column}"" = ""{map(account.Eqx)}""");

            if (account.Nex != null && map != null)
                AppendFilter($@"""{column}"" != ""{map(account.Nex)}""");

            if (account.Null != null)
            {
                AppendFilter(account.Null == true
                    ? $@"""{column}"" IS NULL"
                    : $@"""{column}"" IS NOT NULL");
            }

            return this;
        }

        public SqlBuilder Filter(string column, StringParameter str, Func<string, string> map = null)
        {
            if (str == null) return this;

            if (str.Eq != null)
            {
                AppendFilter($@"""{column}"" = @{column}Eq");
                Params.Add($"{column}Eq", str.Eq);
            }

            if (str.Ne != null)
            {
                AppendFilter($@"""{column}"" != @{column}Ne");
                Params.Add($"{column}Ne", str.Ne);
            }

            if (str.As != null)
            {
                AppendFilter($@"""{column}"" LIKE @{column}As");
                Params.Add($"{column}As", str.As);
            }

            if (str.Un != null)
            {
                AppendFilter($@"NOT (""{column}"" LIKE (@{column}Un))");
                Params.Add($"{column}Un", str.Un);
            }

            if (str.In != null)
            {
                AppendFilter($@"""{column}"" = ANY (@{column}In)");
                Params.Add($"{column}In", str.In);
            }

            if (str.Ni != null)
            {
                AppendFilter($@"NOT (""{column}"" = ANY (@{column}Ni))");
                Params.Add($"{column}Ni", str.Ni);
            }

            if (str.Eqx != null && map != null)
                AppendFilter($@"""{column}"" = ""{map(str.Eqx)}""");

            if (str.Nex != null && map != null)
                AppendFilter($@"""{column}"" != ""{map(str.Nex)}""");

            if (str.Null != null)
            {
                AppendFilter(str.Null == true
                    ? $@"""{column}"" IS NULL"
                    : $@"""{column}"" IS NOT NULL");
            }

            return this;
        }

        public SqlBuilder Filter(string column, Int32Parameter value, Func<string, string> map = null)
        {
            if (value == null) return this;

            if (value.Eq != null)
                AppendFilter($@"""{column}"" = {value.Eq}");

            if (value.Ne != null)
                AppendFilter($@"""{column}"" != {value.Ne}");

            if (value.Gt != null)
                AppendFilter($@"""{column}"" > {value.Gt}");

            if (value.Ge != null)
                AppendFilter($@"""{column}"" >= {value.Ge}");

            if (value.Lt != null)
                AppendFilter($@"""{column}"" < {value.Lt}");

            if (value.Le != null)
                AppendFilter($@"""{column}"" <= {value.Le}");

            if (value.In != null)
            {
                AppendFilter($@"""{column}"" = ANY (@{column}In)");
                Params.Add($"{column}In", value.In);
            }

            if (value.Ni != null)
            {
                AppendFilter($@"NOT (""{column}"" = ANY (@{column}Ni))");
                Params.Add($"{column}Ni", value.Ni);
            }

            if (value.Eqx != null && map != null)
                AppendFilter($@"""{column}"" = ""{map(value.Eqx)}""");

            if (value.Nex != null && map != null)
                AppendFilter($@"""{column}"" != ""{map(value.Nex)}""");

            if (value.Null != null)
            {
                AppendFilter(value.Null == true
                    ? $@"""{column}"" IS NULL"
                    : $@"""{column}"" IS NOT NULL");
            }

            return this;
        }

        public SqlBuilder Filter(string column, Int64Parameter value, Func<string, string> map = null)
        {
            if (value == null) return this;

            if (value.Eq != null)
                AppendFilter($@"""{column}"" = {value.Eq}");

            if (value.Ne != null)
                AppendFilter($@"""{column}"" != {value.Ne}");

            if (value.Gt != null)
                AppendFilter($@"""{column}"" > {value.Gt}");

            if (value.Ge != null)
                AppendFilter($@"""{column}"" >= {value.Ge}");

            if (value.Lt != null)
                AppendFilter($@"""{column}"" < {value.Lt}");

            if (value.Le != null)
                AppendFilter($@"""{column}"" <= {value.Le}");

            if (value.In != null)
            {
                AppendFilter($@"""{column}"" = ANY (@{column}In)");
                Params.Add($"{column}In", value.In);
            }

            if (value.Ni != null)
            {
                AppendFilter($@"NOT (""{column}"" = ANY (@{column}Ni))");
                Params.Add($"{column}Ni", value.Ni);
            }

            if (value.Eqx != null && map != null)
                AppendFilter($@"""{column}"" = ""{map(value.Eqx)}""");

            if (value.Nex != null && map != null)
                AppendFilter($@"""{column}"" != ""{map(value.Nex)}""");

            if (value.Null != null)
            {
                AppendFilter(value.Null == true
                    ? $@"""{column}"" IS NULL"
                    : $@"""{column}"" IS NOT NULL");
            }

            return this;
        }

        public SqlBuilder Filter(string column, BoolParameter value, Func<string, string> map = null)
        {
            if (value == null) return this;

            if (value.Eq != null)
                AppendFilter($@"""{column}"" = {value.Eq}");

            if (value.Null != null)
            {
                AppendFilter(value.Null == true
                    ? $@"""{column}"" IS NULL"
                    : $@"""{column}"" IS NOT NULL");
            }

            return this;
        }

        public SqlBuilder Take(SortParameter sort, OffsetParameter offset, int limit, Func<string, string> map = null)
        {
            var sortAsc = true;
            var sortColumn = "Id";

            if (sort != null && map != null)
            {
                if (sort.Asc != null)
                {
                    sortColumn = map(sort.Asc);
                }
                else if (sort.Desc != null)
                {
                    sortAsc = false;
                    sortColumn = map(sort.Desc);
                }
            }

            if (offset?.Cr != null)
            {
                AppendFilter(sortAsc 
                    ? $@"""{sortColumn}"" > {offset.Cr}"
                    : $@"""{sortColumn}"" < {offset.Cr}");
            }

            Builder.AppendLine(sortAsc
                ? $@"ORDER BY ""{sortColumn}"""
                : $@"ORDER BY ""{sortColumn}"" DESC");

            if (offset != null)
            {
                if (offset.El != null)
                    Builder.AppendLine($"OFFSET {offset.El}");
                else if (offset.Pg != null)
                    Builder.AppendLine($"OFFSET {offset.Pg * limit}");
            }

            Builder.AppendLine($"LIMIT {limit}");
            return this;
        }

        public SqlBuilder Take(OffsetParameter offset, int limit)
        {
            if (offset?.Cr != null)
                AppendFilter($@"""Id"" > {offset.Cr} ");

            Builder.AppendLine($@"ORDER BY ""Id""");

            if (offset != null)
            {
                if (offset.El != null)
                    Builder.AppendLine($"OFFSET {offset.El}");
                else if (offset.Pg != null)
                    Builder.AppendLine($"OFFSET {offset.Pg * limit}");
            }

            Builder.AppendLine($"LIMIT {limit}");
            return this;
        }

        void AppendFilter(string filter)
        {
            if (!Filters)
            {
                Builder.Append("WHERE ");
                Filters = true;
            }
            else
            {
                Builder.Append("AND ");
            }

            Builder.AppendLine(filter);
        }
    }
}
