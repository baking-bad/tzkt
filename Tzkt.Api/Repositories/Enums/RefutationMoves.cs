using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class RefutationMoves
    {
        public const string Start = "start";
        public const string Dissection = "dissection";
        public const string Proof = "proof";
        public const string Timeout = "timeout";

        public static bool TryParse(string value, out int res)
        {
            res = value switch
            {
                Start => (int)RefutationMove.Start,
                Dissection => (int)RefutationMove.Dissection,
                Proof => (int)RefutationMove.Proof,
                Timeout => (int)RefutationMove.Timeout,
                _ => -1
            };
            return res != -1;
        }

        public static string ToString(int value) => value switch
        {
            (int)RefutationMove.Start => Start,
            (int)RefutationMove.Dissection => Dissection,
            (int)RefutationMove.Proof => Proof,
            (int)RefutationMove.Timeout => Timeout,
            _ => throw new Exception("invalid refutation move value")
        };
    }
}
