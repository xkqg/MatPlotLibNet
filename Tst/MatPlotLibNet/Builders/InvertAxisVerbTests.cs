// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>An axis is turned round from the builder, as matplotlib's <c>invert_xaxis()</c> and <c>invert_yaxis()</c> do. matplotlib 3.11.2,
/// read with python: <c>invert_yaxis()</c> is <c>self.yaxis.set_inverted(not self.yaxis.get_inverted())</c>, a toggle: once inverted, twice
/// back. The axis holds it (<see cref="Axis.Inverted"/>), which the JSON carries.</summary>
public class InvertAxisVerbTests
{
    private static Axes AxesOf(Action<AxesBuilder> configure) =>
        Plt.Create().AddSubPlot(1, 1, 1, ax => { ax.Plot([0.0, 1.0], [0.0, 1.0]); configure(ax); }).Build().SubPlots[0];

    [Fact]
    public void InvertYAxis_TurnsTheYAxisRound_AndTheXAxisStays()
    {
        var axes = AxesOf(ax => ax.InvertYAxis());

        Assert.True(axes.YAxis.Inverted);
        Assert.False(axes.XAxis.Inverted);
    }

    [Fact]
    public void InvertXAxis_TurnsTheXAxisRound_AndTheYAxisStays()
    {
        var axes = AxesOf(ax => ax.InvertXAxis());

        Assert.True(axes.XAxis.Inverted);
        Assert.False(axes.YAxis.Inverted);
    }

    [Fact]
    public void Inverting_Twice_TurnsTheAxisBack_AsMatplotlibsToggleDoes()
    {
        Assert.False(AxesOf(ax => ax.InvertYAxis().InvertYAxis()).YAxis.Inverted);
        Assert.False(AxesOf(ax => ax.InvertXAxis().InvertXAxis()).XAxis.Inverted);
    }

    // matplotlib 3.11.2: imshow sets ylim (rows - 0.5, -0.5), the first row on top; invert_yaxis() then turns it, the first row at the bottom.
    [Fact]
    public void InvertingTheYAxisOfAnImageUnderTheMatplotlibThemes_PutsItsFirstRowAtTheBottom()
    {
        var figure = Plt.Create().WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.Image(new double[,] { { 0, 1 }, { 2, 3 } }).InvertYAxis()).Build();

        var range = figure.SubPlots[0].ComputeDataRanges(figure.Theme);

        Assert.Equal((-0.5, 1.5), (range.YMin, range.YMax));
    }

    [Fact]
    public void AnInvertedAxis_RunsHighToLow_AndSurvivesTheJson()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0]).InvertYAxis()).Build();
        var range = figure.SubPlots[0].ComputeDataRanges(figure.Theme);

        Assert.True(range.YMin > range.YMax);
        Assert.Equal(figure.ToSvg(), figure.RoundTripped().ToSvg());
    }
}
