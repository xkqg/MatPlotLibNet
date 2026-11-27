// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>The levels a filled contour draws when it names none, and the run of ticks they are taken from. matplotlib 3.11.2,
/// <c>ContourSet._ensure_locator_exists</c>: <c>ticker.MaxNLocator(N + 1, min_n_ticks=1)</c>; <c>ContourSet._autolev</c>:
/// <c>lev = self.locator.tick_values(self.zmin, self.zmax)</c>, then at most one level below the data and one above it are kept
/// (<c>i0 = under[-1]</c>, <c>i1 = over[0] + 1</c>, all of them when fewer than three are left); <c>MaxNLocator._raw_ticks</c> takes
/// a larger step under <c>axes.autolimit_mode = 'round_numbers'</c>. Every expected list was printed by matplotlib for the same
/// range, in both modes.</summary>
public sealed class SteppedTicksTests
{
    private static void AssertLevels(double[] expected, double[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
            Assert.True(Math.Abs(expected[i] - actual[i]) <= 1e-12 * Math.Max(1, Math.Abs(expected[i])),
                $"level {i}: expected {expected[i]:R}, got {actual[i]:R}");
    }

    /// <summary>The peaks surface of the fidelity check: <c>contourf(X, Y, Z, levels=10)</c> draws eleven levels, -0.8 to 1.2 in
    /// steps of 0.2, in both styles; <c>contourf(X, Y, Z)</c> (seven) draws -0.75 to 1.25 in steps of 0.25.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ThePeaksSurface_TakesMatplotlibsLevels(bool roundNumbers)
    {
        AssertLevels([-0.8, -0.6000000000000001, -0.4, -0.19999999999999996, 0.0, 0.19999999999999996, 0.40000000000000013,
                0.6000000000000001, 0.8, 1.0, 1.2],
            ContourLevels.Auto(-0.6849773668989229, 1.0560418679341015, 10, roundNumbers));
        AssertLevels([-0.75, -0.5, -0.25, 0.0, 0.25, 0.5, 0.75, 1.0, 1.25],
            ContourLevels.Auto(-0.6849773668989229, 1.0560418679341015, 7, roundNumbers));
    }

    public static TheoryData<double, double, int, double[], double[]> Ranges { get; } = new()
    {
        { 2202.046856090197, 2412.9112426911925, 3, [2160, 2220, 2280, 2340, 2400, 2460], [2160, 2240, 2320, 2400, 2480] },
        { 0, 1, 10, [0, 0.1, 0.2, 0.30000000000000004, 0.4, 0.5, 0.6000000000000001, 0.7000000000000001, 0.8, 0.9, 1.0],
            [0, 0.1, 0.2, 0.30000000000000004, 0.4, 0.5, 0.6000000000000001, 0.7000000000000001, 0.8, 0.9, 1.0] },
        { 0, 1, 1, [0, 0.5, 1], [0, 0.5, 1] },
        // Far from zero: the range is offset by a round number before it is stepped, and an edge is judged with more slop.
        { 1000000.3, 1000000.9, 5, [1000000.3, 1000000.4, 1000000.5, 1000000.6, 1000000.7, 1000000.8, 1000000.9],
            [1000000.3, 1000000.45, 1000000.6, 1000000.75, 1000000.9] },
        { -5, -1, 4, [-5.6000000000000005, -4.800000000000001, -4.0, -3.2, -2.4000000000000004, -1.6000000000000005, -0.7999999999999998],
            [-5, -4, -3, -2, -1] },
        { 3, 3.0000001, 10, [3.0, 3.00000001, 3.00000002, 3.0000000300000003, 3.00000004, 3.00000005, 3.00000006, 3.00000007,
            3.00000008, 3.00000009, 3.0000001000000003], [3.0, 3.00000001, 3.00000002, 3.0000000300000003, 3.00000004, 3.00000005,
            3.00000006, 3.00000007, 3.00000008, 3.00000009, 3.0000001000000003] },
        { 0.013, 0.0172, 6, [0.012600000000000002, 0.013200000000000002, 0.013800000000000002, 0.014400000000000001,
            0.015000000000000003, 0.015600000000000003, 0.016200000000000003, 0.016800000000000002, 0.017400000000000002],
            [0.0128, 0.013600000000000001, 0.014400000000000001, 0.015200000000000002, 0.016, 0.016800000000000002, 0.0176] },
    };

    [Theory]
    [MemberData(nameof(Ranges))]
    public void ARange_TakesMatplotlibsLevels_InEitherMode(double lo, double hi, int count, double[] data, double[] roundNumbers)
    {
        AssertLevels(data, ContourLevels.Auto(lo, hi, count, roundNumbers: false));
        AssertLevels(roundNumbers, ContourLevels.Auto(lo, hi, count, roundNumbers: true));
    }

    /// <summary>A range too narrow for its magnitude is widened by a ten-trillionth of each end first (<c>_nonsingular(vmin, vmax,
    /// expander=1e-13, tiny=1e-14)</c>): 1000 to 1000 + 5e-12 takes three levels.</summary>
    [Fact]
    public void ARangeTooNarrowForItsMagnitude_IsWidenedFirst()
    {
        AssertLevels([999.99999999998, 1000.0, 1000.00000000002], ContourLevels.Auto(1000.0, 1000.000000000005, 10, roundNumbers: false));
        AssertLevels([999.999999999975, 1000.0, 1000.000000000025], ContourLevels.Auto(1000.0, 1000.000000000005, 10, roundNumbers: true));
    }

    /// <summary>A step that leaves fewer ticks inside the range than the ladder asks for gives way to the next smaller one:
    /// <c>MaxNLocator(2, min_n_ticks=5).tick_values(0, 1)</c> is 0 to 1 in quarters.</summary>
    [Fact]
    public void AStepWithTooFewTicks_GivesWayToASmallerOne()
    {
        var run = SteppedTicks.Over(0, 1, TickLadder.ForLevels(1) with { MinTicks = 5 }, roundNumbers: false);

        AssertLevels([0, 0.25, 0.5, 0.75, 1.0], [.. Enumerable.Range(0, run.Count).Select(i => run[i])]);
    }
}
