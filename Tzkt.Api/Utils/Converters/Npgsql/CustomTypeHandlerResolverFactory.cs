using System.Numerics;
using Npgsql.Internal.TypeHandling;
using Npgsql.Internal;

namespace Tzkt.Api
{
    public class CustomTypeHandlerResolverFactory : TypeHandlerResolverFactory
    {
        CustomTypeHandlerResolver? Resolver;

        public override TypeHandlerResolver Create(NpgsqlConnector connector)
        {
            return Resolver ??= new CustomTypeHandlerResolver(connector);
        }

        public override string? GetDataTypeNameByClrType(Type clrType)
        {
            return clrType == typeof(BigInteger) ? "numeric" : null;
        }

        public override TypeMappingInfo? GetMappingByDataTypeName(string dataTypeName)
        {
            return Resolver?.GetMappingByDataTypeName(dataTypeName);
        }
    }
}
