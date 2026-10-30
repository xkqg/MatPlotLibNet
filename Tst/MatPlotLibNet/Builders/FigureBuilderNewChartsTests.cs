// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The figure-level forms of the verbs of 1.27.0 draw on the one implicit axes, as every other figure-level series verb
/// does, and hand the series they made to the <c>configure</c> callback.</summary>
public class FigureBuilderNewChartsTests
{
    private static readonly string[] Words = ["alpha", "beta", "gamma"];
    private static readonly double[] Weights = [3, 2, 1];
    private static readonly double[] Top = [0.5, 0.2, 0.3];
    private static readonly double[] Left = [0.3, 0.5, 0.3];
    private static readonly double[] Right = [0.2, 0.3, 0.4];

    private static T OnlySeries<T>(FigureBuilder builder) where T : ChartSeries =>
        Assert.IsType<T>(Assert.Single(builder.Build().SubPlots[0].Series));

    [Fact]
    public void AWordCloud_IsDrawnOnTheImplicitAxes_AndConfigured()
    {
        WordCloudSeries? configured = null;

        var series = OnlySeries<WordCloudSeries>(Plt.Create().WordCloud(Words, Weights, s => configured = s));

        Assert.Same(series, configured);
    }

    [Fact]
    public void AChord_IsDrawnOnTheImplicitAxes_AndConfigured()
    {
        ChordSeries? configured = null;
        double[,] flows = { { 0, 1 }, { 2, 0 } };

        var series = OnlySeries<ChordSeries>(Plt.Create().Chord(["a", "b"], flows, s => configured = s));

        Assert.Same(series, configured);
    }

    [Fact]
    public void ATernaryLine_IsDrawnOnTheImplicitAxes_AndConfigured()
    {
        TernaryLineSeries? configured = null;

        var builder = Plt.Create().TernaryPlot(Top, Left, Right, s => configured = s);
        var series = OnlySeries<TernaryLineSeries>(builder);

        Assert.Same(series, configured);
        Assert.Equal(CoordinateSystem.Ternary, builder.Build().SubPlots[0].CoordinateSystem);
    }

    [Fact]
    public void ATernaryScatter_IsDrawnOnTheImplicitAxes_AndConfigured()
    {
        TernaryScatterSeries? configured = null;

        var series = OnlySeries<TernaryScatterSeries>(Plt.Create().TernaryScatter(Top, Left, Right, s => configured = s));

        Assert.Same(series, configured);
    }

    [Fact]
    public void APiperDiagram_IsDrawnOnTheImplicitAxes_AndConfigured()
    {
        PiperSeries? configured = null;
        WaterSample[] waters = [new() { Label = "spring", Ca = 60, Mg = 10, Na = 8, K = 2, HCO3 = 200, SO4 = 20, Cl = 10 }];

        var series = OnlySeries<PiperSeries>(Plt.Create().Piper(waters, s => configured = s));

        Assert.Same(series, configured);
    }

    [Fact]
    public void ASelectionGroupWithoutAName_IsRefused()
    {
        Assert.Throws<ArgumentException>(() => Plt.Create().LinkSelection(" "));
    }
}
