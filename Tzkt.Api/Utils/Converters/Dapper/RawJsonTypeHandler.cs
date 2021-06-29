using System;
using System.Data;
using Dapper;

namespace Tzkt.Api
{
    public class RawJsonTypeHandler : SqlMapper.TypeHandler<RawJson>
    {
        public override RawJson Parse(object value)
        {
            return value as string;
        }

        public override void SetValue(IDbDataParameter parameter, RawJson value)
        {
            throw new NotImplementedException();
        }
    }
}
