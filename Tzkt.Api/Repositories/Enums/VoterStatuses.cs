using System;
using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class VoterStatuses
    {
        public const string None = "none";
        public const string Upvoted = "upvoted";
        public const string VotedYay = "voted_yay";
        public const string VotedNay = "voted_nay";
        public const string VotedPass = "voted_pass";

        public static bool TryParse(string value, out int res)
        {
            res = value switch
            {
                None => (int)VoterStatus.None,
                Upvoted => (int)VoterStatus.Upvoted,
                VotedYay => (int)VoterStatus.VotedYay,
                VotedNay => (int)VoterStatus.VotedNay,
                VotedPass => (int)VoterStatus.VotedPass,
                _ => -1
            };
            return res != -1;
        }

        public static string ToString(int value) => value switch
        {
            (int)VoterStatus.None => None,
            (int)VoterStatus.Upvoted => Upvoted,
            (int)VoterStatus.VotedYay => VotedYay,
            (int)VoterStatus.VotedNay => VotedNay,
            (int)VoterStatus.VotedPass => VotedPass,
            _ => throw new Exception("invalid voter status value")
        };
    }
}
