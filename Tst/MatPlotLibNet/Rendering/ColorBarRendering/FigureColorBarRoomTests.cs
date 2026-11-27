// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Rendering.Frames;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>Under the matplotlib themes the colour bar of the figure takes its room from the subplots it belongs to, as
/// <c>fig.colorbar(mappable, ax=axs)</c> does. matplotlib 3.11.2, <c>colorbar.make_axes(parents, fraction=0.15, shrink=1.0,
/// aspect=20)</c> with <c>_normalize_location_orientation</c>'s pads (right 0.05, bottom 0.15): the box that holds every parent's
/// original position is split into a part of <c>1 - fraction - pad</c> of its width (of its height for a bar below), a pad and a
/// slot of <c>fraction</c>; every parent is moved by the transform that maps the whole box onto the first part
/// (<c>BboxTransform(parents_bbox, pb1)</c>, so the gaps between the parents shrink with them) and anchored towards the bar
/// (<c>panchor</c>); the bar is <c>set_box_aspect(aspect)</c> in the slot, against the parents and centred along it. Measured with
/// matplotlib 3.11.2, 8 x 6 inch at 100 dpi, <c>fig.subplots(1, 2)</c> of <c>imshow(..., aspect='auto')</c> and
/// <c>fig.colorbar(im, ax=axs)</c>: default style, union (100, 72, 620, 462), the parents from x 100 to 596 (0.80 of the union's
/// width, the gap between them 0.80 of what it was), the whole height, the bar at (627, 72), 23.1 wide, 462 tall; classic style,
/// union (100, 60, 620, 480), the bar at (627, 60), 24 wide, 480 tall; <c>location='bottom'</c> in the default style the parents
/// 323.4 tall (0.70 of 462) and the bar at (100, 464.7), 620 wide, 31 tall. The cells are the ones the figure has without the bar,
/// so these tests hold the rule, not the gap the theme leaves between two subplots. The library's own themes keep the cells and
/// draw the bar beside the plots (<see cref="FigureColorBarGeometryTests"/>).</summary>
public sealed class FigureColorBarRoomTests
{
    private static readonly double[,] Data = { { 1, 2 }, { 3, 4 } };

    /// <summary>A figure of two subplots; the figure's bar when <paramref name="bar"/> says one.</summary>
    private static Figure Pair(Theme theme, Func<ColorBar, ColorBar>? bar, double width = 800, double height = 600,
        Func<AxesBuilder, AxesBuilder>? shape = null)
    {
        var builder = Plt.Create().WithSize(width, height).WithTheme(theme)
            .AddSubPlot(1, 2, 1, ax => (shape ?? (a => a))(ax.Heatmap(Data)))
            .AddSubPlot(1, 2, 2, ax => (shape ?? (a => a))(ax.Heatmap(Data)));
        if (bar is not null) builder.WithColorBar(bar);
        return builder.Build();
    }

    private static Figure Pair(Theme theme, double width = 800, double height = 600, Func<AxesBuilder, AxesBuilder>? shape = null) =>
        Pair(theme, b => b, width, height, shape);

    private static Figure Bare(Theme theme, double width = 800, double height = 600) => Pair(theme, null, width, height);

    /// <summary>The plot areas of the draw and every rectangle it drew, measured and drawn by one context.</summary>
    private sealed record Drawn(IReadOnlyList<Rect> Plots, List<Rect> Rectangles);

    private static Drawn Draw(Figure figure)
    {
        var ctx = new RecordingRenderContext();
        var plots = new ChartRenderer().RenderAndLayOut(figure, new DirectFrameTarget(ctx)).PlotAreas;
        return new Drawn(plots, [.. ctx.Rects()]);
    }

    private static void AssertRect(Rect expected, Rect actual, int precision = 3)
    {
        Assert.Equal(expected.X, actual.X, precision);
        Assert.Equal(expected.Y, actual.Y, precision);
        Assert.Equal(expected.Width, actual.Width, precision);
        Assert.Equal(expected.Height, actual.Height, precision);
    }

