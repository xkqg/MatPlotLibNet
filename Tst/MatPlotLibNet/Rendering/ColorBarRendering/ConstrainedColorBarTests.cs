// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Rendering.Frames;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>Under the constrained layout and a matplotlib theme a colour bar stands as matplotlib's constrained layout stands it,
/// not in a slot of the axes' cell. matplotlib 3.11.2: <c>Figure.colorbar</c> calls <c>make_axes</c> (not
/// <c>make_axes_gridspec</c>) when the layout engine's <c>colorbar_gridspec</c> is false, with <c>pad0 = 0.05 if
/// fig.get_constrained_layout()</c> for every location; <c>_constrained_layout.reposition_colorbar</c> then puts the bar
/// <c>colorbar_get_pad</c> (pad times the parent's inner width, its height for a bar below) past the parent's box with its
/// decorations (<c>get_bbox_for_cb</c>), in a box of <c>fraction</c> (0.15) of the parent across and <c>shrink</c> along, at
/// <c>set_box_aspect(aspect)</c>, and keeps the bar's whole tight box in the layout's margin (<c>make_layout_margins</c>:
/// <c>margin['rightcb'] += cbbbox.width + pad</c>). Printed for <c>fig, ax = plt.subplots(figsize=(6.4, 4.8),
/// layout='constrained')</c>, <c>imshow(aspect='auto')</c> and <c>fig.colorbar(im, ax=ax)</c>: default style, the axes 483.92 x
/// 442.78 ending at x 540.81, its tight box at 556.31, the bar at x 580.51 (556.31 + 0.05 x 483.92), 22.14 wide (442.78 / 20) and
/// 442.78 tall; classic style (80 dpi), the axes 396.8 wide, the bar 0.05 x 396.8 = 19.84 past the axes' tight box, 17.64 wide
/// (352.89 / 20). A tight layout keeps the slot of the cell (<see cref="ColorBarTakesRoomTests"/>).</summary>
public sealed class ConstrainedColorBarTests
{
    private static readonly double[,] Data = { { 0, 1 }, { 2, 3 } };

    private static Figure Heatmap(Theme theme, bool constrained, Func<ColorBar, ColorBar>? bar = null, double width = 640, double height = 480)
    {
        var builder = Plt.Create().WithSize(width, height).WithTheme(theme);
        if (constrained) builder.ConstrainedLayout(); else builder.TightLayout();
        return builder.AddSubPlot(1, 1, 1, ax => ax.Heatmap(Data).WithColorBar(bar)).Build();
    }

    private sealed record Drawn(Rect Plot, List<Rect> Rectangles, EdgeBands Bands);

    private static Drawn Draw(Figure figure)
    {
        var ctx = new RecordingRenderContext();
        var plot = new ChartRenderer().RenderAndLayOut(figure, new DirectFrameTarget(ctx)).PlotAreas[0];
        var bands = AxesRenderer.Create(figure.SubPlots[0], plot, ctx, figure.Theme).MeasureEdgeBands();
        return new Drawn(plot, [.. ctx.Rects()], bands);
    }

    private static void AssertDrawn(Rect expected, Drawn drawn) =>
        Assert.True(drawn.Rectangles.Any(r => Math.Abs(r.X - expected.X) < 1e-6 && Math.Abs(r.Y - expected.Y) < 1e-6
                && Math.Abs(r.Width - expected.Width) < 1e-6 && Math.Abs(r.Height - expected.Height) < 1e-6),
            $"no rectangle of {expected} was drawn; plot {drawn.Plot}");

    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    public void TheBarStandsAPadOfTheAxesWidthPastWhatTheAxesDrawsBesideIt_AsThickAsItsHeightOverItsAspect(string style)
    {
        var theme = style.AsTheme();
        var drawn = Draw(Heatmap(theme, constrained: true));
        var plot = drawn.Plot;
        double past = Math.Max(drawn.Bands.Right, drawn.Bands.Overhang.Right);

        AssertDrawn(new Rect(plot.X + plot.Width + past + 0.05 * plot.Width, plot.Y, plot.Height / 20, plot.Height), drawn);
    }

    /// <summary>The axes keeps all of its cell but the bar's stack: the bar's label, the outermost thing it draws, ends the pad from
    /// the figure's right edge.</summary>
    [Fact]
    public void TheLayoutKeepsTheBarsWholeStackInTheMargin_AndNoMore()
    {
        var figure = Heatmap(Theme.MatplotlibV2, constrained: true, b => b with { Label = "value" });
        var group = figure.ToSvg().Each("g").First(e => (string?)e.Attribute("class") == "colorbar");
        var label = group.Each("text").Single(e => e.Value == "value");
        double x = label.Number("x");
        var measure = new SvgRenderContext();
        double half = measure.MeasureText("value", figure.Theme.LabelFont()).Height / 2;
        // The layout keeps room for the widest label the bar can have (at least a six-character number); the label stands past the
        // widest it has, so it ends at most that estimate short of the room kept.
        double estimate = measure.MeasureText("0.0000", figure.Theme.TickFont()).Width;

        Assert.InRange(x + half, 640 - ConstrainedLayoutEngine.PadRight - estimate, 640 - ConstrainedLayoutEngine.PadRight + 0.01);
    }

