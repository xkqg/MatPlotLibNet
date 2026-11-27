// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An area and a stacked area under the matplotlib themes stand and are drawn as matplotlib's <c>fill_between</c>,
/// <c>fill_betweenx</c> and <c>stackplot</c> draw them. matplotlib 3.11.2, printed with python (8 x 6 inch figure):
/// <list type="bullet">
/// <item><c>fill_between(x, 0, sin(x) + 1.2)</c> over x 0 .. 10 registers no sticky edge, so the default style pads both axes by its
/// margin, 0.05: xlim (-0.5, 10.5), ylim (-0.10998, 2.30968); the classic style (margin 0, round numbers) (0, 10), (0, 2.5).</item>
/// <item><c>fill_betweenx(y, x)</c> over y 0 .. 10, x 2 .. 10: xlim (-0.5, 10.5), ylim (-0.5, 10.5) under the default style.</item>
/// <item><c>stackplot(x, ...)</c> registers 0 as its first layer's sticky floor and nothing on x: xlim (-0.5, 10.5), ylim
/// (0, 8.81866) under the default style; every layer is opaque (alpha None) and edged black at 1.0 pt under the classic style
/// (<c>patch.force_edgecolor</c>), not edged under the default one; its legend handle carries the same edge.</item>
/// </list>
/// The library's own themes keep the area's sticky edges, its 0.3 fill and the stack's 0.7.</summary>
public class FillsLikeMatplotlibTests
{
    private static readonly double OnePoint = 1.0.PointsInPixels;

    private static double[] Linspace(double start, double end, int n) =>
        [.. Enumerable.Range(0, n).Select(i => start + i * (end - start) / (n - 1))];

    private static readonly double[] X100 = Linspace(0, 10, 100);
    private static readonly double[] X50 = Linspace(0, 10, 50);

    private static Axes AreaAxes() =>
        Plt.Create().AddSubPlot(1, 1, 1, ax => ax.FillBetween(X100, [.. X100.Select(v => Math.Sin(v) + 1.2)])).Build().SubPlots[0];

    private static Axes StackAxes(Action<StackedAreaSeries>? configure = null) =>
        Plt.Create().AddSubPlot(1, 1, 1, ax => ax.StackPlot(X50,
            [[.. X50.Select(v => Math.Sin(v) + 2)], [.. X50.Select(v => Math.Cos(v) + 2)], [.. X50.Select(v => Math.Sin(v + 1) + 2)]],
            configure)).Build().SubPlots[0];

    // ---- the range -----------------------------------------------------------------------------------------------------------

    [Fact]
    public void UnderV2_AFillBetween_IsPaddedByTheMarginOnBothAxes()
    {
        var range = AreaAxes().ComputeDataRanges(Theme.MatplotlibV2);

        Assert.Equal(-0.5, range.XMin, 9);
        Assert.Equal(10.5, range.XMax, 9);
        Assert.Equal(-0.10998, range.YMin, 4);
        Assert.Equal(2.30968, range.YMax, 4);
    }

    [Fact]
    public void UnderClassic_AFillBetween_EndsAtItsDataOnXAndRoundsOutOnY()
    {
        var range = AreaAxes().ComputeDataRanges(Theme.MatplotlibClassic);

        Assert.Equal(new DataRange(0, 10, 0, 2.5), range);
    }

    [Fact]
    public void UnderV2_AFillBetweenX_IsPaddedByTheMarginOnBothAxes()
    {
        double[] y = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        double[] x = [2, 4, 3, 6, 5, 8, 6, 9, 7, 10, 8];
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.FillBetweenX(y, x)).Build().SubPlots[0];

