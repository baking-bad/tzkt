using System;
using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class ProposalStatuses
    {
        public const string Active = "active";
        public const string Accepted = "accepted";
        public const string Rejected = "rejected";
        public const string Skipped = "skipped";

        public static string ToString(int value) => value switch
        {
            (int)ProposalStatus.Active => Active,
            (int)ProposalStatus.Accepted => Accepted,
            (int)ProposalStatus.Rejected => Rejected,
            (int)ProposalStatus.Skipped => Skipped,
            _ => throw new Exception("invalid proposal status value")
        };
    }
}
