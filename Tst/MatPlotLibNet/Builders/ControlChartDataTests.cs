// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>What each kind of control chart plots and how it estimates its limits: one factory of
/// <see cref="ControlChartData"/> for each kind. The charts are drawn by <c>ShewhartChart</c>; these tests read the points and
/// the limits off the drawn parts, whose reference lines tell the limits the chart was drawn with.</summary>
public class ControlChartDataTests
{
    /// <summary>Five values: the moving ranges 2, 1, 4, 2 average 9 / 4 = 2.25.</summary>
    private static readonly double[] Five = [10, 12, 11, 15, 13];

    private static readonly double[] Days = [1, 2, 3, 4, 5];

    private static ControlChartParts Drawn(ControlChartData data, ControlRules rules = ControlRules.Nelson1, ControlLimits? limits = null)
    {
        ControlChartParts parts = default;
        Plt.Create().AddSubPlot(1, 1, 1, ax => ax.ShewhartChart(data, rules, limits, p => parts = p)).Build();
        return parts;
    }

    // ---- the moving range chart ---------------------------------------------------------------------------------------------

    /// <summary>The moving range chart plots the distance between each value and the one before it, at the position of the later
    /// of the two: 2, 1, 4 and 2 at positions 2 to 5.</summary>
    [Fact]
    public void TheMovingRange_IsPlotted_AtThePositionOfTheLaterValue()
    {
        var parts = Drawn(ControlChartData.MovingRange(Days, Five));

        Assert.Equal([2.0, 3.0, 4.0, 5.0], parts.Values.XData);
        Assert.Equal([2.0, 1.0, 4.0, 2.0], parts.Values.YData);
    }

    /// <summary>The centre is the average moving range, 9 / 4; the upper limit is D4 = 3.267 times it, the lower limit 0 (D3 is
    /// 0 for ranges of two: a range cannot be negative).</summary>
    [Fact]
    public void TheMovingRangeLimits_AreTheMeanAndD4TimesIt_WithTheLowerAtZero()
    {
        var parts = Drawn(ControlChartData.MovingRange(Days, Five));

        Assert.Equal(2.25, parts.Center.Value);
        Assert.Equal(3.267 * 2.25, parts.Upper.Value);
        Assert.Equal(0.0, parts.Lower.Value);
    }

    /// <summary>The moving range chart and the individuals chart of the same values estimate their limits from ONE average
    /// moving range: the individuals chart's sigma is that average over d2 = 1.128.</summary>
    [Fact]
    public void TheMovingRangeAndTheIndividualsChart_ShareOneAverageMovingRange()
    {
        var moving = Drawn(ControlChartData.MovingRange(Days, Five));
        var individuals = Drawn(ControlChartData.Individuals(Days, Five));

        double sigma = (individuals.Upper.Value - individuals.Center.Value) / 3;
        Assert.Equal(moving.Center.Value, sigma * 1.128, 12);
    }

    /// <summary>A missing value (NaN) is left out and the range spans its gap, as the individuals chart's estimate does: 10, 12
    /// and 14 around a gap are ranges of 2 and 2, at the positions of 12 and 14.</summary>
    [Fact]
    public void AMissingValue_IsLeftOut_AndTheRangeSpansItsGap()
    {
        var parts = Drawn(ControlChartData.MovingRange([1, 2, 3, 4], [10, double.NaN, 12, 14]));

        Assert.Equal([3.0, 4.0], parts.Values.XData);
        Assert.Equal([2.0, 2.0], parts.Values.YData);
        Assert.Equal(2.0, parts.Center.Value);
    }

    /// <summary>A jump larger than the limit allows is flagged by rule 1: the third range here (a step of 12 among steps of 1)
    /// stands above 3.267 times the average.</summary>
    [Fact]
    public void ABigJump_IsBeyondTheUpperLimit()
    {
        var parts = Drawn(ControlChartData.MovingRange([1, 2, 3, 4, 5, 6, 7, 8, 9, 10], [10, 11, 10, 11, 10, 22, 21, 22, 21, 22]));

        Assert.Equal([6.0], parts.Violations.XData);
        Assert.Equal([12.0], parts.Violations.YData);
    }

    /// <summary>Limits that are given are drawn as given, and the points are the moving ranges all the same.</summary>
    [Fact]
    public void GivenLimits_AreDrawnAsGiven()
    {
        var parts = Drawn(ControlChartData.MovingRange(Days, Five), limits: new ControlLimits(2, 0, 6));

        Assert.Equal(6.0, parts.Upper.Value);
        Assert.Equal([2.0, 1.0, 4.0, 2.0], parts.Values.YData);
    }

