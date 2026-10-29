// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;
using MatPlotLibNet.Models.Series;

/// <summary>The log-rank test of k survival groups (the Mantel-Cox form of R's <c>survdiff</c> with rho = 0, lifelines'
/// <c>multivariate_logrank_test</c>): at every time an event happens, each group's expected events are its share of the subjects
/// at risk times the events, the variance is the hypergeometric one with the correction for ties, and the statistic is the
/// quadratic form of the observed minus expected events of all groups but the last, with k - 1 degrees of freedom. The
/// reference statistics are lifelines' (0.30.3), to 1e-9; the p-values come from the chi-square survival function.</summary>
public class LogRankTests
{
    private static void Close(double expected, double actual, double relative) =>
        Assert.True(Math.Abs(actual - expected) <= relative * Math.Abs(expected), $"expected {expected:R}, got {actual:R}");

    /// <summary>Freireich's leukaemia trial: 6-MP against placebo, 21 patients each, 12 of the 6-MP group censored. R's survdiff
    /// gives chi-square 16.8 on 1 degree of freedom, p = 4e-05.</summary>
    [Fact]
    public void Freireich_SixMercaptopurineAgainstPlacebo_MatchesLifelinesAndR()
    {
        SurvivalGroup[] groups =
        [
            new("6-MP", [6.0, 6.0, 6.0, 6.0, 7.0, 9.0, 10.0, 10.0, 11.0, 13.0, 16.0, 17.0, 19.0, 20.0, 22.0, 23.0, 25.0, 32.0, 32.0, 34.0, 35.0], [true, true, true, false, true, false, true, false, false, true, true, false, false, false, true, true, false, false, false, false, false]),
            new("Placebo", [1.0, 1.0, 2.0, 2.0, 3.0, 4.0, 4.0, 5.0, 5.0, 8.0, 8.0, 8.0, 8.0, 11.0, 11.0, 12.0, 12.0, 15.0, 17.0, 22.0, 23.0], [true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true])
        ];

        var result = LogRank.Test(groups);

        Close(16.792940989216547, result.Statistic, 1e-9);
        Assert.Equal(1, result.DegreesOfFreedom);
        Close(4.168809109334511e-05, result.PValue, 1e-6);
        Assert.Equal(16.79, result.Statistic, 2);
    }

    /// <summary>Three groups with many ties, and censorings among them: the statistic has 2 degrees of freedom.</summary>
    [Fact]
    public void ThreeGroupsWithTies_MatchLifelines()
    {
        SurvivalGroup[] groups =
        [
            new("G1", [5.0, 10.0, 7.0, 4.0, 4.0, 8.0, 8.0, 10.0], [true, true, true, true, true, true, false, true]),
            new("G2", [12.0, 8.0, 11.0, 7.0, 4.0, 6.0, 9.0, 9.0, 8.0], [true, true, false, true, true, false, true, true, false]),
            new("G3", [1.0, 2.0, 1.0, 1.0, 7.0, 8.0, 8.0], [true, false, false, true, true, true, true])
        ];

        var result = LogRank.Test(groups);

        Close(4.981659715785285, result.Statistic, 1e-9);
        Assert.Equal(2, result.DegreesOfFreedom);
        Close(0.08284119162569575, result.PValue, 1e-6);
    }

    /// <summary>A pair small enough to work by hand, with a tie and a subject censored on an event time (who is still at risk
    /// there).</summary>
    [Fact]
    public void ASmallPairWithATieAndACensoringOnAnEventTime_MatchesLifelines()
    {
        SurvivalGroup[] groups =
        [
            new("A", [2.0, 3.0, 3.0, 5.0], [true, true, false, true]),
            new("B", [1.0, 3.0, 4.0, 6.0], [true, true, true, false])
        ];

        var result = LogRank.Test(groups);

        Close(0.006634599436059062, result.Statistic, 1e-9);
        Close(0.9350816610273595, result.PValue, 1e-6);
    }

    /// <summary>The order of the groups and of the subjects within them does not change the statistic.</summary>
    [Fact]
    public void TheStatistic_DoesNotDependOnTheOrder()
    {
        SurvivalGroup[] groups =
        [
            new("A", [2, 3, 3, 5], [true, true, false, true]),
            new("B", [1, 3, 4, 6], [true, true, true, false]),
            new("C", [2, 2, 7, 8], [true, false, true, true]),
        ];
        var shuffled = new[] { groups[2], groups[0], groups[1] };
        shuffled[0] = new("C", [8, 7, 2, 2], [true, true, false, true]);

        Close(LogRank.Test(groups).Statistic, LogRank.Test(shuffled).Statistic, 1e-12);
    }

    [Fact]
    public void GroupsThatDoNotDiffer_HaveAStatisticOfZero_AndAPOfOne()
    {
        SurvivalGroup[] groups =
        [
            new("A", [1, 2, 3, 4], [true, true, true, true]),
            new("B", [1, 2, 3, 4], [true, true, true, true]),
        ];

        var result = LogRank.Test(groups);

        Assert.Equal(0, result.Statistic, 12);
        Assert.Equal(1, result.PValue, 12);
    }

    /// <summary>With no event there is nothing to compare: no statistic, and no p.</summary>
    [Fact]
    public void WithNoEvents_ThereIsNoStatistic()
    {
        SurvivalGroup[] groups =
        [
            new("A", [1, 2], [false, false]),
            new("B", [3, 4], [false, false]),
        ];

        var result = LogRank.Test(groups);

        Assert.True(double.IsNaN(result.Statistic));
        Assert.True(double.IsNaN(result.PValue));
        Assert.Equal(1, result.DegreesOfFreedom);
    }

    [Fact]
    public void OneGroup_HasNothingToBeComparedWith()
    {
        var result = LogRank.Test([new("A", [1, 2, 3], [true, true, false])]);

        Assert.True(double.IsNaN(result.Statistic));
        Assert.True(double.IsNaN(result.PValue));
        Assert.Equal(0, result.DegreesOfFreedom);
    }

    /// <summary>The number at risk at a time is the subjects whose duration is that time or later - the one count the curve, its
    /// table and the test all read.</summary>
    [Theory]
    [InlineData(0.0, 4)]
    [InlineData(2.0, 4)]
    [InlineData(3.0, 3)]
    [InlineData(3.5, 1)]
    [InlineData(5.0, 1)]
    [InlineData(5.1, 0)]
    public void TheNumberAtRisk_IsTheSubjectsFollowedToThatTimeOrLonger(double time, int expected)
    {
        var group = new SurvivalGroup("A", [2, 3, 3, 5], [true, true, false, true]);

        Assert.Equal(expected, group.AtRiskAt(time));
    }

    /// <summary>The curve counts its risk set by a running total; the extension counts it afresh. They are the same number.</summary>
    [Fact]
    public void TheNumberAtRisk_IsTheOneTheCurveEstimatesWith()
    {
        var group = new SurvivalGroup("A", [2, 3, 3, 5, 5, 8, 9], [true, true, false, true, false, true, true]);
        var curve = group.ProductLimit("group");

        for (int i = 0; i < curve.Times.Length; i++)
            if (curve.Events[i] > 0) Assert.Equal(curve.AtRisk[i], group.AtRiskAt(curve.Times[i]));
    }
}