    /// <summary>An axes whose shape is fixed keeps its cell and stands against its bar, as <c>make_axes</c> anchors each parent
    /// (<c>panchor</c> (1.0, 0.5) for a bar on the right); the figure's bar anchors the subplots the same way.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnAxesWithAnAspect_StandsAgainstItsBar(bool figureBar)
    {
        var builder = Plt.Create().WithSize(800, 300).WithTheme(Theme.MatplotlibV2).ConstrainedLayout()
            .AddSubPlot(1, 1, 1, ax => { var h = ax.Heatmap(Data).SetAspect(1); if (!figureBar) h.WithColorBar(); });
        if (figureBar) builder.WithColorBar();
        var figure = builder.Build();
        var ctx = new SvgRenderContext();
        var frame = new ChartRenderer().FrameOf(figure, ctx);
        var cell = new FigureGrid(figure).Cells(frame.PlotAreaTop, frame.Spacing)[0];

        Assert.True(frame.PlotAreas[0].Width < cell.Width - 1, "the aspect narrows the plot");
        Assert.Equal(cell.X + cell.Width, frame.PlotAreas[0].X + frame.PlotAreas[0].Width, 6);
    }

    [Fact]
    public void TheClearance_IsWhatTheAxesDrawsPastItsSpineOnTheBarsSide()
    {
        var bands = new EdgeBands(0, Right: 12, 0, Bottom: 22, BottomTitle: 44, Overhang: new SideOverhang(0, 18));
        var beside = new ColorBar { Visible = true };
        var below = beside with { Orientation = ColorBarOrientation.Horizontal };

        Assert.Equal(18, ColorBarRoom.ConstrainedMargin.ClearanceOf(beside, new MeasuredBands(bands, 0)));
        Assert.Equal(12, ColorBarRoom.ConstrainedMargin.ClearanceOf(beside, new MeasuredBands(bands with { Overhang = default }, 0)));
        Assert.Equal(44, ColorBarRoom.ConstrainedMargin.ClearanceOf(below, new MeasuredBands(bands, 0)));
        Assert.Equal(0, ColorBarRoom.Margin.ClearanceOf(beside, new MeasuredBands(bands, 0)));
        Assert.Equal(0, ColorBarRoom.Slot.ClearanceOf(below, new MeasuredBands(bands, 0)));
        // Beside the plot in the library's margin: everything drawn past the right spine, which its renderer or the layout measured.
        Assert.Equal(37, ColorBarRoom.Margin.ClearanceOf(beside, new MeasuredBands(bands, 37)));
        Assert.Equal(44, ColorBarRoom.Margin.ClearanceOf(below, new MeasuredBands(bands, 37)));
        Assert.Equal(0, ColorBarRoom.Slot.ClearanceOf(beside, new MeasuredBands(bands, 37)));
        // Under the constrained layout the farthest of the overhanging X tick label and everything past the right spine (a twin's axis).
        Assert.Equal(37, ColorBarRoom.ConstrainedMargin.ClearanceOf(beside, new MeasuredBands(bands, 37)));
        Assert.Equal(18, ColorBarRoom.ConstrainedMargin.ClearanceOf(beside, new MeasuredBands(bands, 5)));
    }

    [Fact]
    public void InTheLibrarysMargin_TheBarBesideStandsAndReachesPastTheClearance()
    {
        var beside = new ColorBar { Visible = true };
        var plot = new Rect(50, 20, 300, 200);
        var ctx = new SvgRenderContext();

        var body = ColorBarRoom.Margin.BodyOf(beside, plot, null, 37);
        double reach = ColorBarRoom.Margin.ReachBeside(beside, plot, [], ctx, Theme.Default, 37);
        double bare = ColorBarRoom.Margin.ReachBeside(beside, plot, [], ctx, Theme.Default, 0);

        Assert.Equal(plot.Right + 37 + beside.Padding, body.X, 9);
        Assert.Equal(bare + 37, reach, 9);
    }

    [Fact]
    public void ThePlacement_FollowsTheThemeAndTheConstrainedLayout()
    {
        Assert.Same(ColorBarRoom.Margin, ColorBarRoom.Under(Theme.Default, constrained: true));
        Assert.Same(ColorBarRoom.Slot, ColorBarRoom.Under(Theme.MatplotlibV2, constrained: false));
        Assert.Same(ColorBarRoom.ConstrainedMargin, ColorBarRoom.Under(Theme.MatplotlibClassic, constrained: true));
    }

    /// <summary>Without a known plot the constrained layout's bar has no size: it reaches nothing, which the layout settles once
    /// the plot is known.</summary>
    [Fact]
    public void WithoutAPlot_TheConstrainedBarReachesNothing()
    {
        var beside = new ColorBar { Visible = true };
        var ctx = new SvgRenderContext();

        Assert.Equal(0, ColorBarRoom.ConstrainedMargin.ReachBeside(beside, null, [], ctx, Theme.MatplotlibV2, 0));
        Assert.Equal(0, ColorBarRoom.ConstrainedMargin.ReachBelow(beside with { Orientation = ColorBarOrientation.Horizontal }, null, Theme.MatplotlibV2, 0));
    }

