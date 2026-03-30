using Xunit;

namespace Tzkt.Api.Tests.Utils;

public class SqlBuilderUnstakeStatusFilterTests
{
    const string CycleCol = @"""Cycle""";
    const string RemainingAmountCol = @"""RemainingAmount""";
    const string RoundingErrorCol = @"COALESCE(""RoundingError"", 0)";
    const int UnfrozenCycle = 800;

    [Fact]
    public void FilterA_NullStatus_NoFilterAdded()
    {
        var builder = new SqlBuilder();
        builder.FilterA(CycleCol, RemainingAmountCol, RoundingErrorCol, null, UnfrozenCycle);
        Assert.Equal("", builder.Query);
    }

    [Fact]
    public void FilterA_EqPending_FiltersByCycleAboveUnfrozen()
    {
        var builder = new SqlBuilder();
        var status = new UnstakeRequestStatusParameter { Eq = "pending" };
        builder.FilterA(CycleCol, RemainingAmountCol, RoundingErrorCol, status, UnfrozenCycle);

        var query = builder.Query;
        Assert.Contains($"{CycleCol} > {UnfrozenCycle}", query);
    }

    [Fact]
    public void FilterA_EqFinalizable_FiltersRemainingNotEqualRoundingError()
    {
        var builder = new SqlBuilder();
        var status = new UnstakeRequestStatusParameter { Eq = "finalizable" };
        builder.FilterA(CycleCol, RemainingAmountCol, RoundingErrorCol, status, UnfrozenCycle);

        var query = builder.Query;
        Assert.Contains($"{RemainingAmountCol} != {RoundingErrorCol}", query);
        Assert.Contains($"{CycleCol} <= {UnfrozenCycle}", query);
    }

    [Fact]
    public void FilterA_EqFinalized_FiltersRemainingEqualRoundingError()
    {
        var builder = new SqlBuilder();
        var status = new UnstakeRequestStatusParameter { Eq = "finalized" };
        builder.FilterA(CycleCol, RemainingAmountCol, RoundingErrorCol, status, UnfrozenCycle);

        var query = builder.Query;
        Assert.Contains($"{RemainingAmountCol} = {RoundingErrorCol}", query);
        Assert.Contains($"{CycleCol} <= {UnfrozenCycle}", query);
    }

    [Fact]
    public void FilterA_NeFinalizable_InvertsFinalizableCondition()
    {
        var builder = new SqlBuilder();
        var status = new UnstakeRequestStatusParameter { Ne = "finalizable" };
        builder.FilterA(CycleCol, RemainingAmountCol, RoundingErrorCol, status, UnfrozenCycle);

        var query = builder.Query;
        Assert.Contains($"{RemainingAmountCol} = {RoundingErrorCol}", query);
        Assert.Contains($"{CycleCol} > {UnfrozenCycle}", query);
    }

    [Fact]
    public void FilterA_NeFinalized_InvertsFinalizedCondition()
    {
        var builder = new SqlBuilder();
        var status = new UnstakeRequestStatusParameter { Ne = "finalized" };
        builder.FilterA(CycleCol, RemainingAmountCol, RoundingErrorCol, status, UnfrozenCycle);

        var query = builder.Query;
        Assert.Contains($"{RemainingAmountCol} != {RoundingErrorCol}", query);
        Assert.Contains($"{CycleCol} > {UnfrozenCycle}", query);
    }

    [Fact]
    public void FilterA_NePending_FiltersByCycleAtOrBelowUnfrozen()
    {
        var builder = new SqlBuilder();
        var status = new UnstakeRequestStatusParameter { Ne = "pending" };
        builder.FilterA(CycleCol, RemainingAmountCol, RoundingErrorCol, status, UnfrozenCycle);

        var query = builder.Query;
        Assert.Contains($"{CycleCol} <= {UnfrozenCycle}", query);
    }
}
