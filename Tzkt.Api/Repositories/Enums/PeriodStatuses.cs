using System;
using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class PeriodStatuses
    {
        public const string Active = "active";
        public const string NoProposals = "no_proposals";
        public const string NoQuorum = "no_quorum";
        public const string NoSupermajority = "no_supermajority";
        public const string NoSingleWinner = "no_single_winner";
        public const string Success = "success";

        public static string ToString(int value) => value switch
        {
            (int)PeriodStatus.Active => Active,
            (int)PeriodStatus.NoProposals => NoProposals,
            (int)PeriodStatus.NoQuorum => NoQuorum,
            (int)PeriodStatus.NoSupermajority => NoSupermajority,
            (int)PeriodStatus.NoSingleWinner => NoSingleWinner,
            (int)PeriodStatus.Success => Success,
            _ => throw new Exception("invalid period status value")
        };
    }
}