        Assert.Equal(new DataRange(-0.5, 10.5, -0.5, 10.5), Rounded(axes.ComputeDataRanges(Theme.MatplotlibV2)));
    }

    [Fact]
    public void UnderV2_AStackPlot_IsPaddedOnX_AndStandsOnZero()
    {
        var range = StackAxes().ComputeDataRanges(Theme.MatplotlibV2);

        Assert.Equal(-0.5, range.XMin, 9);
        Assert.Equal(10.5, range.XMax, 9);
        Assert.Equal(0, range.YMin, 9);
        Assert.Equal(8.81866, range.YMax, 4);
    }

    [Fact]
    public void UnderTheLibraryTheme_AnAreaAndAStack_KeepTheirStickyEdges()
    {
        var area = AreaAxes().ComputeDataRanges(Theme.Default);
        var stack = StackAxes().ComputeDataRanges(Theme.Default);

        Assert.Equal((0.0, 10.0), (area.XMin, area.XMax));
        Assert.Equal(0, area.YMin);
        Assert.Equal((0.0, 10.0), (stack.XMin, stack.XMax));
    }

    private static DataRange Rounded(DataRange r) =>
        new(Math.Round(r.XMin, 9), Math.Round(r.XMax, 9), Math.Round(r.YMin, 9), Math.Round(r.YMax, 9));

    // ---- the stacked area's look --------------------------------------------------------------------------------------------

    private static List<(Color? Fill, Color? Stroke, double Width)> Layers(Theme theme, Action<StackedAreaSeries>? configure = null)
    {
        var figure = Plt.Create().WithTheme(theme).AddSubPlot(1, 1, 1, ax => ax.StackPlot(X50,
            [[.. X50.Select(v => Math.Sin(v) + 2)], [.. X50.Select(v => Math.Cos(v) + 2)]], configure)).Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        return [.. ctx.Of<DrawPolygonCall>()
            .Select(c => (c.Fill, c.Stroke, c.StrokeThickness))];
    }

    [Fact]
    public void UnderClassic_EveryLayerIsOpaque_AndEdgedInBlackAtOnePoint()
    {
        var layers = Layers(Theme.MatplotlibClassic);

        Assert.Equal(2, layers.Count);
        Assert.All(layers, l =>
        {
            Assert.Equal(255, l.Fill!.Value.A);
            Assert.Equal(Colors.Black, l.Stroke);
            Assert.Equal(OnePoint, l.Width, 9);
        });
    }

    [Fact]
    public void UnderV2_EveryLayerIsOpaque_AndNotEdged()
    {
        var layers = Layers(Theme.MatplotlibV2);

        Assert.All(layers, l =>
        {
            Assert.Equal(255, l.Fill!.Value.A);
            Assert.Null(l.Stroke);
        });
    }

    [Fact]
    public void UnderTheLibraryTheme_EveryLayerIsDrawnAt07_AndAGivenAlphaIsDrawnUnderEveryTheme()
    {
        Assert.All(Layers(Theme.Default), l => Assert.InRange(l.Fill!.Value.A, 178, 179));
        Assert.All(Layers(Theme.MatplotlibV2, s => s.Alpha = 0.7), l => Assert.InRange(l.Fill!.Value.A, 178, 179));
        Assert.All(Layers(Theme.MatplotlibClassic, s => s.Alpha = 0.5), l => Assert.InRange(l.Fill!.Value.A, 127, 128));
    }

    // ---- the legend handles --------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("stack")]
    [InlineData("area")]
    public void UnderClassic_TheLegendHandleOfAFill_IsEdgedInBlack(string kind)
    {
        var figure = Plt.Create().WithTheme(Theme.MatplotlibClassic).AddSubPlot(1, 1, 1, ax => (kind == "stack"
            ? ax.StackPlot(X50, [[.. X50.Select(v => v + 1)]], s => s.Labels = ["A"])
            : ax.FillBetween(X50, [.. X50.Select(v => v + 1)], configure: s => s.Label = "A")).WithLegend()).Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);

        var handle = ctx.Of<DrawRectangleCall>().Last();
        Assert.Equal(Colors.Black, handle.Stroke);
        Assert.Equal(OnePoint, handle.StrokeThickness, 9);
    }

    // ---- the JSON --------------------------------------------------------------------------------------------------------------

    [Fact]
    public void AStacksAlpha_IsWrittenOnlyWhenGiven_AndReadsBackAsGiven()
    {
        var unset = new StackedAreaSeries([0.0, 1.0], [[1.0, 2.0]]);
        var given = new StackedAreaSeries([0.0, 1.0], [[1.0, 2.0]]) { Alpha = 0.7 };

        Assert.Equal(0.7, unset.Alpha);
        Assert.Null(((IGivenAlpha)unset).GivenAlpha);
        Assert.Null(unset.ToSeriesDto().Alpha);
        Assert.Equal(0.7, given.ToSeriesDto().Alpha);

        var figure = Plt.Create().WithTheme(Theme.MatplotlibV2).AddSubPlot(1, 1, 1, ax => ax.StackPlot([0.0, 1.0], [[1.0, 2.0]], s => s.Alpha = 0.7)).Build();
        var back = (StackedAreaSeries)figure.RoundTripped().SubPlots[0].Series[0];
        Assert.Equal(0.7, ((IGivenAlpha)back).GivenAlpha);
    }
}
