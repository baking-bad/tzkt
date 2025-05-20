using System.Numerics;
using Npgsql.Internal;
using Npgsql.Internal.Postgres;

#pragma warning disable NPG9001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
namespace Tzkt.Api
{
    class BigIntegerNumericTypeInfoResolverFactory : PgTypeInfoResolverFactory
    {
        public override IPgTypeInfoResolver CreateResolver() => new Resolver();
        public override IPgTypeInfoResolver? CreateArrayResolver() => new ArrayResolver();

        class Resolver : IPgTypeInfoResolver
        {
            protected static DataTypeName NumericDataTypeName => new("pg_catalog.numeric");

            TypeInfoMappingCollection? _mappings;
            TypeInfoMappingCollection Mappings => _mappings ??= AddMappings(new());

            protected virtual TypeInfoMappingCollection AddMappings(TypeInfoMappingCollection mappings)
            {
                mappings.AddStructType<BigInteger>(NumericDataTypeName,
                    static (options, mapping, _) => mapping.CreateInfo(options, new BigIntegerNumericConverter()), true);
                return mappings;
            }

            public PgTypeInfo? GetTypeInfo(Type? type, DataTypeName? dataTypeName, PgSerializerOptions options)
            {
                return Mappings.Find(type, dataTypeName, options);
            }
        }

        class ArrayResolver : Resolver
        {
            protected override TypeInfoMappingCollection AddMappings(TypeInfoMappingCollection mappings)
            {
                base.AddMappings(mappings).AddStructArrayType<BigInteger>(NumericDataTypeName);
                return mappings;
            }
        }
    }
}
#pragma warning restore NPG9001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
