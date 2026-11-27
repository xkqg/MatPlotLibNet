// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An axis title stands past the tick labels of the line its ticks hang from, so a moved spine takes its title along with its
/// ticks, the X title as the Y title. matplotlib 3.11.2, read with python: <c>ax.spines["bottom"].set_position(("outward", 20))</c> moves
/// the X label's box down 27.78 px (20 points at 100 dpi), and <c>ax.spines["left"].set_position(("outward", 20))</c> moves the Y label's
/// box left by the same; with both spines at a data value the labels stand beside the spines in the middle of the plot.</summary>
public class AxisTitleFollowsItsSpineTests
{
    private static Figure Line(Theme theme, double bottom, double left)
    {
        var figure = Plt.Create().WithSize(600, 400).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 10.0], [0.0, 10.0]).SetXLabel("x title").SetYLabel("y title"))
            .Build();
        var axes = figure.SubPlots[0];
        axes.Spines = axes.Spines with
        {
            Bottom = axes.Spines.Bottom with { Position = SpinePosition.Axes, PositionValue = bottom },
            Left = axes.Spines.Left with { Position = SpinePosition.Axes, PositionValue = left },
        };
        return figure;
    }

    private static Point TitleAt(Figure figure, string title)
    {
        var drawn = new RecordingRenderContext();
        new ChartRenderer().Render(figure, drawn);
        return drawn.Of<DrawTextCall>().Select(call => (call.Text, call.Position))
            .Single(t => t.Item1 == title).Item2;
    }

    private static IEnumerable<Point> Labels(Figure figure, string text)
    {
        var drawn = new RecordingRenderContext();
        new ChartRenderer().Render(figure, drawn);
        return drawn.Of<DrawTextCall>().Where(call => call.Text == text).Select(call => call.Position);
    }

    /// <summary>The X tick label <paramref name="text"/>: the one of that text standing lowest.</summary>
    private static Point LowestLabel(Figure figure, string text) => Labels(figure, text).MaxBy(p => p.Y);

    /// <summary>The Y tick label <paramref name="text"/>: the one of that text standing leftmost.</summary>
    private static Point LeftmostLabel(Figure figure, string text) => Labels(figure, text).MinBy(p => p.X);

    [Theory]
    [InlineData("Default")]
    [InlineData("MatplotlibV2")]
    public void AMovedBottomSpine_TakesTheXTitleAlong_AsAMovedLeftSpineTakesTheYTitle(string name)
    {
        var theme = name.AsTheme();
        var atEdge = Line(theme, 0, 0);
        var moved = Line(theme, -0.1, -0.1);
        // Each figure read from its own plot, laid out with the context it is drawn with: the layout keeps the room the moved labels
        // take, so the plots differ.
        var plot = new ChartRenderer().ComputeLayout(moved, new RecordingRenderContext()).PlotAreas[0];
        var atEdgePlot = new ChartRenderer().ComputeLayout(atEdge, new RecordingRenderContext()).PlotAreas[0];

        double down = (TitleAt(moved, "x title").Y - plot.Bottom) - (TitleAt(atEdge, "x title").Y - atEdgePlot.Bottom);
        double leftward = (plot.X - TitleAt(moved, "y title").X) - (atEdgePlot.X - TitleAt(atEdge, "y title").X);

        // As far as the spine moved its tick labels (the spine stands on the pixel grid: about a tenth of the plot).
        double labelsDown = (LowestLabel(moved, "4").Y - plot.Bottom) - (LowestLabel(atEdge, "4").Y - atEdgePlot.Bottom);
        double labelsLeft = (plot.X - LeftmostLabel(moved, "4").X) - (atEdgePlot.X - LeftmostLabel(atEdge, "4").X);
        Assert.Equal(0.1 * plot.Height, labelsDown, 0);
        Assert.Equal(labelsDown, down, 6);
        Assert.Equal(labelsLeft, leftward, 6);
    }
}
