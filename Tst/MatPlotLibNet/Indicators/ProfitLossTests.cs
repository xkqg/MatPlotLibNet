// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="ProfitLoss"/> behavior.</summary>
public class ProfitLossTests
{
    /// <summary>A return goes to the profit bars when it is above zero and to the loss bars when it is below; each set of bars
    /// is 0 where the other holds the return, and a return of zero is 0 in both.</summary>
    [Fact]
    public void Apply_SplitsTheReturnsIntoProfitAndLossBars()
    {
        var axes = new Axes();
        new ProfitLoss([1.0, -2.0, 3.0, -4.0, 0.0]).Apply(axes);

        var bars = axes.Series.OfType<BarSeries>().ToArray();
        Assert.Equal([1.0, 0.0, 3.0, 0.0, 0.0], bars[0].Values);
        Assert.Equal([0.0, -2.0, 0.0, -4.0, 0.0], bars[1].Values);
    }

    /// <summary>A colour given to the losses is the colour of the loss bars, and one given to the profits the colour of the profit bars.</summary>
    [Fact]
    public void GivenColours_AreTheColoursOfTheirBars()
    {
        var axes = new Axes();

        new ProfitLoss([1.0, -2.0]) { ProfitColor = MatPlotLibNet.Styling.Colors.Blue, LossColor = MatPlotLibNet.Styling.Colors.Orange }.Apply(axes);

        var bars = axes.Series.OfType<BarSeries>().ToArray();
        Assert.Equal(MatPlotLibNet.Styling.Colors.Blue, bars[0].Color);
        Assert.Equal(MatPlotLibNet.Styling.Colors.Orange, bars[1].Color);
    }
}
