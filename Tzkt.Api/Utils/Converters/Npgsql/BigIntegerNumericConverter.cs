using System.Buffers;
using System.Numerics;
using Npgsql.Internal;

#pragma warning disable NPG9001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
namespace Tzkt.Api
{
    sealed class BigIntegerNumericConverter : PgStreamingConverter<BigInteger>
    {
        const int StackAllocByteThreshold = 64 * sizeof(uint);

        public override BigInteger Read(PgReader reader)
        {
            if (reader.ShouldBuffer(sizeof(short)))
                reader.Buffer(sizeof(short));

            var digitCount = reader.ReadInt16();
            short[]? digitsFromPool = null;
            var digits = (digitCount <= StackAllocByteThreshold / sizeof(short)
                ? stackalloc short[StackAllocByteThreshold / sizeof(short)]
                : (digitsFromPool = ArrayPool<short>.Shared.Rent(digitCount)).AsSpan()).Slice(0, digitCount);

            var value = ConvertTo(NumericConverter.Read(reader, digits));

            if (digitsFromPool is not null)
                ArrayPool<short>.Shared.Return(digitsFromPool);

            return value;
        }

        public override ValueTask<BigInteger> ReadAsync(PgReader reader, CancellationToken cancellationToken = default)
        {
            // If we don't need a read and can read buffered we delegate to our sync read method which won't do IO in such a case.
            if (!reader.ShouldBuffer(reader.CurrentRemaining))
                Read(reader);

            return AsyncCore(reader, cancellationToken);

            static async ValueTask<BigInteger> AsyncCore(PgReader reader, CancellationToken cancellationToken)
            {
                if (reader.ShouldBuffer(sizeof(short)))
                    await reader.BufferAsync(sizeof(short), cancellationToken).ConfigureAwait(false);

                var digitCount = reader.ReadInt16();
                var digits = new ArraySegment<short>(ArrayPool<short>.Shared.Rent(digitCount), 0, digitCount);
                var value = ConvertTo(await NumericConverter.ReadAsync(reader, digits, cancellationToken).ConfigureAwait(false));

                ArrayPool<short>.Shared.Return(digits.Array!);

                return value;
            }
        }

        public override Size GetSize(SizeContext context, BigInteger value, ref object? writeState) =>
            PgNumeric.GetByteCount(PgNumeric.GetDigitCount(value));

        public override void Write(PgWriter writer, BigInteger value)
        {
            // We don't know how many digits we need so we allocate a decent chunk of stack for the builder to use.
            // If it's not enough for the builder will do a heap allocation (for decimal it's always enough).
            Span<short> destination = stackalloc short[StackAllocByteThreshold / sizeof(short)];
            var numeric = ConvertFrom(value, destination);
            NumericConverter.Write(writer, numeric);
        }

        public override ValueTask WriteAsync(PgWriter writer, BigInteger value, CancellationToken cancellationToken = default)
        {
            if (writer.ShouldFlush(writer.Current.Size))
                return AsyncCore(writer, value, cancellationToken);

            // If we don't need a flush and can write buffered we delegate to our sync write method which won't flush in such a case.
            Write(writer, value);
            return new();

            static async ValueTask AsyncCore(PgWriter writer, BigInteger value, CancellationToken cancellationToken)
            {
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                var numeric = ConvertFrom(value, Array.Empty<short>()).Build();
                await NumericConverter.WriteAsync(writer, numeric, cancellationToken).ConfigureAwait(false);
            }
        }

        static PgNumeric.Builder ConvertFrom(BigInteger value, Span<short> destination) => new(value, destination);
        static BigInteger ConvertTo(in PgNumeric.Builder numeric) => numeric.ToBigInteger();
        static BigInteger ConvertTo(in PgNumeric numeric) => numeric.ToBigInteger();
    }
}
#pragma warning restore NPG9001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
