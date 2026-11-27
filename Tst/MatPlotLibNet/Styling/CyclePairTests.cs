// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>The first two colours of the standard cycle are one pair, <see cref="CyclePair.Standard"/>, and every part whose colour is a
/// choice made before any theme is known (a value at rest that the JSON always writes, the pair a verb gives its series and axes, the
/// look an indicator chooses) reads it. The source scan that holds the spelling of the two colours to this one place is a row of
/// <c>OneOwnerSourceContractTests</c>.</summary>
public class CyclePairTests
{
    [Fact]
    public void TheStandardPair_IsTheFirstTwoColoursOfTheStandardCycle()
    {
        Assert.Equal(QualitativeColorMaps.Tab10At(0), CyclePair.Standard.First);
        Assert.Equal(QualitativeColorMaps.Tab10At(1), CyclePair.Standard.Second);
        Assert.Equal(Colors.Tab10Blue, CyclePair.Standard.First);
        Assert.Equal(Colors.Tab10Orange, CyclePair.Standard.Second);
    }

    [Fact]
    public void ARecordStruct_HoldsItsTwoColoursAsGiven()
    {
        var (first, second) = new CyclePair(Colors.Red, Colors.Green);

        Assert.Equal(Colors.Red, first);
        Assert.Equal(Colors.Green, second);
    }

    [Fact]
    public void TheValuesAPartRestsAt_AreTheFirstColourOfThePair()
    {
        Assert.Equal(CyclePair.Standard.First, new ProgressBarSeries(0.5).FillColor);
        Assert.Equal(CyclePair.Standard.First, new WaterfallSeries(["a"], [1.0]).TotalColor);
        Assert.Equal(CyclePair.Standard.First, new Circle(new(0, 0), 1).FaceColor);
        Assert.Equal(CyclePair.Standard.First, new PropCyclerBuilder().Build()[0].Color);
    }

    [Fact]
    public void AnIndicator_ChoosesThePairForItsFirstAndSecondLine()
    {
        Assert.Equal(CyclePair.Standard, IndicatorLook.Library.Lines);
    }

    [Fact]
    public void AParetoChart_GivesItsBarsAndItsLine_ThePair()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Pareto(["a", "b"], [3.0, 1.0])).Build().SubPlots[0];

        Assert.Equal(CyclePair.Standard.First, ((BarSeries)axes.Series[0]).Color);
        Assert.Equal(CyclePair.Standard.Second, ((LineSeries)axes.YAxes.Right[0].Series[0]).Color);
    }
}
