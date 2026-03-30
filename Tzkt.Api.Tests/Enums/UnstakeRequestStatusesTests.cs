using Xunit;

namespace Tzkt.Api.Tests.Enums;

public class UnstakeRequestStatusesTests
{
    [Theory]
    [InlineData(100, 0)]
    [InlineData(100, 1000)]
    [InlineData(0, 1000)]
    public void ToString_CycleAboveUnfrozen_ReturnsPending(long remainingAmount, int unfrozenCycle)
    {
        var result = UnstakeRequestStatuses.ToString(cycle: 1001, remainingAmount, roundingError: 0, unfrozenCycle);
        Assert.Equal("pending", result);
    }

    [Theory]
    [InlineData(1000, 0)]
    [InlineData(100, 1)]
    [InlineData(5, -2)]
    public void ToString_RemainingDiffersFromRoundingError_ReturnsFinalizable(
        long remainingAmount, long roundingError)
    {
        var result = UnstakeRequestStatuses.ToString(cycle: 739, remainingAmount, roundingError, unfrozenCycle: 800);
        Assert.Equal("finalizable", result);
    }

    [Fact]
    public void ToString_ZeroRemainingZeroRoundingError_ReturnsFinalized()
    {
        var result = UnstakeRequestStatuses.ToString(cycle: 739, remainingAmount: 0, roundingError: 0, unfrozenCycle: 800);
        Assert.Equal("finalized", result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-2)]
    public void ToString_RemainingEqualsRoundingError_ReturnsFinalized(long value)
    {
        // When remainingAmount == roundingError, the "remaining" is purely a rounding artifact.
        // Nothing real to finalize.
        var result = UnstakeRequestStatuses.ToString(cycle: 739, remainingAmount: value, roundingError: value, unfrozenCycle: 800);
        Assert.Equal("finalized", result);
    }

    [Fact]
    public void ToString_RealWorldRoundingErrorCase()
    {
        // Real mainnet case: baker tz1bZ8vsMAXmaWEV7FRnyhcuUs2fYMaQ6Hkk, unstake request id=9761
        // RequestedAmount=694905295, SlashedAmount=694905295, RoundingError=1
        // With correct formula (+ RoundingError): ActualAmount = 1, RemainingAmount = 1
        // Baker aggregate raw context (unstaked_frozen_deposits) shows actual_amount=1,
        // but staker-level endpoint shows amount=0.
        // This 1 mutez is a rounding artifact — can't be burned or withdrawn.
        var result = UnstakeRequestStatuses.ToString(cycle: 739, remainingAmount: 1, roundingError: 1, unfrozenCycle: 800);
        Assert.Equal("finalized", result);
    }
}
