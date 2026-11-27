// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="Sma"/> behavior.</summary>
public class SmaTests
{
    /// <summary>Verifies that Compute returns correct simple moving average values.</summary>
    [Fact]
    public void Compute_ReturnsCorrectValues()
    {
        double[] prices = [10, 20, 30, 40, 50];
        double[] result = new Sma(prices, 3).Compute();
        Assert.Equal(20, result[0]); // (10+20+30)/3
        Assert.Equal(30, result[1]); // (20+30+40)/3
        Assert.Equal(40, result[2]); // (30+40+50)/3
    }

    /// <summary>Verifies that a custom color is applied to the resulting LineSeries.</summary>
    [Fact]
    public void Apply_RespectsCustomColor()
    {
        var axes = new Axes();
        new Sma([10, 20, 30, 40, 50], 3) { Color = Colors.Red }.Apply(axes);
        Assert.Equal(Colors.Red, ((LineSeries)axes.Series[0]).Color);
    }

    /// <summary>Verifies that Sma implements the IIndicator interface.</summary>
    [Fact]
    public void ImplementsIIndicator()
    {
        Assert.IsAssignableFrom<IIndicator>(new Sma([1.0], 1));
    }

    /// <summary>Verifies that Sma extends the Indicator base class.</summary>
    [Fact]
    public void ExtendsIndicatorBase()
    {
        Assert.IsAssignableFrom<Indicator>(new Sma([1.0], 1));
    }
}
