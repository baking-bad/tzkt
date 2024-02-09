using Mvkt.Data.Models;

namespace Mvkt.Api
{
    static class RefutationGameStatuses
    {
        public const string None = "none";
        public const string Ongoing = "ongoing";
        public const string Loser = "loser";
        public const string Draw = "draw";

        public static bool TryParse(string value, out int res)
        {
            res = value switch
            {
                None => (int)RefutationGameStatus.None,
                Ongoing => (int)RefutationGameStatus.Ongoing,
                Loser => (int)RefutationGameStatus.Loser,
                Draw => (int)RefutationGameStatus.Draw,
                _ => -1
            };
            return res != -1;
        }

        public static string ToString(int value) => value switch
        {
            (int)RefutationGameStatus.None => None,
            (int)RefutationGameStatus.Ongoing => Ongoing,
            (int)RefutationGameStatus.Loser => Loser,
            (int)RefutationGameStatus.Draw => Draw,
            _ => throw new Exception("invalid refutation game status value")
        };

        public static bool IsEnd(int value) => value >= (int)RefutationGameStatus.Loser;
    }
}
