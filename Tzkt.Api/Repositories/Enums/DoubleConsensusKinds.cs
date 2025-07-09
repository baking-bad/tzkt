using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class DoubleConsensusKinds
    {
        public const string DoubleAttestation = "double_attestation";
        public const string DoublePreattestation = "double_preattestation";

        public static bool TryParse(string value, out int res)
        {
            res = value switch
            {
                DoubleAttestation => (int)DoubleConsensusKind.DoubleAttestation,
                DoublePreattestation => (int)DoubleConsensusKind.DoublePreattestation,
                _ => -1
            };
            return res != -1;
        }

        public static string ToString(int value) => value switch
        {
            (int)DoubleConsensusKind.DoubleAttestation => DoubleAttestation,
            (int)DoubleConsensusKind.DoublePreattestation => DoublePreattestation,
            _ => throw new Exception("invalid double consensus kind")
        };
    }
}
