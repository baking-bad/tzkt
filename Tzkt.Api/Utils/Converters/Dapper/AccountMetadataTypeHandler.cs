using System;
using System.Data;
using System.Text.Json;
using Dapper;
using Tzkt.Api.Models;

namespace Tzkt.Api
{
    public class AccountMetadataTypeHandler : SqlMapper.TypeHandler<AccountMetadata>
    {
        public override AccountMetadata Parse(object value)
        {
            if (value is not string) return null;
            return JsonSerializer.Deserialize<AccountMetadata>((string)value);
        }

        public override void SetValue(IDbDataParameter parameter, AccountMetadata value)
        {
            throw new NotImplementedException();
        }
    }
}
