using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class PvmKinds
    {
        public const string Arith = "arith";
        public const string Wasm = "wasm";

        public static bool TryParse(string value, out int res)
        {
            res = value switch
            {
                Arith => (int)PvmKind.Arith,
                Wasm => (int)PvmKind.Wasm,
                _ => -1
            };
            return res != -1;
        }

        public static string ToString(int value) => value switch
        {
            (int)PvmKind.Arith => Arith,
            (int)PvmKind.Wasm => Wasm,
            _ => throw new Exception("invalid PVM kind value")
        };
    }
}
