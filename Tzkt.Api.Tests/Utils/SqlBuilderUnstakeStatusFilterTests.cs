using Xunit;

namespace Tzkt.Api.Tests.Utils;

public class SqlBuilderUnstakeStatusFilterTests
{
    const string CycleCol = @"""Cycle""";
    const string RemainingAmountCol = @"""RemainingAmount""";
    const int UnfrozenCycle = 800;

    [Fact]
    public void FilterA_NullStatus_NoFilterAdded()
    {
        var builder = new SqlBuilder();
        builder.FilterA(CycleCol, RemainingAmountCol, null, UnfrozenCycle);
        Assert.Equal("", builder.Query);
    }

    [Fact]
    public void FilterA_EqPending_FiltersByCycleAboveUnfrozen()
    {
        var builder = new SqlBuilder();
        var status = new UnstakeRequestStatusParameter { Eq = "pending" };
        builder.FilterA(CycleCol, RemainingAmountCol, status, UnfrozenCycle);

        var query = builder.Query;
        Assert.Contains($"{CycleCol} > {UnfrozenCycle}", query);
    }

    [Fact]
    public void FilterA_EqFinalizable_UsesGreaterThanZero()
    {
        var builder = new SqlBuilder();
        var status = new UnstakeRequestStatusParameter { Eq = "finalizable" };
        builder.FilterA(CycleCol, RemainingAmountCol, status, UnfrozenCycle);

        var query = builder.Query;
        Assert.Contains($"{RemainingAmountCol} > 0", query);
        Assert.Contains($"{CycleCol} <= {UnfrozenCycle}", query);
        // Must NOT use != 0 (old broken behavior)
        Assert.DoesNotContain("!= 0", query);
    }

    [Fact]
    public void FilterA_EqFinalized_UsesLessThanOrEqualZero()
    {
        var builder = new SqlBuilder();
        var status = new UnstakeRequestStatusParameter { Eq = "finalized" };
        builder.FilterA(CycleCol, RemainingAmountCol, status, UnfrozenCycle);

        var query = builder.Query;
        Assert.Contains($"{RemainingAmountCol} <= 0", query);
        Assert.Contains($"{CycleCol} <= {UnfrozenCycle}", query);
        // Must NOT use = 0 (old broken behavior that missed negative values)
        Assert.DoesNotContain($"{RemainingAmountCol} = 0", query);
    }

    [Fact]
    public void FilterA_NeFinalizable_InvertsFinalizableCondition()
    {
        var builder = new SqlBuilder();
        var status = new UnstakeRequestStatusParameter { Ne = "finalizable" };
        builder.FilterA(CycleCol, RemainingAmountCol, status, UnfrozenCycle);

        var query = builder.Query;
        Assert.Contains($"{RemainingAmountCol} <= 0", query);
        Assert.Contains($"{CycleCol} > {UnfrozenCycle}", query);
    }

    [Fact]
    public void FilterA_NeFinalized_InvertsFinalizedCondition()
    {
        var builder = new SqlBuilder();
        var status = new UnstakeRequestStatusParameter { Ne = "finalized" };
        builder.FilterA(CycleCol, RemainingAmountCol, status, UnfrozenCycle);

        var query = builder.Query;
        Assert.Contains($"{RemainingAmountCol} > 0", query);
        Assert.Contains($"{CycleCol} > {UnfrozenCycle}", query);
    }

    [Fact]
    public void FilterA_NePending_FiltersByCycleAtOrBelowUnfrozen()
    {
        var builder = new SqlBuilder();
        var status = new UnstakeRequestStatusParameter { Ne = "pending" };
        builder.FilterA(CycleCol, RemainingAmountCol, status, UnfrozenCycle);

        var query = builder.Query;
        Assert.Contains($"{CycleCol} <= {UnfrozenCycle}", query);
    }
}
