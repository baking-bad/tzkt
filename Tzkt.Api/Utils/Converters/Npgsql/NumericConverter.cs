using Npgsql.Internal;

#pragma warning disable NPG9001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
namespace Tzkt.Api
{
    static class NumericConverter
    {
        public static readonly int DecimalBasedMaxByteCount = PgNumeric.GetByteCount(PgNumeric.Builder.MaxDecimalNumericDigits);

        public static PgNumeric.Builder Read(PgReader reader, Span<short> digits)
        {
            var remainingStructureSize = PgNumeric.GetByteCount(0) - sizeof(short);
            if (reader.ShouldBuffer(remainingStructureSize))
                reader.Buffer(remainingStructureSize);
            var weight = reader.ReadInt16();
            var sign = reader.ReadInt16();
            var scale = reader.ReadInt16();
            foreach (ref var digit in digits)
            {
                if (reader.ShouldBuffer(sizeof(short)))
                    reader.Buffer(sizeof(short));
                digit = reader.ReadInt16();
            }

            return new PgNumeric.Builder(digits, weight, sign, scale);
        }

        public static async ValueTask<PgNumeric> ReadAsync(PgReader reader, ArraySegment<short> digits, CancellationToken cancellationToken)
        {
            var remainingStructureSize = PgNumeric.GetByteCount(0) - sizeof(short);
            if (reader.ShouldBuffer(remainingStructureSize))
                await reader.BufferAsync(remainingStructureSize, cancellationToken).ConfigureAwait(false);
            var weight = reader.ReadInt16();
            var sign = reader.ReadInt16();
            var scale = reader.ReadInt16();
            var array = digits.Array!;
            for (var i = digits.Offset; i < array.Length; i++)
            {
                if (reader.ShouldBuffer(sizeof(short)))
                    await reader.BufferAsync(sizeof(short), cancellationToken).ConfigureAwait(false);
                array[i] = reader.ReadInt16();
            }

            return new PgNumeric.Builder(digits, weight, sign, scale).Build();
        }

        public static void Write(PgWriter writer, PgNumeric.Builder numeric)
        {
            if (writer.ShouldFlush(PgNumeric.GetByteCount(0)))
                writer.Flush();
            writer.WriteInt16((short)numeric.Digits.Length);
            writer.WriteInt16(numeric.Weight);
            writer.WriteInt16(numeric.Sign);
            writer.WriteInt16(numeric.Scale);

            foreach (var digit in numeric.Digits)
            {
                if (writer.ShouldFlush(sizeof(short)))
                    writer.Flush();
                writer.WriteInt16(digit);
            }
        }

        public static async ValueTask WriteAsync(PgWriter writer, PgNumeric numeric, CancellationToken cancellationToken)
        {
            if (writer.ShouldFlush(PgNumeric.GetByteCount(0)))
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            writer.WriteInt16((short)numeric.Digits.Count);
            writer.WriteInt16(numeric.Weight);
            writer.WriteInt16(numeric.Sign);
            writer.WriteInt16(numeric.Scale);

            foreach (var digit in numeric.Digits)
            {
                if (writer.ShouldFlush(sizeof(short)))
                    await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                writer.WriteInt16(digit);
            }
        }
    }
}
#pragma warning restore NPG9001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
