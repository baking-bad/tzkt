using System.Linq;
using System.Text.Json;
using Netmavryk.Encoding;
using Mvkt.Data.Models;

namespace Mvkt.Sync.Protocols
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
                    ? new byte[] { 5, 186, 196 }
                    : bytes[0] == 1
                        ? new byte[] { 5, 186, 199 }
                        : new byte[] { 5, 186, 201 };

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