    private static void AssertDrawn(Rect expected, Drawn drawn) =>
        Assert.True(drawn.Rectangles.Any(r => Math.Abs(r.X - expected.X) < 1e-3 && Math.Abs(r.Y - expected.Y) < 1e-3
                && Math.Abs(r.Width - expected.Width) < 1e-3 && Math.Abs(r.Height - expected.Height) < 1e-3),
            $"no rectangle of {expected} was drawn");

    /// <summary>The cell <paramref name="cell"/> becomes when the box <paramref name="union"/> is mapped onto its first
    /// <paramref name="ratio"/> across (matplotlib's <c>BboxTransform</c>).</summary>
    private static Rect AcrossOnly(Rect cell, Rect union, double ratio) =>
        new(union.X + (cell.X - union.X) * ratio, cell.Y, cell.Width * ratio, cell.Height);

    private static Rect UnionOf(IReadOnlyList<Rect> cells) =>
        new(cells.Min(r => r.X), cells.Min(r => r.Y), cells.Max(r => r.Right) - cells.Min(r => r.X), cells.Max(r => r.Bottom) - cells.Min(r => r.Y));

    [Fact]
    public void V2_EverySubplotShrinksToFourFifthsOfTheUnion_AndTheBarStandsInTheSlotBesideThem()
    {
        var cells = Draw(Bare(Theme.MatplotlibV2)).Plots;
        var union = UnionOf(cells);
        AssertRect(new Rect(100, 72, 620, 462), union, 6);

        var drawn = Draw(Pair(Theme.MatplotlibV2));

        AssertRect(AcrossOnly(cells[0], union, 0.8), drawn.Plots[0], 6);
        AssertRect(AcrossOnly(cells[1], union, 0.8), drawn.Plots[1], 6);
        Assert.Equal(596, drawn.Plots[1].Right, 6);
        AssertDrawn(new Rect(627, 72, 23.1, 462), drawn);
    }

    [Fact]
    public void Classic_TakesTheSameRoom()
    {
        var cells = Draw(Bare(Theme.MatplotlibClassic)).Plots;
        var union = UnionOf(cells);
        AssertRect(new Rect(100, 60, 620, 480), union, 6);

        var drawn = Draw(Pair(Theme.MatplotlibClassic));

        AssertRect(AcrossOnly(cells[0], union, 0.8), drawn.Plots[0], 6);
        AssertRect(AcrossOnly(cells[1], union, 0.8), drawn.Plots[1], 6);
        AssertDrawn(new Rect(627, 60, 24, 480), drawn);
    }

    [Fact]
    public void ABarBelow_TakesAFractionAndAPadOfTheHeightOfTheUnion()
    {
        var cells = Draw(Bare(Theme.MatplotlibV2)).Plots;

        var drawn = Draw(Pair(Theme.MatplotlibV2, b => b with { Orientation = ColorBarOrientation.Horizontal }));

        AssertRect(new Rect(cells[0].X, 72, cells[0].Width, 323.4), drawn.Plots[0], 6);
        AssertRect(new Rect(cells[1].X, 72, cells[1].Width, 323.4), drawn.Plots[1], 6);
        AssertDrawn(new Rect(100, 464.7, 620, 31), drawn);
    }

