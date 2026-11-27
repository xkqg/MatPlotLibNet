// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>What the base of the indicators draws: the line of a signal carries the label, colour, width and dash the indicator was given.</summary>
public class IndicatorTests
{
    [Fact]
    public void PlotSignal_SetsLabelColorLineWidthLineStyle()
    {
        var indicator = new Sma([10, 20, 30, 40, 50], 3);
        indicator.Label = "TestSMA";
        indicator.Color = Color.FromHex("#FF0000");
        indicator.LineWidth = 2.5;
        indicator.LineStyle = LineStyle.Dashed;
        var axes = new Axes();

        indicator.Apply(axes);

        var line = axes.Series.OfType<LineSeries>().First();
        Assert.Equal("TestSMA", line.Label);
        Assert.Equal(Color.FromHex("#FF0000"), line.Color);
        Assert.Equal(2.5, line.LineWidth);
        Assert.Equal(LineStyle.Dashed, line.LineStyle);
    }

    /// <summary>Verifies that setting Offset shifts the X coordinates of the resulting series.</summary>
    [Fact]
    public void Sma_WithOffset_ShiftsXCoordinates()
    {
        var axes = new Axes();
        new Sma([10, 20, 30, 40, 50], 3) { Offset = 5 }.Apply(axes);
        var series = (LineSeries)axes.Series[0];
        // Normal X starts at period-1=2. With offset 5, should start at 7.
        Assert.Equal(7, series.XData[0]);
    }

    [Fact]
    public void AColourTheCallerGave_IsTheSeriesColour_AndTheChoiceIsLeftUnmade()
    {
        var line = new LineSeries([1.0, 2.0], [1.0, 2.0]);

        Indicator.WearColor(line, Colors.Red, Colors.Green);

        Assert.Equal(Colors.Red, line.Color);
        Assert.Null(line.VerbColor);
    }

    [Fact]
    public void WithNoColourGiven_TheIndicatorsChoiceIsTheVerbColour_AndTheSeriesNamesNone()
    {
        var bars = new BarSeries(["a", "b"], [1.0, 2.0]);

        Indicator.WearColor(bars, null, Colors.Green);

        Assert.Null(bars.Color);
        Assert.Equal(Colors.Green, bars.VerbColor);
    }

    [Fact]
    public void ADotSeries_TakesTheSameDecision()
    {
        var given = new ScatterSeries([1.0], [1.0]);
        var chosen = new ScatterSeries([1.0], [1.0]);

        Indicator.WearColor(given, Colors.Red, Colors.Green);
        Indicator.WearColor(chosen, null, Colors.Green);

        Assert.Equal(Colors.Red, given.Color);
        Assert.Null(given.VerbColor);
        Assert.Null(chosen.Color);
        Assert.Equal(Colors.Green, chosen.VerbColor);
    }
}
