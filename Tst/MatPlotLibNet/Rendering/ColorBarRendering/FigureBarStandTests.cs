// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>Where the figure's own colour bar stands in a draw: the box it stands by, the box of every cell it takes its room from,
/// what it clears of the subplots on its edge, and how far it reaches past the plot of an axes on that edge.</summary>
public class FigureBarStandTests
{
    private static readonly double[] X = [0, 1, 2, 3, 4];
    private static readonly SubPlotSpacing Spacing = new() { MarginLeft = 60, MarginRight = 20, MarginTop = 40, MarginBottom = 50, HorizontalGap = 40, VerticalGap = 30 };

    /// <summary>Two subplots side by side under <paramref name="theme"/>, the figure's bar over both.</summary>
    private static Figure Pair(Theme theme, bool visible = true) => Plt.Create().WithSize(800, 600).WithTheme(theme)
        .AddSubPlot(1, 2, 1, ax => ax.Scatter(X, X, s => s.C = X))
        .AddSubPlot(1, 2, 2, ax => ax.Scatter(X, X, s => s.C = X))
        .WithColorBar(c => c with { Visible = visible, Label = "value" })
        .Build();

    private static FigureBarStand StandOf(Figure figure, Rect? cell = null)
    {
        var grid = new FigureGrid(figure);
        return FigureBarStand.Of(figure, grid, grid.PlotRects(Spacing.MarginTop, Spacing), Spacing, cell, ranges: null);
    }

    [Fact]
    public void WithSubplots_TheBarStandsByTheBoxOfEveryPlot()
    {
        var figure = Pair(Theme.Default);
        var grid = new FigureGrid(figure);
        var plots = grid.PlotRects(Spacing.MarginTop, Spacing);

        var stand = FigureBarStand.Of(figure, grid, plots, Spacing, null, null);

        Assert.Equal(Rect.Enclosing(plots), stand.Box);
        Assert.Null(stand.Whole);
    }

    [Fact]
    public void WithoutASubplot_TheBarStandsByWhatTheMarginsLeave_AndTakesItsRoomFromItUnderTheSlot()
    {
        var figure = new Figure { Width = 800, Height = 600, Theme = Theme.MatplotlibV2, FigureColorBar = new ColorBar { Visible = true } };

        var stand = FigureBarStand.Of(figure, new FigureGrid(figure), [], Spacing, null, null);

        Assert.Equal(new Rect(60, 40, 720, 510), stand.Box);
        Assert.Equal(stand.Box, stand.Whole);
    }

    [Fact]
    public void UnderTheSlot_TheBarTakesItsRoomFromTheCellItIsGiven_AndFromNoneWhenNoneIsKnown()
    {
        var figure = Pair(Theme.MatplotlibV2);
        var cell = new Rect(60, 40, 720, 510);

        Assert.Equal(cell, StandOf(figure, cell).Whole);
        Assert.Null(StandOf(figure).Whole);
    }

    [Fact]
    public void AHiddenBar_ReachesPastNoPlot()
    {
        var figure = Pair(Theme.Default, visible: false);
        var stand = StandOf(figure);

        Assert.Null(stand.Bar);
        Assert.Equal(0, stand.PastPlotOf(figure.SubPlots[1], new Rect(470, 40, 310, 510), new SvgRenderContext()));
    }

    [Fact]
    public void TheBar_ReachesPastThePlotOfTheLastColumnOnly()
    {
        var figure = Pair(Theme.Default);
        var stand = StandOf(figure);
        var plots = new FigureGrid(figure).PlotRects(Spacing.MarginTop, Spacing);
        var ctx = new SvgRenderContext();

        double last = stand.PastPlotOf(figure.SubPlots[1], plots[1], ctx);

        Assert.Same(figure.FigureColorBar, stand.Bar);
        Assert.Equal(0, stand.PastPlotOf(figure.SubPlots[0], plots[0], ctx));
        // The bar in the library's margin: past what the last column draws past its right spine, its gap, its body and its text.
        var bar = figure.FigureColorBar!;
        double clearance = stand.ClearanceWith(ctx);
        double expected = clearance + bar.Padding + bar.Width + bar.TextReachBeside(figure.SubPlots.SelectMany(a => a.Series), ctx, figure.Theme);
        Assert.Equal(expected, last, 9);
    }

    [Fact]
    public void AnAxesOutsideTheGrid_IsNotOnTheBarsEdge()
    {
        var figure = Pair(Theme.Default);
        var inset = Pair(Theme.Default).SubPlots[1];

        Assert.Equal(0, StandOf(figure).PastPlotOf(inset, new Rect(470, 40, 310, 510), new SvgRenderContext()));
    }

    [Fact]
    public void APlotThatReachesPastTheBar_IsReachedByNothing()
    {
        var figure = Pair(Theme.Default);

        Assert.Equal(0, StandOf(figure).PastPlotOf(figure.SubPlots[1], new Rect(0, 40, 5000, 510), new SvgRenderContext()));
    }

    /// <summary>The clearance is kept from the first ask: a second ask, with another surface, reads what the first measured, so the
    /// legend that stands past the bar and the bar's drawing read one value.</summary>
    [Fact]
    public void TheClearance_IsKeptFromTheFirstAsk()
    {
        var stand = StandOf(Pair(Theme.Default));

        double first = stand.ClearanceWith(new SvgRenderContext());

        Assert.Equal(first, stand.ClearanceWith(new RecordingRenderContext()));
    }
}
