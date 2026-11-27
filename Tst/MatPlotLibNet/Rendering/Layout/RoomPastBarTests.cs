// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>The room an edge keeps when an outside legend stands past a colour bar there (<c>AxesEdgeNeeds.RoomPastBar</c>), and the
/// room the edge of the figure's own bar keeps for the outside legends of the axes on it (<c>AxesEdgeNeeds.RoomPastFigureBar</c>): what
/// the edge needs for the bar, then the legend's box and a gap on either side.</summary>
public class RoomPastBarTests
{
    private static readonly double[] X = [0, 1, 2, 3, 4];
    private static readonly ColorBar Beside = new() { Visible = true, Orientation = ColorBarOrientation.Vertical };
    private static readonly ColorBar Below = new() { Visible = true, Orientation = ColorBarOrientation.Horizontal };

    private static Axes LegendAt(LegendPosition position) =>
        Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, X, s => s.Label = "a line").WithLegend(position)).Build().SubPlots[0];

    [Theory]
    [InlineData(LegendPosition.OutsideRight, true)]
    [InlineData(LegendPosition.OutsideBottom, false)]
    public void ALegendOnTheBarsEdge_KeepsItsBoxAndAGapOnEitherSidePastTheBar(LegendPosition position, bool beside)
    {
        var axes = LegendAt(position);
        var ctx = new SvgRenderContext();
        var box = LegendLayoutCalculator.MeasureLegendBox(axes, ctx, Theme.Default);

        double room = AxesEdgeNeeds.RoomPastBar(axes, ctx, Theme.Default, beside ? Beside : Below, 50);

        Assert.Equal(50 + (beside ? box.Width : box.Height) + LegendPositionStrategy.OutsideReserve, room, 9);
    }

    [Theory]
    [InlineData(LegendPosition.UpperRight, true)]
    [InlineData(LegendPosition.OutsideRight, false)]
    [InlineData(LegendPosition.OutsideBottom, true)]
    public void ALegendInsideOrOnAnotherEdge_KeepsNothingPastTheBar(LegendPosition position, bool beside) =>
        Assert.Equal(50, AxesEdgeNeeds.RoomPastBar(LegendAt(position), new SvgRenderContext(), Theme.Default, beside ? Beside : Below, 50));

    /// <summary>Two subplots side by side, the legend outside the right of the one at <paramref name="legendAt"/>.</summary>
    private static Figure Pair(int legendAt, bool withBar)
    {
        void Panel(AxesBuilder ax, int at)
        {
            ax.Plot(X, X, s => s.Label = "a line");
            if (at == legendAt) ax.WithLegend(LegendPosition.OutsideRight);
        }

        return Plt.Create().AddSubPlot(1, 2, 1, ax => Panel(ax, 0)).AddSubPlot(1, 2, 2, ax => Panel(ax, 1))
            .WithColorBar(c => c with { Visible = withBar }).Build();
    }

    [Fact]
    public void TheFiguresBar_KeepsRoomForTheOutsideLegendOfTheLastColumn()
    {
        var figure = Pair(legendAt: 1, withBar: true);
        var ctx = new SvgRenderContext();

        double room = AxesEdgeNeeds.RoomPastFigureBar(figure, new FigureGrid(figure), ctx, 50);

        Assert.Equal(AxesEdgeNeeds.RoomPastBar(figure.SubPlots[1], ctx, figure.Theme, figure.FigureColorBar!, 50), room);
        Assert.True(room > 50);
    }

    [Theory]
    [InlineData(0, true, 50)]
    [InlineData(1, false, 50)]
    [InlineData(1, true, 0)]
    public void TheFiguresBar_KeepsOnlyItsOwnNeed_WithNoLegendOnItsEdge_WhenHidden_OrWhenItNeedsNone(int legendAt, bool withBar, double past)
    {
        var figure = Pair(legendAt, withBar);

        Assert.Equal(past, AxesEdgeNeeds.RoomPastFigureBar(figure, new FigureGrid(figure), new SvgRenderContext(), past));
    }
}