    /// <summary>A bar below an upper row and a bar beside an interior column widen the gap to the neighbour; a bar below the last row
    /// widens the bottom margin.</summary>
    [Fact]
    public void ABarOfAnInteriorAxes_WidensTheGap()
    {
        Figure Grid(bool bars) => Plt.Create().WithSize(800, 600).WithTheme(Theme.MatplotlibV2).ConstrainedLayout()
            .AddSubPlot(2, 2, 1, ax => { var h = ax.Heatmap(Data); if (bars) h.WithColorBar(); })
            .AddSubPlot(2, 2, 2, ax => { var h = ax.Heatmap(Data); if (bars) h.WithColorBar(b => b with { Orientation = ColorBarOrientation.Horizontal }); })
            .AddSubPlot(2, 2, 3, ax => { var h = ax.Heatmap(Data); if (bars) h.WithColorBar(b => b with { Orientation = ColorBarOrientation.Horizontal }); })
            .AddSubPlot(2, 2, 4, ax => ax.Heatmap(Data)).Build();
        var ctx = new SvgRenderContext();

        var with = new ChartRenderer().FrameOf(Grid(true), ctx).Spacing;
        var without = new ChartRenderer().FrameOf(Grid(false), ctx).Spacing;

        Assert.True(with.HorizontalGap > without.HorizontalGap, $"gap {with.HorizontalGap} against {without.HorizontalGap}");
        Assert.True(with.MarginBottom > without.MarginBottom, $"bottom {with.MarginBottom} against {without.MarginBottom}");
        Assert.True(with.VerticalGap > without.VerticalGap, $"row gap {with.VerticalGap} against {without.VerticalGap}");
    }

    /// <summary>The axes is not narrowed to the 0.80 of its cell the gridspec rule keeps: the bar takes only its stack.</summary>
    [Fact]
    public void TheAxesKeepsMoreThanTheGridspecSlotLeavesIt()
    {
        var constrained = Draw(Heatmap(Theme.MatplotlibV2, constrained: true)).Plot;
        var tight = Draw(Heatmap(Theme.MatplotlibV2, constrained: false)).Plot;

        Assert.True(constrained.Width > tight.Width, $"constrained {constrained.Width}, tight {tight.Width}");
    }

    /// <summary>A tight layout keeps matplotlib's gridspec rule: the gap is 0.05 of the cell, so 0.05 / 0.80 of the axes.</summary>
    [Fact]
    public void ATightLayout_KeepsTheGridspecSlot()
    {
        var drawn = Draw(Heatmap(Theme.MatplotlibV2, constrained: false));
        var plot = drawn.Plot;
        double cellWidth = plot.Width / 0.8;

        AssertDrawn(new Rect(plot.X + cellWidth * 0.85, plot.Y, plot.Height / 20, plot.Height), drawn);
    }

    /// <summary>A bar below stands a pad of the axes' height under what the axes draws below its plot, as thick as the axes is wide
    /// over the aspect, as long as the axes is wide.</summary>
    [Fact]
    public void ABarBelow_StandsAPadOfTheAxesHeightUnderItsLabels()
    {
        var drawn = Draw(Heatmap(Theme.MatplotlibV2, constrained: true, b => b with { Orientation = ColorBarOrientation.Horizontal }));
        var plot = drawn.Plot;

        AssertDrawn(new Rect(plot.X, plot.Y + plot.Height + drawn.Bands.BottomExtent + 0.05 * plot.Height, plot.Width, plot.Width / 20), drawn);
    }

    /// <summary>The figure's bar stands the same way past the box of every plot: a pad of the box's width away, as thick as the box
    /// is tall over the aspect.</summary>
    [Fact]
    public void TheFiguresBar_StandsAPadOfTheBoxWidthPastThePlots()
    {
        var figure = Plt.Create().WithSize(800, 400).WithTheme(Theme.MatplotlibV2).ConstrainedLayout()
            .AddSubPlot(1, 2, 1, ax => ax.Heatmap(Data)).AddSubPlot(1, 2, 2, ax => ax.Heatmap(Data)).WithColorBar().Build();
        var ctx = new RecordingRenderContext();
        var plots = new ChartRenderer().RenderAndLayOut(figure, new DirectFrameTarget(ctx)).PlotAreas;
        var box = new Rect(plots[0].X, plots.Min(p => p.Y), plots[1].X + plots[1].Width - plots[0].X, plots.Max(p => p.Y + p.Height) - plots.Min(p => p.Y));
        var bands = AxesRenderer.Create(figure.SubPlots[1], plots[1], ctx, figure.Theme).MeasureEdgeBands();
        double past = Math.Max(bands.Right, bands.Overhang.Right);
        var drawn = new Drawn(box, [.. ctx.Rects()], bands);

        AssertDrawn(new Rect(box.X + box.Width + past + 0.05 * box.Width, box.Y, box.Height / 20, box.Height), drawn);
    }
}
