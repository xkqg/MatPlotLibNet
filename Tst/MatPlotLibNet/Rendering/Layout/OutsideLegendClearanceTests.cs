// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>A legend outside the plot stands its gap past what the axes draws on that edge, by the room the layout keeps there: past
/// the Y tick labels and the Y title on the left, past the mirrored Y tick labels on the right, above the axes title, below the X tick
/// labels and the X title. The room is read by the rules the layout keeps it by (the left stack, the band past the right spine, the
/// title band, the extent below the plot), and the box stays inside the figure, which the layout widened for it.</summary>
public class OutsideLegendClearanceTests
{
    private static Figure Chart(LegendPosition position, Theme theme) =>
        Plt.Create().WithSize(600, 400).WithTheme(theme).TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([0.0, 10.0], [0.0, 1000.0], s => s.Label = "line")
                .WithTitle("Title")
                .SetXLabel("x title")
                .SetYLabel("y title")
                .WithYTicksMirrored()
                .WithLegend(position))
            .Build();

    [Theory]
    [InlineData(LegendPosition.OutsideLeft, "Default")]
    [InlineData(LegendPosition.OutsideRight, "Default")]
    [InlineData(LegendPosition.OutsideTop, "Default")]
    [InlineData(LegendPosition.OutsideBottom, "Default")]
    [InlineData(LegendPosition.OutsideLeft, "MatplotlibV2")]
    [InlineData(LegendPosition.OutsideRight, "MatplotlibV2")]
    [InlineData(LegendPosition.OutsideTop, "MatplotlibV2")]
    [InlineData(LegendPosition.OutsideBottom, "MatplotlibV2")]
    public void AnOutsideLegend_StandsItsGapPastWhatTheAxesDrawsOnThatEdge(LegendPosition position, string themeName)
    {
        var theme = themeName.AsTheme();
        var figure = Chart(position, theme);
        var ctx = new SvgRenderContext();
        var plot = new ChartRenderer().ComputeLayout(figure, ctx).PlotAreas[0];
        var axes = figure.SubPlots[0];
        var renderer = AxesRenderer.Create(axes, plot, ctx, theme);
        var box = Assert.NotNull(renderer.PlaceLegend(() => MarkObstacles.None));
        var bands = renderer.MeasureEdgeBands();
        var needs = new AxesEdgeNeeds();
        const double gap = LegendPositionStrategy.OutsideGap;

        switch (position)
        {
            case LegendPosition.OutsideLeft:
                double left = axes.LeftStackBand(theme, bands.Left, bands.Insets, needs.LeftAxisBands(axes, ctx, theme));
                Assert.True(left > 0);
                Assert.Equal(plot.X - left - gap, box.Right, 6);
                break;
            case LegendPosition.OutsideRight:
                double right = needs.MeasureRightBand(axes, ctx, theme, bands.RightPastEdge).Past;
                Assert.True(right > 0);
                Assert.Equal(plot.Right + right + gap, box.X, 6);
                break;
            case LegendPosition.OutsideTop:
                double top = axes.TitleBand(ctx, theme, bands.TopPastEdge, 0);
                Assert.True(top > 0);
                Assert.Equal(plot.Y - top - gap, box.Bottom, 6);
                break;
            default:
                double bottom = bands.BottomExtent;
                Assert.True(bottom > 0);
                Assert.Equal(plot.Bottom + bottom + gap, box.Y, 6);
                break;
        }

        // The layout kept the room: the box stands inside the figure.
        Assert.InRange(box.X, 0, figure.Width);
        Assert.InRange(box.Right, 0, figure.Width);
        Assert.InRange(box.Y, 0, figure.Height);
        Assert.InRange(box.Bottom, 0, figure.Height);
    }
}
