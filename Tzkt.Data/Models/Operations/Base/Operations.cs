using System;

namespace Tzkt.Data.Models
{
    [Flags]
    public enum Operations
    {
        None                = 0b_0000_0000_0000_0000,

        Endorsements        = 0b_0000_0000_0000_0001,

        Ballots             = 0b_0000_0000_0000_0010,
        Proposals           = 0b_0000_0000_0000_0100,

        Activations         = 0b_0000_0000_0000_1000,
        DoubleBakings       = 0b_0000_0000_0001_0000,
        DoubleEndorsings    = 0b_0000_0000_0010_0000,
        Revelations         = 0b_0000_0000_0100_0000,

        Transactions        = 0b_0000_0000_1000_0000,
        Delegations         = 0b_0000_0001_0000_0000,
        Originations        = 0b_0000_0010_0000_0000,
        Reveals             = 0b_0000_0100_0000_0000,

        Migrations          = 0b_0000_1000_0000_0000,
        RevelationPenalty   = 0b_0001_0000_0000_0000,
        Baking              = 0b_0010_0000_0000_0000,

        RegisterConstant    = 0b_0100_0000_0000_0000,

        EndorsingRewards    = 0b_1000_0000_0000_0000,
    }
}