    [Fact]
    public void AMovingRange_NeedsTwoValuesThatCanBeDrawn_EvenWithLimits()
    {
        var error = Assert.Throws<ArgumentException>(() => ControlChartData.MovingRange([1, 2], [4.2, double.NaN]));

        Assert.Equal("values", error.ParamName);
        Assert.Equal("The sample has 1 observation that can be drawn; a moving range chart is drawn from 2 or more (NaN and the infinities are left out). (Parameter 'values')", error.Message);
    }

    [Fact]
    public void TheMovingRangeFactory_RefusesWhatTheIndividualsFactoryRefuses()
    {
        Assert.Equal("x", Assert.Throws<ArgumentNullException>(() => ControlChartData.MovingRange(null!, Five)).ParamName);
        Assert.Equal("values", Assert.Throws<ArgumentNullException>(() => ControlChartData.MovingRange(Days, null!)).ParamName);
        Assert.Equal("values", Assert.Throws<ArgumentException>(() => ControlChartData.MovingRange([1, 2, 3], [1, 2])).ParamName);
    }

    // ---- the X-bar and range charts -----------------------------------------------------------------------------------------

    /// <summary>Four subgroups of three: means 4, 5, 4 and 7 (grand mean 5), ranges 4, 4, 0 and 4 (average 3).</summary>
    private static readonly double[][] Subgroups = [[2, 4, 6], [3, 5, 7], [4, 4, 4], [5, 7, 9]];

    /// <summary>A2 and D4 for subgroups of three, to the five decimals of the table.</summary>
    private const double A2OfThree = 1.02333;
    private const double D4OfThree = 2.57459;

    /// <summary>The X-bar chart plots the mean of each subgroup at the subgroup's number from 1; the centre is the grand mean
    /// and the limits stand A2 times the average range either side of it.</summary>
    [Fact]
    public void TheXBar_PlotsTheSubgroupMeans_WithLimitsA2TimesTheAverageRangeFromTheGrandMean()
    {
        var parts = Drawn(ControlChartData.XBar(Subgroups));

        Assert.Equal([1.0, 2.0, 3.0, 4.0], parts.Values.XData);
        Assert.Equal([4.0, 5.0, 4.0, 7.0], parts.Values.YData);
        Assert.Equal(5.0, parts.Center.Value);
        Assert.Equal(5 + A2OfThree * 3, parts.Upper.Value, 12);
        Assert.Equal(5 - A2OfThree * 3, parts.Lower.Value, 12);
    }

    /// <summary>The range chart plots the range of each subgroup; the centre is the average range, the limits D4 and D3 times it:
    /// for subgroups of three D3 is 0, so the lower limit is 0.</summary>
    [Fact]
    public void TheRange_PlotsTheSubgroupRanges_WithLimitsD4AndD3TimesTheAverageRange()
    {
        var parts = Drawn(ControlChartData.Range(Subgroups));

        Assert.Equal([1.0, 2.0, 3.0, 4.0], parts.Values.XData);
        Assert.Equal([4.0, 4.0, 0.0, 4.0], parts.Values.YData);
        Assert.Equal(3.0, parts.Center.Value);
        Assert.Equal(D4OfThree * 3, parts.Upper.Value, 12);
        Assert.Equal(0.0, parts.Lower.Value);
    }

    /// <summary>For subgroups of 7 or more D3 is above 0 and the range chart's lower limit stands there: for subgroups of seven
    /// with an average range of 5, 0.07571 x 5.</summary>
    [Fact]
    public void TheRange_OfSubgroupsOfSeven_HasALowerLimitAboveZero()
    {
        double[][] sevens = [[0, 1, 2, 3, 4, 5, 6], [10, 11, 12, 13, 14, 12, 12]];   // ranges 6 and 4: average 5
        var parts = Drawn(ControlChartData.Range(sevens));

        Assert.Equal(5.0, parts.Center.Value);
        Assert.Equal(0.07571 * 5, parts.Lower.Value, 12);
        Assert.Equal(1.92429 * 5, parts.Upper.Value, 12);
    }

    /// <summary>A subgroup with a missing observation (NaN) or an infinity is missing as a whole: its point is a gap in the line
    /// and it is left out of both averages.</summary>
    [Fact]
    public void ASubgroupWithAMissingObservation_IsMissing_AndLeftOutOfTheEstimate()
    {
        var xbar = Drawn(ControlChartData.XBar([[2, 4, 6], [3, double.NaN, 7], [4, 4, 4]]));
        var range = Drawn(ControlChartData.Range([[2, 4, 6], [3, double.PositiveInfinity, 7], [4, 4, 4]]));

        Assert.Equal(4.0, xbar.Values.YData[0]);
        Assert.True(double.IsNaN(xbar.Values.YData[1]));
        Assert.Equal(4.0, xbar.Center.Value);
        Assert.Equal(4 + A2OfThree * 2, xbar.Upper.Value, 12);
        Assert.True(double.IsNaN(range.Values.YData[1]));
        Assert.Equal(2.0, range.Center.Value);
        Assert.Empty(xbar.Violations.XData);
    }

