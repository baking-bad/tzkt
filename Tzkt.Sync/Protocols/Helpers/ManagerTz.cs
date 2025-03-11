using System.Text.Json;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols
{
    static class ManagerTz
    {
        public static bool Test(JsonElement code, JsonElement storage)
        {
            if (storage.ValueKind != JsonValueKind.Object || storage.EnumerateObject().Count() != 1 ||
                (!storage.TryGetProperty("string", out _) && !storage.TryGetProperty("bytes", out _)))
                return false;

            return Script.ManagerTzBytes.IsEqual(Micheline.FromJson(code)!.ToBytes());
        }

        public static string? GetManager(JsonElement storage)
        {
            if (storage.TryGetProperty("bytes", out var keyBytes) && Hex.TryParse(keyBytes.RequiredString(), out var bytes))
            {
                if (bytes[0] > 2) return null;

                byte[] prefix = bytes[0] switch
                {
                    0 => [6, 161, 159],
                    1 => [6, 161, 161],
                    2 => [6, 161, 164],
                    3 => [6, 161, 166],
                    _ => throw new Exception("Invalid address prefix"),
                };

                return Base58.Convert(bytes.GetBytes(1, bytes.Length - 1), prefix);
            }
            else if (storage.TryGetProperty("string", out var keyStr))
            {
                return keyStr.GetString();
            }
            return null;
        }
    }
}
