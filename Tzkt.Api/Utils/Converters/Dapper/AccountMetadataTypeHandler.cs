using System;
using System.Data;
using Dapper;
using Tzkt.Api.Models;

namespace Tzkt.Api
{
    public class AccountMetadataTypeHandler : SqlMapper.TypeHandler<AccountMetadata>
    {
        public override AccountMetadata Parse(object value)
        {
            return AccountMetadata.Parse(value as string);
        }

        public override void SetValue(IDbDataParameter parameter, AccountMetadata value)
        {
            throw new NotImplementedException();
        }
    }
}
