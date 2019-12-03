using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Tzkt.Api
{
    public abstract class DbConnection
    {
        readonly string ConnectionString;

        protected DbConnection(IConfiguration config)
        {
            ConnectionString = config.GetConnectionString("DefaultConnection");
        }

        protected IDbConnection GetConnection() => new NpgsqlConnection(ConnectionString);
    }
}
