using System;
using System.Data;
using Dapper;
using Tzkt.Api.Models;

namespace Tzkt.Api
{
    public class ProfileMetadataTypeHandler : SqlMapper.TypeHandler<ProfileMetadata>
    {
        public override ProfileMetadata Parse(object value)
        {
            return AccountMetadata.Parse(value as string)?.Profile;
        }

        public override void SetValue(IDbDataParameter parameter, ProfileMetadata value)
        {
            throw new NotImplementedException();
        }
    }
}
