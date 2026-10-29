// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>The eight tests for a process out of control (Nelson 1984, "The Shewhart control chart: tests for special causes"),
/// each judged at every point from the points up to it: the point that COMPLETES a pattern is flagged, and every later point
/// that keeps it. Every case below is a hand-made array with the limits 0 / -3 / 3, so sigma is exactly 1 and the zones are
/// plain numbers: zone C is within 1 of the centre, B between 1 and 2, A beyond 2. A run is only judged by the points that
/// exist: at the start of the data a run that needs nine points cannot have happened yet, while two of three beyond 2 sigma has
/// already happened when the first two are.</summary>
public class NelsonRulesTests
{
    private static readonly ControlLimits Limits = new(0, -3, 3);

    private readonly record struct Case(ControlRules Rule, double[] Values, int[] Flagged);

    private static double[] Repeat(double value, int count) => [.. Enumerable.Repeat(value, count)];

    private static double[] Alternating(double high, double low, int count) => [.. Enumerable.Range(0, count).Select(i => i % 2 == 0 ? high : low)];

    private static readonly Dictionary<string, Case> Cases = new()
    {
        // Rule 1: a point strictly beyond a control limit.
        ["1: beyond the upper limit"] = new(ControlRules.Nelson1, [0, 3.5, 0], [1]),
        ["1: beyond the lower limit"] = new(ControlRules.Nelson1, [-3.0001, 0], [0]),
        ["1: on a limit is not beyond"] = new(ControlRules.Nelson1, [3.0, -3.0, 2.9], []),

        // Rule 2: nine points in a row on the same side of the centre line.
        ["2: nine above"] = new(ControlRules.Nelson2, Repeat(0.5, 9), [8]),
        ["2: nine below"] = new(ControlRules.Nelson2, Repeat(-0.5, 9), [8]),
        ["2: eight above are not enough"] = new(ControlRules.Nelson2, Repeat(0.5, 8), []),
        ["2: ten above keep it"] = new(ControlRules.Nelson2, Repeat(0.5, 10), [8, 9]),
        ["2: a point on the centre breaks the run"] = new(ControlRules.Nelson2, [0.5, 0.5, 0.5, 0.5, 0, 0.5, 0.5, 0.5, 0.5], []),
        ["2: a point on the other side breaks the run"] = new(ControlRules.Nelson2, [0.5, 0.5, 0.5, 0.5, -0.5, 0.5, 0.5, 0.5, 0.5], []),

        // Rule 3: six points in a row steadily increasing or decreasing.
        ["3: six rising"] = new(ControlRules.Nelson3, [-0.5, -0.3, -0.1, 0.1, 0.3, 0.5], [5]),
        ["3: six falling"] = new(ControlRules.Nelson3, [0.5, 0.3, 0.1, -0.1, -0.3, -0.5], [5]),
        ["3: five rising are not enough"] = new(ControlRules.Nelson3, [-0.5, -0.3, -0.1, 0.1, 0.3], []),
        ["3: seven rising keep it"] = new(ControlRules.Nelson3, [-0.6, -0.5, -0.3, -0.1, 0.1, 0.3, 0.5], [5, 6]),
        ["3: a tie breaks the run"] = new(ControlRules.Nelson3, [-0.5, -0.3, -0.3, 0.1, 0.3, 0.5], []),

        // Rule 4: fourteen points in a row alternating up and down.
        ["4: fourteen alternating"] = new(ControlRules.Nelson4, Alternating(0.5, -0.5, 14), [13]),
        ["4: thirteen alternating are not enough"] = new(ControlRules.Nelson4, Alternating(0.5, -0.5, 13), []),
        ["4: fifteen keep it"] = new(ControlRules.Nelson4, Alternating(0.5, -0.5, 15), [13, 14]),
        ["4: a repeat breaks the run"] = new(ControlRules.Nelson4, [.. Alternating(0.5, -0.5, 6), -0.5, .. Alternating(0.5, -0.5, 7)], []),

        // Rule 5: two of three points in a row beyond 2 sigma, on the same side.
        ["5: two of three beyond 2 sigma"] = new(ControlRules.Nelson5, [2.5, 0.1, 2.5], [2]),
        ["5: the first two, with nothing after yet"] = new(ControlRules.Nelson5, [2.5, 2.5], [1]),
        ["5: on opposite sides are not a pair"] = new(ControlRules.Nelson5, [2.5, 0.1, -2.5], []),
        ["5: one beyond is not enough"] = new(ControlRules.Nelson5, [2.5, 0.1, 0.1, 0.1], []),
        ["5: two three apart are not in one window"] = new(ControlRules.Nelson5, [2.5, 0.1, 0.1, 2.5], []),
        ["5: exactly 2 sigma is not beyond"] = new(ControlRules.Nelson5, [2.0, 0.1, 2.0], []),

        // Rule 6: four of five points in a row beyond 1 sigma, on the same side.
        ["6: four of five beyond 1 sigma"] = new(ControlRules.Nelson6, [1.5, 1.5, 0.0, 1.5, 1.5], [4]),
        ["6: the first four, with nothing after yet"] = new(ControlRules.Nelson6, [1.5, 1.5, 1.5, 1.5], [3]),
        ["6: three of five are not enough"] = new(ControlRules.Nelson6, [1.5, 1.5, 0.0, 0.0, 1.5], []),
        ["6: four of six are not in one window of five"] = new(ControlRules.Nelson6, [1.5, 0.0, 0.0, 1.5, 1.5, 1.5], []),
        ["6: on opposite sides are not four"] = new(ControlRules.Nelson6, [1.5, -1.5, 1.5, -1.5, 1.5], []),
        ["6: exactly 1 sigma is not beyond"] = new(ControlRules.Nelson6, [1.0, 1.0, 1.0, 1.0, 1.0], []),

        // Rule 7: fifteen points in a row within 1 sigma of the centre line (zone C), on either side.
        ["7: fifteen within 1 sigma"] = new(ControlRules.Nelson7, [.. Enumerable.Range(0, 15).Select(i => i % 2 == 0 ? 0.3 : -0.4)], [14]),
        ["7: fourteen are not enough"] = new(ControlRules.Nelson7, Repeat(0.3, 14), []),
        ["7: one point in zone B breaks the run"] = new(ControlRules.Nelson7, [.. Repeat(0.3, 7), 1.2, .. Repeat(0.3, 7)], []),
        ["7: exactly 1 sigma is not within"] = new(ControlRules.Nelson7, Repeat(1.0, 15), []),

        // Rule 8: eight points in a row more than 1 sigma from the centre line (none in zone C), on either side.
        ["8: eight beyond 1 sigma"] = new(ControlRules.Nelson8, Alternating(1.5, -1.5, 8), [7]),
        ["8: seven are not enough"] = new(ControlRules.Nelson8, Alternating(1.5, -1.5, 7), []),
        ["8: one point in zone C breaks the run"] = new(ControlRules.Nelson8, [1.5, -1.5, 1.5, 0.5, 1.5, -1.5, 1.5, -1.5], []),
        ["8: ten keep it"] = new(ControlRules.Nelson8, Alternating(1.5, -1.5, 10), [7, 8, 9]),
    };

