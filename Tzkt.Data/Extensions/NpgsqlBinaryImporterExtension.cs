using Npgsql;
using NpgsqlTypes;

namespace Tzkt.Data
{
    static class NpgsqlBinaryImporterExtension
    {
        public static void WriteNullable<T>(this NpgsqlBinaryImporter writer, T? value, NpgsqlDbType type)
        {
            if (value is T _value)
                writer.Write(_value, type);
            else
                writer.WriteNull();
        }
    }
}
