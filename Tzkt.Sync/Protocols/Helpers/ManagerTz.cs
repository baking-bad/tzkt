using System.Linq;
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

            return Script.ManagerTzBytes.IsEqual(Micheline.FromJson(code).ToBytes());
        }

        public static string GetManager(JsonElement storage)
        {
            if (storage.TryGetProperty("bytes", out var keyBytes) && Hex.TryParse(keyBytes.GetString(), out var bytes))
            {
                if (bytes[0] > 2) return null;

                var prefix = bytes[0] == 0
                    ? new byte[] { 6, 161, 159 }
                    : bytes[0] == 1
                        ? new byte[] { 6, 161, 161 }
                        : new byte[] { 6, 161, 164 };

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