    public static TheoryData<string> CaseNames { get; } = new(Cases.Keys);

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void ARule_FlagsThePointThatCompletesIt_AndTheLaterOnesThatKeepIt(string name)
    {
        var (rule, values, flagged) = Cases[name];

        Assert.Equal(flagged, NelsonRules.Flagged(rule, values, Limits));
    }

    /// <summary>A missing value (NaN) cannot be placed: it breaks every run through it and is never flagged itself, and an
    /// infinity is no better.</summary>
    [Theory]
    [InlineData(ControlRules.Nelson1, 3.5)]
    [InlineData(ControlRules.Nelson2, 0.5)]
    [InlineData(ControlRules.Nelson7, 0.3)]
    public void AMissingValue_BreaksTheRun_AndIsNeverFlagged(ControlRules rule, double level)
    {
        int length = rule switch { ControlRules.Nelson2 => 9, ControlRules.Nelson7 => 15, _ => 3 };
        double[] values = Repeat(level, length);
        values[length / 2] = double.NaN;

        int[] flagged = NelsonRules.Flagged(rule, values, Limits);

        Assert.DoesNotContain(length / 2, flagged);
        if (rule != ControlRules.Nelson1) Assert.Empty(flagged);
    }

    [Fact]
    public void AnInfinity_IsNeverFlagged()
    {
        Assert.Empty(NelsonRules.Flagged(ControlRules.Nelson1, [double.PositiveInfinity, double.NegativeInfinity], Limits));
    }

