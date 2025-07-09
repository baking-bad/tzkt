namespace Tzkt.Data.Models
{
    [Flags]
    public enum BlockEvents
    {
        None                            = 0b_0000_0000_0000_0000,
        CycleBegin                      = 0b_0000_0000_0000_0001,
        CycleEnd                        = 0b_0000_0000_0000_0010,
        ProtocolBegin                   = 0b_0000_0000_0000_0100,
        ProtocolEnd                     = 0b_0000_0000_0000_1000,
        Deactivations                   = 0b_0000_0000_0001_0000,
        NewAccounts                     = 0b_0000_0000_0010_0000,
        BalanceSnapshot                 = 0b_0000_0000_0100_0000,
        Bigmaps                         = 0b_0000_0000_1000_0000,
        Tokens                          = 0b_0000_0001_0000_0000,
        Events                          = 0b_0000_0010_0000_0000,
        Tickets                         = 0b_0000_0100_0000_0000,
        DoubleBakingSlashing            = 0b_0000_1000_0000_0000,
        DoubleConsensusSlashing         = 0b_0001_0000_0000_0000,
        DelegationSnapshot              = 0b_0010_0000_0000_0000
    }
}
