using System;
using System.Linq;
using System.Text.Json;
using Tzkt.Sync.Utils;

namespace Tzkt.Sync.Protocols
{
    static class ManagerTz
    {
        const string Code = @"[{""prim"":""parameter"",""args"":[{""prim"":""or"",""args"":[{""prim"":""lambda"",""args"":[{""prim"":""unit""},{""prim"":""list"",""args"":[{""prim"":""operation""}]}],""annots"":[""%do""]},{""prim"":""unit"",""annots"":[""%default""]}]}]},{""prim"":""storage"",""args"":[{""prim"":""key_hash""}]},{""prim"":""code"",""args"":[[[[{""prim"":""DUP""},{""prim"":""CAR""},{""prim"":""DIP"",""args"":[[{""prim"":""CDR""}]]}]],{""prim"":""IF_LEFT"",""args"":[[{""prim"":""PUSH"",""args"":[{""prim"":""mutez""},{""int"":""0""}]},{""prim"":""AMOUNT""},[[{""prim"":""COMPARE""},{""prim"":""EQ""}],{""prim"":""IF"",""args"":[[],[[{""prim"":""UNIT""},{""prim"":""FAILWITH""}]]]}],[{""prim"":""DIP"",""args"":[[{""prim"":""DUP""}]]},{""prim"":""SWAP""}],{""prim"":""IMPLICIT_ACCOUNT""},{""prim"":""ADDRESS""},{""prim"":""SENDER""},[[{""prim"":""COMPARE""},{""prim"":""EQ""}],{""prim"":""IF"",""args"":[[],[[{""prim"":""UNIT""},{""prim"":""FAILWITH""}]]]}],{""prim"":""UNIT""},{""prim"":""EXEC""},{""prim"":""PAIR""}],[{""prim"":""DROP""},{""prim"":""NIL"",""args"":[{""prim"":""operation""}]},{""prim"":""PAIR""}]]}]]}]";

        public static bool Test(JsonElement code, JsonElement storage)
        {
            if (storage.ValueKind != JsonValueKind.Object || storage.EnumerateObject().Count() != 1 ||
                (!storage.TryGetProperty("string", out _) && !storage.TryGetProperty("bytes", out _)))
                return false;

            return JsonSerializer.Serialize(code) == Code;
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
