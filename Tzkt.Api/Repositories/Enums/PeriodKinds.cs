using System;
using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class PeriodKinds
    {
        public const string Proposal = "proposal";
        public const string Exploration = "exploration";
        public const string Testing = "testing";
        public const string Promotion = "promotion";
        public const string Adoption = "adoption";

        public static string ToString(int value) => value switch
        {
            (int)PeriodKind.Proposal => Proposal,
            (int)PeriodKind.Exploration => Exploration,
            (int)PeriodKind.Testing => Testing,
            (int)PeriodKind.Promotion => Promotion,
            (int)PeriodKind.Adoption => Adoption,
            _ => throw new Exception("invalid period kind value")
        };
    }
}
