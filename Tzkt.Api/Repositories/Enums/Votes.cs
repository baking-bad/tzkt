using System;
using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class Votes
    {
        public const string Yay = "yay";
        public const string Nay = "nay";
        public const string Pass = "pass";

        public static string ToString(int value) => value switch
        {
            (int)Vote.Yay => Yay,
            (int)Vote.Nay => Nay,
            (int)Vote.Pass => Pass,
            _ => throw new Exception("invalid vote value")
        };
    }
}
