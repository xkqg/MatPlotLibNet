// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="ParabolicSar"/> behavior.</summary>
public class ParabolicSarTests
{
    private static readonly double[] High = [48.70, 48.72, 48.90, 48.87, 48.82, 49.05, 49.20, 49.35, 49.92, 50.19,
        50.12, 49.66, 49.88, 50.19, 50.36, 50.57, 50.65, 50.43, 49.63, 48.66];
    private static readonly double[] Low = [47.79, 48.14, 48.39, 48.37, 48.24, 48.64, 48.94, 48.86, 49.50, 49.87,
        49.20, 48.90, 49.43, 49.73, 49.26, 50.09, 50.30, 49.21, 48.98, 48.22];

    /// <summary>Five bars each three higher (or lower) than the one before, with a step of 0.05 and a maximum of 0.07, and the SAR
    /// TA-Lib 0.8.3 gives for them (<c>talib.SAR(high, low, 0.05, 0.07)</c>), and whether the trend is up.
    /// <para>On the way up: <c>[nan, 10.0, 10.2, 10.676, 11.32868]</c>; worked by hand: the first bar has no SAR, the second stands at
    /// the first low (10) with the second high (14) as the extreme point, and each bar after moves the SAR toward the extreme point of
    /// the bars before it by the factor, <c>10 + 0.05 × (14 - 10)</c>, then <c>10.2 + 0.07 × (17 - 10.2)</c> (the factor capped at 0.07
    /// where 0.1 would follow), then <c>10.676 + 0.07 × (20 - 10.676)</c>.</para>
    /// <para>On the way down: <c>[nan, 31.0, 30.8, 30.324, 29.67132]</c>. The second low is below the first by more than the second high
    /// is below the first high (matplotlib's reference, pandas-ta and TA-Lib take the trend's start from that minus directional
    /// movement), so the trend starts down, the SAR at the first high (31) and the extreme point at the second low (27).</para></summary>
    public static TheoryData<double[], double[], double[], bool> TrendsThatReachTheMaximum => new()
    {
        { [11, 14, 17, 20, 23], [10, 13, 16, 19, 22], [10.0, 10.2, 10.676, 11.32868], true },
        { [31, 28, 25, 22, 19], [30, 27, 24, 21, 18], [31.0, 30.8, 30.324, 29.67132], false },
    };

    /// <summary>The acceleration factor stops growing at the maximum, on the way up and on the way down.</summary>
    [Theory]
    [MemberData(nameof(TrendsThatReachTheMaximum))]
    public void Compute_AccelerationStopsAtTheMaximum(double[] high, double[] low, double[] expected, bool up)
    {
        var result = new ParabolicSar(high, low, step: 0.05, max: 0.07).Compute();

        Assert.True(double.IsNaN(result.Sar[0]));
        for (int i = 0; i < expected.Length; i++) Assert.Equal(expected[i], result.Sar[i + 1], 12);
        Assert.All(result.IsLong.Skip(1), isLong => Assert.Equal(up, isLong));
    }

    /// <summary>Wilder's own example (the bars of <c>High</c> and <c>Low</c>, from "New Concepts in Technical Trading Systems") gives
    /// the SAR TA-Lib gives: <c>talib.SAR(high, low, 0.02, 0.2)</c>, TA-Lib 0.8.3, printed with python.</summary>
    [Fact]
    public void Compute_WildersExample_GivesTaLibsSar()
    {
        double[] expected = [47.79, 47.8086, 47.852256, 47.89416576, 47.9343991296, 48.001335181824004, 48.097228367278085,
            48.22250553055028, 48.42620486688425, 48.673136185520455, 48.885497119547594, 48.9, 48.9, 49.0806, 49.26, 49.26, 50.65,
            50.65, 50.5832];

        var result = new ParabolicSar(High, Low).Compute();

        Assert.True(double.IsNaN(result.Sar[0]));
        for (int i = 0; i < expected.Length; i++) Assert.Equal(expected[i], result.Sar[i + 1], 9);
    }

    /// <summary>On the bar the price crosses the SAR, the trend reverses and the SAR jumps to the extreme point of the trend that
    /// ended (Wilder's rule, TA-Lib's and pandas-ta's): at bar 17 of Wilder's example the low (49.21) falls through the SAR of an up-trend,
    /// and the SAR of that bar is the highest high of the up-trend, 50.65, above the bar's own high (50.43), drawn as a short dot. It
    /// is never the low of the bar that crossed.</summary>
    [Fact]
    public void Compute_OnAReversal_TheSarIsTheExtremePointOfTheTrendThatEnded()
    {
        var result = new ParabolicSar(High, Low).Compute();

        Assert.True(result.IsLong[16]);
        Assert.False(result.IsLong[17]);
        Assert.Equal(50.65, result.Sar[17], 12);
        Assert.True(result.Sar[17] > High[17]);
    }

    /// <summary>The first bar has no SAR (it only seeds the trend with the second), so it has no dot.</summary>
    [Fact]
    public void Apply_DrawsNoDotForTheFirstBar()
    {
        var axes = new Axes();
        new ParabolicSar(High, Low).Apply(axes);

        int dots = axes.Series.Cast<ScatterSeries>().Sum(s => s.XData.Length);
        Assert.Equal(High.Length - 1, dots);
        Assert.DoesNotContain(axes.Series.Cast<ScatterSeries>(), s => s.XData.Contains(0.0));
    }

    /// <summary>SAR array length equals input length.</summary>
    [Fact]
    public void Compute_SarLengthEqualsInput()
    {
        var result = new ParabolicSar(High, Low).Compute();
        Assert.Equal(High.Length, result.Sar.Length);
        Assert.Equal(High.Length, result.IsLong.Length);
    }
}