    /// <summary>Over one subplot the figure's bar is the axes' bar: the same plot area, the same body.</summary>
    [Fact]
    public void OverOneSubplot_TheFigureBarIsTheAxesBar()
    {
        var withFigureBar = Draw(Plt.Create().WithSize(800, 600).WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(Data)).WithColorBar().Build());
        var withAxesBar = Draw(Plt.Create().WithSize(800, 600).WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(Data).WithColorBar()).Build());

        AssertRect(new Rect(100, 72, 496, 462), withAxesBar.Plots[0], 6);
        AssertRect(withAxesBar.Plots[0], withFigureBar.Plots[0], 6);
        AssertDrawn(new Rect(627, 72, 23.1, 462), withFigureBar);
        AssertDrawn(new Rect(627, 72, 23.1, 462), withAxesBar);
    }

    /// <summary>A parent whose shape is fixed stands against the bar: <c>panchor</c> is (1.0, 0.5) for a bar on the right.
    /// Measured, 8 x 2.4 inch, <c>fig.subplots(1, 2)</c> of <c>imshow(6 x 6)</c>: the parents are 184.8 square, each one's right edge
    /// is the right edge of its shrunk cell (325.455 and 596), and the bar is 9.24 wide, as tall as the parents.</summary>
    [Fact]
    public void AnAxesWithAnAspect_StandsAgainstTheBar()
    {
        var cells = Draw(Bare(Theme.MatplotlibV2, 800, 240)).Plots;
        var union = UnionOf(cells);

        var drawn = Draw(Pair(Theme.MatplotlibV2, 800, 240, ax => ax.SetAspect(1)));

        foreach (int i in new[] { 0, 1 })
        {
            var shrunk = AcrossOnly(cells[i], union, 0.8);
            Assert.Equal(shrunk.Right, drawn.Plots[i].Right, 6);
            Assert.Equal(shrunk.Height, drawn.Plots[i].Width, 6);
            Assert.Equal(shrunk.Height, drawn.Plots[i].Height, 6);
        }

        Assert.Equal(596, drawn.Plots[1].Right, 6);
        AssertDrawn(new Rect(627, union.Y, union.Height / 20, union.Height), drawn);
    }

    /// <summary>The library's own themes keep every cell and draw the bar beside the plots, in a margin that holds it.</summary>
    [Fact]
    public void TheLibrarysThemes_KeepTheCells()
    {
        Figure Roomy(bool bar)
        {
            var builder = Plt.Create().WithSize(800, 600).WithTheme(Theme.Default).WithSubPlotSpacing(sp => sp with { MarginRight = 200 })
                .AddSubPlot(1, 2, 1, ax => ax.Heatmap(Data)).AddSubPlot(1, 2, 2, ax => ax.Heatmap(Data));
            if (bar) builder.WithColorBar();
            return builder.Build();
        }

        Assert.Equal(Draw(Roomy(bar: false)).Plots, Draw(Roomy(bar: true)).Plots);
    }

    /// <summary>A hidden bar takes no room.</summary>
    [Fact]
    public void AHiddenBar_TakesNoRoom()
    {
        Assert.Equal(Draw(Bare(Theme.MatplotlibV2)).Plots, Draw(Pair(Theme.MatplotlibV2, b => b with { Visible = false })).Plots);
    }

    /// <summary>The hit test reads the plot areas the drawing used.</summary>
    [Fact]
    public void TheHitTest_ReadsThePlotAreasTheDrawingUsed()
    {
        var figure = Pair(Theme.MatplotlibV2);
        var ctx = new RecordingRenderContext();

        var laidOut = new ChartRenderer().ComputeLayout(figure, ctx).PlotAreas;

        Assert.Equal(Draw(figure).Plots, laidOut);
    }

    private static Figure Narrow(Func<ColorBar, ColorBar> bar, double width, double height) =>
        Plt.Create().WithSize(width, height).WithTheme(Theme.MatplotlibV2).TightLayout().WithSubPlotSpacing(sp => sp with { MarginBottom = 0 })
            .AddSubPlot(1, 2, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 40000 } }))
            .AddSubPlot(1, 2, 2, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 40000 } }))
            .WithColorBar(bar).Build();

    /// <summary>A slot too narrow for the bar's tick labels and its label: a tight layout widens the right margin to what they
    /// reach past the box that holds the subplots, and the pad, as it does for the bar of an axes.</summary>
    [Fact]
    public void ATightLayout_WidensTheRightMarginForWhatTheLabelsReachPastTheSlot()
    {
        var figure = Narrow(b => b with { Label = "counts" }, 480, 600);
        var ctx = new SvgRenderContext();

        var spacing = new ChartRenderer().FrameOf(figure, ctx).Spacing;

        var whole = new FigureGrid(figure).FigureBarCell(spacing.MarginTop, spacing)!.Value;
        double reach = ColorBarRoom.Slot.ReachBeside(figure.FigureColorBar!, whole, figure.SubPlots.SelectMany(a => a.Series), ctx, figure.Theme, 0);
        Assert.True(reach > 0);
        Assert.True(spacing.MarginRight >= reach + ConstrainedLayoutEngine.PadRight - 0.05, $"margin {spacing.MarginRight}, reach {reach}");
    }

    /// <summary>The labels of a bar below stand under its slot: a tight layout widens the bottom margin to what they reach past the
    /// box that holds the subplots, and the pad.</summary>
    [Fact]
    public void ATightLayout_WidensTheBottomMarginForWhatTheLabelsOfABarBelowReachPastTheSlot()
    {
        var figure = Narrow(b => b with { Orientation = ColorBarOrientation.Horizontal, Label = "counts" }, 800, 200);
        var ctx = new SvgRenderContext();

        var spacing = new ChartRenderer().FrameOf(figure, ctx).Spacing;

        var whole = new FigureGrid(figure).FigureBarCell(spacing.MarginTop, spacing)!.Value;
        double reach = ColorBarRoom.Slot.ReachBelow(figure.FigureColorBar!, whole, figure.Theme, 0);
        Assert.True(reach > 0);
        Assert.True(spacing.MarginBottom >= reach + ConstrainedLayoutEngine.PadBottom - 0.05, $"margin {spacing.MarginBottom}, reach {reach}");
    }

    /// <summary>A renderer made outside a draw is told no box: the bar takes the box the plot areas were kept from.</summary>
    [Fact]
    public void WithoutABox_TheBarStandsBesideTheBoxThePlotAreasWereKeptFrom()
    {
        var figure = new Figure { Width = 800, Height = 600, Theme = Theme.MatplotlibV2 };
        var ctx = new RecordingRenderContext();

        new ChartRenderer().RenderFigureColorBar(figure, [new Rect(100, 72, 496, 462)], new ColorBar { Visible = true }, ctx);

        Assert.Contains(ctx.Of<DrawRectangleCall>(), call => (call.Rect) == new Rect(627, 72, 23.1, 462));
    }

    /// <summary>A figure with no subplot has nothing to take the room from: the bar stands in the slot of the box the margins
    /// leave (the default style's 0.125, 0.10, 0.12 and 0.11).</summary>
    [Fact]
    public void AFigureWithNoSubplot_StandsInTheSlotOfTheBoxTheMarginsLeave()
    {
        var figure = new Figure { Width = 800, Height = 600, Theme = Theme.MatplotlibV2, FigureColorBar = new ColorBar { Visible = true } };
        var ctx = new RecordingRenderContext();

        new ChartRenderer().Render(figure, ctx);

        var frame = ctx.Rects().Last();
        AssertRect(new Rect(627, 72, 23.1, 462), frame, 6);
    }

    /// <summary>A figure whose margins leave no room holds no box to shrink the subplots by; the draw stays whole.</summary>
    [Fact]
    public void AFigureWhoseMarginsLeaveNoRoom_ShrinksNothing()
    {
        var figure = Plt.Create().WithSize(100, 300).WithTheme(Theme.MatplotlibV2)
            .WithSubPlotSpacing(sp => sp with { MarginLeft = 80, MarginRight = 80 })
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(Data)).WithColorBar().Build();

        var plot = Draw(figure).Plots[0];

        Assert.True(plot.Width <= 0);
        plot.AssertFinite("plot");
    }
}
