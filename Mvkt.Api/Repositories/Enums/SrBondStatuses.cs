using Mvkt.Data.Models;

namespace Mvkt.Api
{
    static class SrBondStatuses
    {
        public const string Active = "active";
        public const string Returned = "returned";
        public const string Lost = "lost";

        public static bool TryParse(string value, out int res)
        {
            res = value switch
            {
                Active => (int)SmartRollupBondStatus.Active,
                Returned => (int)SmartRollupBondStatus.Returned,
                Lost => (int)SmartRollupBondStatus.Lost,
                _ => -1
            };
            return res != -1;
        }

        public static string ToString(int value) => value switch
        {
            (int)SmartRollupBondStatus.Active => Active,
            (int)SmartRollupBondStatus.Returned => Returned,
            (int)SmartRollupBondStatus.Lost => Lost,
            _ => throw new Exception("invalid smart rollup bond status value")
        };
    }
}
