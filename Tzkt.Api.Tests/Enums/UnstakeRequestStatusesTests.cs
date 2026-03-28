using Xunit;

namespace Tzkt.Api.Tests.Enums;

public class UnstakeRequestStatusesTests
{
    [Theory]
    [InlineData(100, 0)]
    [InlineData(100, 1000)]
    [InlineData(100, -1)]
    public void ToString_CycleAboveUnfrozen_ReturnsPending(long remainingAmount, int unfrozenCycle)
    {
        var result = UnstakeRequestStatuses.ToString(cycle: 1001, remainingAmount, unfrozenCycle: unfrozenCycle);
        Assert.Equal("pending", result);
    }

    [Theory]
    [InlineData(1, 1000, 1000)]
    [InlineData(100, 999, 1000)]
    [InlineData(1000000, 0, 1000)]
    public void ToString_CycleAtOrBelowUnfrozen_PositiveRemaining_ReturnsFinalizable(
        long remainingAmount, int cycle, int unfrozenCycle)
    {
        var result = UnstakeRequestStatuses.ToString(cycle, remainingAmount, unfrozenCycle);
        Assert.Equal("finalizable", result);
    }

    [Fact]
    public void ToString_CycleAtOrBelowUnfrozen_ZeroRemaining_ReturnsFinalized()
    {
        var result = UnstakeRequestStatuses.ToString(cycle: 739, remainingAmount: 0, unfrozenCycle: 800);
        Assert.Equal("finalized", result);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ToString_CycleAtOrBelowUnfrozen_NegativeRemaining_ReturnsFinalized(long remainingAmount)
    {
        // Key test case: negative remainingAmount due to Tezos node rounding error
        // in balance_updates vs context state. Must be "finalized", not "finalizable".
        var result = UnstakeRequestStatuses.ToString(cycle: 739, remainingAmount, unfrozenCycle: 800);
        Assert.Equal("finalized", result);
    }

    [Fact]
    public void ToString_RealWorldRoundingErrorCase()
    {
        // Real mainnet case: baker tz1bZ8vsMAXmaWEV7FRnyhcuUs2fYMaQ6Hkk, unstake request id=9761
        // RequestedAmount=694905295, SlashedAmount=694905295, RoundingError=1
        // With old formula (- RoundingError): ActualAmount = -1, was incorrectly "finalizable"
        // RPC node confirms amount = 0 after slashing — nothing to finalize.
        long remainingAmount = -1;
        var result = UnstakeRequestStatuses.ToString(cycle: 739, remainingAmount, unfrozenCycle: 800);
        Assert.Equal("finalized", result);
    }
}
