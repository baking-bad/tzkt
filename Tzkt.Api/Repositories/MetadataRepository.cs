using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using TzKT_Client;

namespace Tzkt.Api.Repositories
{
    public class MetadataRepository : DbConnection
    {
        public MetadataRepository(IConfiguration config) : base(config) {}
        
        
        public async Task Update(string table, string key, List<Met> metadatas)
        {
            //TODO pass table and list of obj key, metadata
            using var db = GetConnection();

            foreach (var metadata in metadatas)
            {
                var upd = $@"UPDATE ""{table}"" SET ""Metadata"" = @metadata::jsonb WHERE ""{key}"" = @key";
                var rows = await db.ExecuteAsync(upd, new {metadata = metadata.MetJson, key = metadata.Key});
            }
        }

        public async Task GetMetadata(string table, string key)
        {
            throw new NotImplementedException();
        }
    }
}