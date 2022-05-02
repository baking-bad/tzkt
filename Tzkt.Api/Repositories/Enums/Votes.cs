using System;
using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class Votes
    {
        public const string Yay = "yay";
        public const string Nay = "nay";
        public const string Pass = "pass";

        public static bool TryParse(string value, out int res)
        {
            res = value switch
            {
                Yay => (int)Vote.Yay,
                Nay => (int)Vote.Nay,
                Pass => (int)Vote.Pass,
                _ => -1
            };
            return res != -1;
        }

        public static string ToString(int value) => value switch
        {
            (int)Vote.Yay => Yay,
            (int)Vote.Nay => Nay,
            (int)Vote.Pass => Pass,
            _ => throw new Exception("invalid vote value")
        };
    }
}
