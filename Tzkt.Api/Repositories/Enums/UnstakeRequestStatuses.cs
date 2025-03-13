using System.Diagnostics.CodeAnalysis;

namespace Tzkt.Api
{
    static class UnstakeRequestStatuses
    {
        public const string Pending = "pending";
        public const string Finalizable = "finalizable";
        public const string Finalized = "finalized";

        public static bool TryParse(string value, [NotNullWhen(true)] out string? res)
        {
            res = value switch
            {
                Pending => Pending,
                Finalizable => Finalizable,
                Finalized => Finalized,
                _ => null
            };
            return res != null;
        }

        public static string ToString(int cycle, long remainingAmount, int unfrozenCycle)
        {
            return cycle > unfrozenCycle ? Pending : remainingAmount != 0 ? Finalizable : Finalized;
        }
    }
}
