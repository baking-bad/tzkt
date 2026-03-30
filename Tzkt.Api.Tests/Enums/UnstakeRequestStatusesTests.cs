using Xunit;

namespace Tzkt.Api.Tests.Enums;

public class UnstakeRequestStatusesTests
{
    [Theory]
    [InlineData(1001, 0, null)]
    [InlineData(1001, 1, null)]
    [InlineData(1001, 1, 1L)]
    [InlineData(1001, 2, 1L)]
    [InlineData(1001, 2, -1L)]
    public void ToString_ReturnsPending(int cycle, long remainingAmount, long? roundingError)
    {
        var result = UnstakeRequestStatuses.ToString(cycle, remainingAmount, roundingError, unfrozenCycle: 1000);
        Assert.Equal("pending", result);
    }

    [Theory]
    [InlineData(999, 1, null)]
    [InlineData(999, 1, 0L)]
    [InlineData(999, 2, 1L)]
    [InlineData(999, 2, -1L)]
    [InlineData(1000, 1, null)]
    [InlineData(1000, 1, 0L)]
    [InlineData(1000, 2, 1L)]
    [InlineData(1000, 2, -1L)]
    public void ToString_ReturnsFinalizable(int cycle, long remainingAmount, long? roundingError)
    {
        var result = UnstakeRequestStatuses.ToString(cycle, remainingAmount, roundingError, unfrozenCycle: 1000);
        Assert.Equal("finalizable", result);
    }

    [Theory]
    [InlineData(999, 0, null)]
    [InlineData(999, 1, 1L)]
    [InlineData(1000, 0, null)]
    [InlineData(1000, 1, 1L)]
    public void ToString_ReturnsFinalized(int cycle, long remainingAmount, long? roundingError)
    {
        var result = UnstakeRequestStatuses.ToString(cycle, remainingAmount, roundingError, unfrozenCycle: 1000);
        Assert.Equal("finalized", result);
    }
}
