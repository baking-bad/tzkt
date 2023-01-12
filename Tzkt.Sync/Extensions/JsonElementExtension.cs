using System;
using System.Linq;
using System.Text.Json;

namespace Tzkt.Sync
{
    static class JsonElementExtension
    {
        public static JsonElement Required(this JsonElement el, string name)
        {
            return el.TryGetProperty(name, out var res) ? res
                : throw new SerializationException($"Missed required property {name}");
        }

        public static int Count(this JsonElement el)
        {
            return el.EnumerateArray().Count();
        }

        public static JsonElement RequiredArray(this JsonElement el)
        {
            return el.ValueKind == JsonValueKind.Array ? el
                : throw new SerializationException($"Expected array but got {el.ValueKind}");
        }

        public static JsonElement RequiredArray(this JsonElement el, string name)
        {
            return el.TryGetProperty(name, out var res) && res.ValueKind == JsonValueKind.Array ? res
                : throw new SerializationException($"Missed required array {name}");
        }

        public static JsonElement? OptionalArray(this JsonElement el, string name)
        {
            if (!el.TryGetProperty(name, out var res))
                return null;

            return res.ValueKind == JsonValueKind.Array ? res
                : throw new SerializationException($"Expected array but got {res.ValueKind}");
        }

        public static JsonElement RequiredArray(this JsonElement el, string name, int count)
        {
            return el.TryGetProperty(name, out var res) && res.ValueKind == JsonValueKind.Array
                && res.EnumerateArray().Count() == count ? res
                    : throw new SerializationException($"Missed required array {name}[{count}]");
        }

        public static string RequiredString(this JsonElement el)
        {
            return el.ValueKind == JsonValueKind.String ? el.GetString()
                : throw new SerializationException($"Expected string but got {el.ValueKind}");
        }

        public static string RequiredString(this JsonElement el, string name)
        {
            return el.TryGetProperty(name, out var res) && res.ValueKind == JsonValueKind.String ? res.GetString()
                : throw new SerializationException($"Missed required string {name}");
        }

        public static string OptionalString(this JsonElement el, string name)
        {
            if (!el.TryGetProperty(name, out var res))
                return null;
            
            return res.ValueKind == JsonValueKind.String ? res.GetString()
                : throw new SerializationException($"Expected string but got {res.ValueKind}");
        }

        public static DateTime RequiredDateTime(this JsonElement el, string name)
        {
            return el.TryGetProperty(name, out var res) && res.ValueKind == JsonValueKind.String ? res.GetDateTimeOffset().UtcDateTime
                : throw new SerializationException($"Missed required string {name}");
        }

        public static bool RequiredBool(this JsonElement el, string name)
        {
            return el.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.True || prop.ValueKind == JsonValueKind.False
                ? prop.ValueKind == JsonValueKind.True
                : throw new SerializationException($"Missed required bool {name}");
        }

        public static bool? OptionalBool(this JsonElement el, string name)
        {
            if (!el.TryGetProperty(name, out var prop))
                return null;

            return prop.ValueKind == JsonValueKind.True || (prop.ValueKind == JsonValueKind.False ? false
                : throw new SerializationException($"Invalid bool {name}"));
        }

        public static int? OptionalInt32(this JsonElement el, string name)
        {
            if (!el.TryGetProperty(name, out var prop))
                return null;

            return prop.TryParseInt32(out var res) ? res
                : throw new SerializationException($"Invalid int {name}");
        }

        public static long? OptionalInt64(this JsonElement el, string name)
        {
            if (!el.TryGetProperty(name, out var prop))
                return null;

            return prop.TryParseInt64(out var res) ? res
                : throw new SerializationException($"Invalid long {name}");
        }

        public static int RequiredInt32(this JsonElement el)
        {
            return el.TryParseInt32(out var res) ? res
                : throw new SerializationException($"Expected int but got {el.ValueKind}");
        }

        public static int RequiredInt32(this JsonElement el, string name)
        {
            return el.TryGetProperty(name, out var prop) && prop.TryParseInt32(out var res) ? res
                : throw new SerializationException($"Missed required int {name}");
        }

        public static long RequiredInt64(this JsonElement el)
        {
            return el.TryParseInt64(out var res) ? res
                : throw new SerializationException($"Expected long but got {el.ValueKind}");
        }

        public static long RequiredInt64(this JsonElement el, string name)
        {
            return el.TryGetProperty(name, out var prop) && prop.TryParseInt64(out var res) ? res
                : throw new SerializationException($"Missed required long {name}");
        }

        public static int ParseInt32(this JsonElement el)
        {
            return el.ValueKind == JsonValueKind.String
                ? int.Parse(el.GetString())
                : el.GetInt32();
        }

        public static bool TryParseInt32(this JsonElement el, out int res)
        {
            return el.ValueKind == JsonValueKind.String
                ? int.TryParse(el.GetString(), out res)
                : el.TryGetInt32(out res);
        }

        public static long ParseInt64(this JsonElement el)
        {
            return el.ValueKind == JsonValueKind.String
                ? long.Parse(el.GetString())
                : el.GetInt64();
        }

        public static bool TryParseInt64(this JsonElement el, out long res)
        {
            return el.ValueKind == JsonValueKind.String
                ? long.TryParse(el.GetString(), out res)
                : el.TryGetInt64(out res);
        }
    }
}
