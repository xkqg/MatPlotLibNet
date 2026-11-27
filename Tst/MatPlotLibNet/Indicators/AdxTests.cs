// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="Adx"/> behavior.</summary>
public class AdxTests
{
    private static readonly double[] High = [30.20, 30.28, 30.45, 29.35, 29.35, 29.29, 28.83, 28.73, 28.67, 28.85, 28.64, 27.68, 27.21, 26.87, 27.41, 26.94, 26.52, 26.52, 27.09, 27.69];
    private static readonly double[] Low = [29.41, 29.32, 29.96, 28.74, 28.56, 28.41, 28.08, 27.43, 27.66, 27.83, 27.40, 27.09, 26.18, 26.13, 26.63, 26.13, 25.43, 25.35, 26.18, 26.58];
    private static readonly double[] Close = [29.87, 30.24, 30.10, 28.90, 28.92, 28.48, 28.56, 27.56, 28.47, 28.28, 27.49, 27.23, 26.35, 26.33, 27.03, 26.22, 26.01, 25.46, 27.03, 27.45];

    /// <summary>Verifies that Compute returns a non-empty result array.</summary>
    [Fact]
    public void Compute_ReturnsNonEmpty()
    {
        double[] adx = new Adx(OhlcvBars.Hlc(High, Low, Close), 5).Compute();
        Assert.True(adx.Length > 0);
    }

    /// <summary>Covers custom +DI / -DI color overrides on Adx.</summary>
    [Fact]
    public void Apply_CustomDiColors_AppliedToSeries()
    {
        var axes = new Axes();
        new Adx(OhlcvBars.Hlc(High, Low, Close), 5)
        {
            PlusDiColor = MatPlotLibNet.Styling.Colors.Cyan,
            MinusDiColor = MatPlotLibNet.Styling.Colors.Magenta
        }.Apply(axes);
        // Series 0 = ADX, 1 = +DI (Cyan), 2 = -DI (Magenta)
        Assert.True(axes.Series.Count >= 3);
        Assert.Equal(MatPlotLibNet.Styling.Colors.Cyan, ((LineSeries)axes.Series[1]).Color);
        Assert.Equal(MatPlotLibNet.Styling.Colors.Magenta, ((LineSeries)axes.Series[2]).Color);
    }

    /// <summary>ComputeFull must return real +DI / -DI — not the 50-stub placeholder.</summary>
    [Fact]
    public void ComputeFull_PlusDi_NotAllFifty()
    {
        var r = new Adx(OhlcvBars.Hlc(High, Low, Close), 5).ComputeFull();
        Assert.NotEmpty(r.PlusDi);
        Assert.False(r.PlusDi.All(v => v == 50.0), "+DI must not be the 50 placeholder");
    }

    /// <summary>ComputeFull must return all three arrays with the same length.</summary>
    [Fact]
    public void ComputeFull_AllThree_SameLength()
    {
        var r = new Adx(OhlcvBars.Hlc(High, Low, Close), 5).ComputeFull();
        Assert.NotEmpty(r.Adx);
        Assert.Equal(r.Adx.Length, r.PlusDi.Length);
        Assert.Equal(r.Adx.Length, r.MinusDi.Length);
    }
}
