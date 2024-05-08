using System.Numerics;
using Npgsql.BackendMessages;
using Npgsql.Internal.TypeHandlers.NumericHandlers;
using Npgsql.Internal.TypeHandling;
using Npgsql.Internal;
using Npgsql.PostgresTypes;
using Npgsql;

namespace Tzkt.Api
{
    public class NumericBigIntegerTypeHandler : NpgsqlTypeHandler<BigInteger>
    {
        readonly NumericHandler Numeric;

        public NumericBigIntegerTypeHandler(PostgresType postgresType) : base(postgresType)
        {
            Numeric = new NumericHandler(postgresType);
        }

        public override ValueTask<BigInteger> Read(NpgsqlReadBuffer buf, int len, bool async, FieldDescription fieldDescription = null)
        {
            return ValueTask.FromResult(Numeric.Read<BigInteger>(buf, len, fieldDescription));
        }

        public override int ValidateAndGetLength(BigInteger value, ref NpgsqlLengthCache lengthCache, NpgsqlParameter parameter)
        {
            return Numeric.ValidateAndGetLength(value, ref lengthCache, parameter);
        }

        public override int ValidateObjectAndGetLength(object value, ref NpgsqlLengthCache lengthCache, NpgsqlParameter parameter)
        {
            return Numeric.ValidateObjectAndGetLength(value, ref lengthCache, parameter);
        }

        public override Task Write(BigInteger value, NpgsqlWriteBuffer buf, NpgsqlLengthCache lengthCache, NpgsqlParameter parameter, bool async, CancellationToken cancellationToken = default)
        {
            return Numeric.Write(value, buf, lengthCache, parameter, async, cancellationToken);
        }

        public override Task WriteObjectWithLength(object value, NpgsqlWriteBuffer buf, NpgsqlLengthCache lengthCache, NpgsqlParameter parameter, bool async, CancellationToken cancellationToken = default)
        {
            return Numeric.WriteObjectWithLength(value, buf, lengthCache, parameter, async, cancellationToken);
        }
    }
}