    /// <summary>The rules asked for are judged together: the points flagged by any of them, once each, in the order of the
    /// values. Nothing asked for flags nothing.</summary>
    [Fact]
    public void SeveralRules_FlagTheUnion_OnceEach_InOrder()
    {
        // Index 1 is beyond the upper limit (rule 1); indices 3 to 11 are nine above the centre (rule 2, flagged at 11);
        // index 5 is also beyond the limit.
        double[] values = [0, 3.5, 0, 0.5, 0.5, 3.4, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5];

        Assert.Equal([1, 5, 11], NelsonRules.Flagged(ControlRules.Nelson1 | ControlRules.Nelson2, values, Limits));
        Assert.Equal([1, 5], NelsonRules.Flagged(ControlRules.Nelson1, values, Limits));
        Assert.Equal([11], NelsonRules.Flagged(ControlRules.Nelson2, values, Limits));
        Assert.Empty(NelsonRules.Flagged(ControlRules.None, values, Limits));
    }

    /// <summary>Every rule asked for at once agrees with the rules one at a time.</summary>
    [Fact]
    public void AllTheRules_AreTheUnionOfEachOne()
    {
        double[] values = [.. Cases.Values.SelectMany(c => c.Values).Concat([0.0])];

        int[] all = NelsonRules.Flagged(ControlRules.All, values, Limits);
        ControlRules[] each =
        [
            ControlRules.Nelson1, ControlRules.Nelson2, ControlRules.Nelson3, ControlRules.Nelson4,
            ControlRules.Nelson5, ControlRules.Nelson6, ControlRules.Nelson7, ControlRules.Nelson8,
        ];
        int[] oneByOne = [.. each.SelectMany(r => NelsonRules.Flagged(r, values, Limits)).Distinct().Order()];

        Assert.Equal(oneByOne, all);
        Assert.NotEmpty(all);
    }

    /// <summary>The zones are a third of the way from the centre to the upper limit: the same values on limits twice as wide
    /// are in zone C, where they were in zone A.</summary>
    [Fact]
    public void TheZones_AreAThirdOfTheDistanceFromTheCentreToTheUpperLimit()
    {
        double[] values = [2.5, 0.1, 2.5];

        Assert.Equal([2], NelsonRules.Flagged(ControlRules.Nelson5, values, new ControlLimits(0, -3, 3)));
        Assert.Empty(NelsonRules.Flagged(ControlRules.Nelson5, values, new ControlLimits(0, -6, 6)));
    }

    /// <summary>With no spread (the limits on the centre) a value off the centre is beyond a limit and beyond every zone, and
    /// a value on it is in none: no division by a sigma of 0.</summary>
    [Fact]
    public void LimitsOnTheCentre_HaveNoZones_AndNothingIsNaN()
    {
        var flat = new ControlLimits(5, 5, 5);

        Assert.Equal([1], NelsonRules.Flagged(ControlRules.Nelson1, [5, 5.1, 5], flat));
        Assert.Empty(NelsonRules.Flagged(ControlRules.Nelson7, Repeat(5, 15), flat));
    }
}