    /// <summary>The same subgroups give the X-bar and range charts limits from ONE average range.</summary>
    [Fact]
    public void TheXBarAndRangeCharts_ShareOneAverageRange()
    {
        var xbar = Drawn(ControlChartData.XBar(Subgroups));
        var range = Drawn(ControlChartData.Range(Subgroups));

        Assert.Equal(range.Center.Value, (xbar.Upper.Value - xbar.Center.Value) / A2OfThree, 12);
    }

    /// <summary>Limits that are given are drawn as given, and the points are the statistic all the same.</summary>
    [Fact]
    public void GivenLimits_AreDrawnAsGiven_ForSubgroups()
    {
        var parts = Drawn(ControlChartData.XBar(Subgroups), limits: new ControlLimits(5, 3, 7));

        Assert.Equal(7.0, parts.Upper.Value);
        Assert.Equal([4.0, 5.0, 4.0, 7.0], parts.Values.YData);
    }

    /// <summary>A subgroup of one has no range and one past ten has no constants (the table stops at ten): both are refused,
    /// naming the subgroups.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(11)]
    public void SubgroupsOfASizeWithoutConstants_AreRefused(int size)
    {
        double[][] subgroups = [[.. Enumerable.Range(0, size).Select(i => (double)i)], [.. Enumerable.Range(0, size).Select(i => i + 1.0)]];

        var xbar = Assert.Throws<ArgumentException>(() => ControlChartData.XBar(subgroups));
        var range = Assert.Throws<ArgumentException>(() => ControlChartData.Range(subgroups));

        Assert.Equal("subgroups", xbar.ParamName);
        Assert.Equal("subgroups", range.ParamName);
        Assert.StartsWith($"The subgroups hold {size} observation{(size == 1 ? "" : "s")} each; the constants of a control chart for subgroups are tabulated for 2 to 10.", xbar.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SubgroupsOfUnequalSize_AreRefused_NamingTheOffender()
    {
        var error = Assert.Throws<ArgumentException>(() => ControlChartData.XBar([[1, 2, 3], [1, 2, 3], [1, 2, 3, 4]]));

        Assert.Equal("subgroups", error.ParamName);
        Assert.StartsWith("Subgroup 3 holds 4 observations and subgroup 1 holds 3 observations; the limits of the chart are for one subgroup size.", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NoSubgroups_AMissingSubgroup_OrNoCompleteOne_AreRefused()
    {
        Assert.Equal("subgroups", Assert.Throws<ArgumentNullException>(() => ControlChartData.XBar(null!)).ParamName);
        Assert.StartsWith("No subgroups were given;", Assert.Throws<ArgumentException>(() => ControlChartData.Range([])).Message, StringComparison.Ordinal);
        Assert.StartsWith("Subgroup 2 is missing (null);", Assert.Throws<ArgumentException>(() => ControlChartData.XBar([[1, 2], null!])).Message, StringComparison.Ordinal);
        var none = Assert.Throws<ArgumentException>(() => ControlChartData.XBar([[1, double.NaN], [double.NaN, 2]]));
        Assert.Equal("subgroups", none.ParamName);
        Assert.StartsWith("No subgroup has all its observations;", none.Message, StringComparison.Ordinal);
    }

    // ---- the p chart ----------------------------------------------------------------------------------------------------------

    /// <summary>Six samples of 100 with 4, 6, 5, 9, 3 and 5 defectives: proportions 0.04 ... 0.05; the centre is the pooled
    /// proportion 32 / 600; sigma is sqrt(p (1 - p) / n) = 0.02247; the upper limit is p + 3 sigma = 0.12074, and the lower limit,
    /// which would be -0.0141, stands at 0 (a proportion cannot be negative).</summary>
    [Fact]
    public void TheP_PlotsTheProportions_WithBinomialLimits_TheLowerAtZeroWhenItWouldBeNegative()
    {
        var parts = Drawn(ControlChartData.P([4, 6, 5, 9, 3, 5], 100));

        Assert.Equal([0.04, 0.06, 0.05, 0.09, 0.03, 0.05], parts.Values.YData);
        Assert.Equal(32.0 / 600, parts.Center.Value, 15);
        Assert.Equal(0.12074253, parts.Upper.Value, 8);
        Assert.Equal(0.0, parts.Lower.Value);
    }

    /// <summary>A pooled proportion far enough from 0 keeps its lower limit: five samples of 100 around 22 defectives.</summary>
    [Fact]
    public void TheP_KeepsALowerLimitAbove0_WhenThereIsOne()
    {
        var parts = Drawn(ControlChartData.P([20, 24, 18, 22, 26], 100));

        Assert.Equal(0.22, parts.Center.Value, 15);
        Assert.Equal(0.22 + 3 * Math.Sqrt(0.22 * 0.78 / 100), parts.Upper.Value, 15);
        Assert.Equal(0.22 - 3 * Math.Sqrt(0.22 * 0.78 / 100), parts.Lower.Value, 15);
        Assert.True(parts.Lower.Value > 0);
    }

    /// <summary>A sample with no result (NaN) is a gap and is left out of the pooled proportion.</summary>
    [Fact]
    public void TheP_LeavesOutASampleWithNoResult()
    {
        var parts = Drawn(ControlChartData.P([20, double.NaN, 24], 100));

        Assert.True(double.IsNaN(parts.Values.YData[1]));
        Assert.Equal(0.22, parts.Center.Value, 15);
    }

    [Fact]
    public void TheP_RefusesWhatItCannotMakeAProportionOf()
    {
        Assert.Equal("defectives", Assert.Throws<ArgumentNullException>(() => ControlChartData.P(null!, 100)).ParamName);
        Assert.Equal("sampleSize", Assert.Throws<ArgumentOutOfRangeException>(() => ControlChartData.P([1, 2], 0)).ParamName);
        var over = Assert.Throws<ArgumentException>(() => ControlChartData.P([4, 120, 5], 100));
        Assert.Equal("defectives", over.ParamName);
        Assert.StartsWith("Sample 2 has 120 defectives; a sample of 100 has between 0 and 100.", over.Message, StringComparison.Ordinal);
        Assert.StartsWith("Sample 1 has -1 defectives;", Assert.Throws<ArgumentException>(() => ControlChartData.P([-1, 2], 100)).Message, StringComparison.Ordinal);
        var none = Assert.Throws<ArgumentException>(() => ControlChartData.P([double.NaN], 100));
        Assert.StartsWith("No sample has a result;", none.Message, StringComparison.Ordinal);
    }

    // ---- the c chart ----------------------------------------------------------------------------------------------------------

    /// <summary>Eight counts averaging 35 / 8 = 4.375: sigma is sqrt(4.375) = 2.0917, the upper limit 4.375 + 6.2750 = 10.650, and
    /// the lower, which would be -1.900, stands at 0.</summary>
    [Fact]
    public void TheC_PlotsTheCounts_WithPoissonLimits_TheLowerAtZeroWhenItWouldBeNegative()
    {
        var parts = Drawn(ControlChartData.C([3, 5, 2, 7, 4, 6, 3, 5]));

        Assert.Equal([1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0], parts.Values.XData);
        Assert.Equal([3.0, 5.0, 2.0, 7.0, 4.0, 6.0, 3.0, 5.0], parts.Values.YData);
        Assert.Equal(4.375, parts.Center.Value);
        Assert.Equal(4.375 + 3 * Math.Sqrt(4.375), parts.Upper.Value, 15);
        Assert.Equal(10.650, parts.Upper.Value, 3);
        Assert.Equal(0.0, parts.Lower.Value);
    }

    [Fact]
    public void TheC_KeepsALowerLimitAbove0_WhenThereIsOne()
    {
        var parts = Drawn(ControlChartData.C([18, 22, 20, 19, 21]));

        Assert.Equal(20.0, parts.Center.Value);
        Assert.Equal(20 - 3 * Math.Sqrt(20), parts.Lower.Value, 15);
        Assert.True(parts.Lower.Value > 0);
    }

    [Fact]
    public void TheC_LeavesOutAMissingCount_AndRefusesWhatIsNotACount()
    {
        var gap = Drawn(ControlChartData.C([4, double.NaN, 6]));

        Assert.True(double.IsNaN(gap.Values.YData[1]));
        Assert.Equal(5.0, gap.Center.Value);
        Assert.Equal("counts", Assert.Throws<ArgumentNullException>(() => ControlChartData.C(null!)).ParamName);
        var negative = Assert.Throws<ArgumentException>(() => ControlChartData.C([3, -1, 4]));
        Assert.Equal("counts", negative.ParamName);
        Assert.StartsWith("Sample 2 has a count of -1; a count is a finite number of 0 or more.", negative.Message, StringComparison.Ordinal);
        Assert.StartsWith("No sample has a count;", Assert.Throws<ArgumentException>(() => ControlChartData.C([double.NaN])).Message, StringComparison.Ordinal);
    }

    /// <summary>A count of 0 sits ON a lower limit of 0, which is not beyond it (a limit is not a violation).</summary>
    [Fact]
    public void AValueOnALowerLimitAt0_IsNotFlagged()
    {
        var parts = Drawn(ControlChartData.C([3, 0, 4, 5, 3, 4]));

        Assert.Empty(parts.Violations.XData);
    }
}
