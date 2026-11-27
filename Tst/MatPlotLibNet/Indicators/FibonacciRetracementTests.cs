// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="FibonacciRetracement"/> behavior.</summary>
public class FibonacciRetracementTests
{
    /// <summary>Verifies that Apply adds five Fibonacci retracement reference lines.</summary>
    [Fact]
    public void Apply_AddsFiveReferenceLines()
    {
        var axes = new Axes();
        axes.Plot([0, 1, 2], [100, 200, 150]); // need a series for axes to render
        new FibonacciRetracement(100, 200).Apply(axes);
        Assert.Equal(5, axes.ReferenceLines.Count); // 23.6%, 38.2%, 50%, 61.8%, 78.6%
    }

    /// <summary>Verifies that all retracement lines fall within the high-low price range.</summary>
    [Fact]
    public void Apply_LinesAreWithinRange()
    {
        var axes = new Axes();
        axes.Plot([0, 1], [100, 200]);
        new FibonacciRetracement(100, 200).Apply(axes);
        foreach (var line in axes.ReferenceLines)
        {
            Assert.InRange(line.Value, 100, 200);
        }
    }

    /// <summary>The five levels are the retracement tool's five inner levels, to the last bit: the tool owns the ratios and the price
    /// a ratio of the move stands at.</summary>
    [Fact]
    public void Compute_ReturnsTheToolsFiveInnerLevels()
    {
        double[] levels = new FibonacciRetracement(100, 200).Compute();
        var tool = new MatPlotLibNet.Models.Tools.FibonacciRetracement(priceHigh: 200, priceLow: 100);

        Assert.Equal(tool.Levels.Skip(1).Take(5).Select(level => level.Price), levels);
    }

    /// <summary>Covers the explicit-color branch in <see cref="FibonacciRetracement.Apply"/>.</summary>
    [Fact]
    public void Apply_CustomColor_AppliedToLines()
    {
        var axes = new Axes();
        axes.Plot([0, 1], [100, 200]);
        new FibonacciRetracement(100, 200) { Color = MatPlotLibNet.Styling.Colors.Cyan }.Apply(axes);
        foreach (var line in axes.ReferenceLines)
        {
            Assert.Equal(MatPlotLibNet.Styling.Colors.Cyan, line.Color);
        }
    }

    private static Axes Applied(FibonacciRetracement fib)
    {
        var axes = new Axes();
        axes.Plot([0, 1], [100, 200]);
        fib.Apply(axes);
        return axes;
    }

    [Fact]
    public void WithNoStyleGiven_TheLevelsAreDashed()
    {
        var fib = new FibonacciRetracement(100, 200);

        Assert.Equal(LineStyle.Dashed, fib.LineStyle);
        Assert.All(Applied(fib).ReferenceLines, line => Assert.Equal(LineStyle.Dashed, line.LineStyle));
    }

    [Theory]
    [InlineData(LineStyle.Solid)]
    [InlineData(LineStyle.Dotted)]
    [InlineData(LineStyle.DashDot)]
    [InlineData(LineStyle.None)]
    public void ACallersLineStyle_DrawsTheLevels(LineStyle style)
    {
        var axes = Applied(new FibonacciRetracement(100, 200) { LineStyle = style });

        Assert.Equal(5, axes.ReferenceLines.Count);
        Assert.All(axes.ReferenceLines, line => Assert.Equal(style, line.LineStyle));
    }
}
