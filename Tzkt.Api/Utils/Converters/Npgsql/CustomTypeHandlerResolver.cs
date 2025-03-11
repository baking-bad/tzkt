using System.Numerics;
using Npgsql.Internal.TypeHandling;
using Npgsql.Internal;
using NpgsqlTypes;

namespace Tzkt.Api
{
    public class CustomTypeHandlerResolver : TypeHandlerResolver
    {
        readonly NumericBigIntegerTypeHandler NumericBigInteger;

        public CustomTypeHandlerResolver(NpgsqlConnector connector)
        {
            NumericBigInteger = new NumericBigIntegerTypeHandler(
                connector.DatabaseInfo.GetPostgresTypeByName("numeric"));
        }

        public override TypeMappingInfo? GetMappingByDataTypeName(string dataTypeName)
        {
            return dataTypeName == "numeric"
                ? new TypeMappingInfo(NpgsqlDbType.Numeric, "numeric", typeof(BigInteger))
                : null;
        }

        public override NpgsqlTypeHandler? ResolveByClrType(Type type)
        {
            return type == typeof(BigInteger) ? NumericBigInteger : null;
        }

        public override NpgsqlTypeHandler? ResolveByDataTypeName(string typeName)
        {
            return typeName == "numeric" ? NumericBigInteger : null;
        }
    }
}
