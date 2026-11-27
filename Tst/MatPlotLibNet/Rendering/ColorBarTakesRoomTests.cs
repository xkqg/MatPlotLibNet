// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Under the matplotlib themes an axes' colour bar takes its room from the axes, as <c>fig.colorbar(mappable, ax=ax)</c>
/// does. matplotlib 3.11.2, <c>colorbar.make_axes_gridspec(parent, fraction=0.15, shrink=1.0, aspect=20)</c> with
/// <c>_normalize_location_orientation</c>'s pads (right 0.05, bottom 0.15): the axes' cell is split into the axes, a gap of
/// <c>pad</c> and a slot of <c>fraction</c> of its width (of its height for a bar below), the axes anchored towards the bar
/// (<c>panchor</c>), the bar <c>set_box_aspect(aspect)</c> in its slot, anchored to the slot's side next to the axes, as long as the
/// slot times <c>shrink</c> and centred along it. Measured with matplotlib 3.11.2, an 8 x 6 inch figure at 100 dpi in the default
/// style: <c>imshow(..., aspect='auto')</c> with a bar keeps its axes at x 0.125 .. 0.745 of the figure (0.80 of the cell's 0.775),
/// the bar at 0.78375, 0.028875 wide (the axes' 462 px over 20), the cell's whole height; <c>matshow</c> fits its square image
/// in the axes' part and the bar keeps the cell's height; a bar below keeps the axes at 0.70 of the cell's height, the bar at the
/// top of its slot, as tall as the axes are wide over 20. The library's own themes keep the bar in the margin beside a plot that
/// keeps its cell (<see cref="ColorBar.Padding"/> 10 px away, <see cref="ColorBar.Width"/> 20 px wide).</summary>
public sealed class ColorBarTakesRoomTests
{
    private const double FigureWidth = 800, FigureHeight = 600;

    private static Figure Heatmap(Theme theme, Func<AxesBuilder, AxesBuilder>? more = null, Func<ColorBar, ColorBar>? bar = null) =>
        Plt.Create().WithSize(FigureWidth, FigureHeight).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => (more ?? (a => a))(ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }).WithColorBar(bar)))
            .Build();

    private static void AssertRect(Rect expected, Rect actual, int precision = 6)
    {
        Assert.Equal(expected.X, actual.X, precision);
        Assert.Equal(expected.Y, actual.Y, precision);
        Assert.Equal(expected.Width, actual.Width, precision);
        Assert.Equal(expected.Height, actual.Height, precision);
    }

    // The v2 theme's subplot params: left 0.125, right 0.9, bottom 0.11, top 0.88.
    private static readonly Rect V2Cell = new(100, 72, 620, 462);

    [Fact]
    public void V2_TheAxesKeepsFourFifthsOfItsCell_AndTheBarStandsInTheSlotBesideIt()
    {
        var figure = Heatmap(Theme.MatplotlibV2);

        AssertRect(new Rect(100, 72, 496, 462), figure.FirstPlotArea());
        AssertRect(new Rect(100 + 620 * 0.85, 72, 462 / 20.0, 462), ColorBarRoom.Slot.BodyOf(figure.SubPlots[0].ColorBar!, V2Cell, V2Cell, 0));
    }

    [Fact]
    public void Classic_TakesTheSameRoom()
    {
        // The classic theme's subplot params: left 0.125, right 0.9, bottom 0.1, top 0.9: a cell 620 x 480.
        AssertRect(new Rect(100, 60, 496, 480), Heatmap(Theme.MatplotlibClassic).FirstPlotArea());
    }

    /// <summary>A matrix keeps its square cells in the axes' part, against the bar's side, and the bar keeps the cell's height.</summary>
    [Fact]
    public void AnAxesWithAnAspect_FitsInItsPart_AgainstTheBar_AndTheBarKeepsTheCellsHeight()
    {
        var figure = Plt.Create().WithSize(FigureWidth, FigureHeight).WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.MatShow(new double[6, 9]))
            .Build();

        var plot = figure.FirstPlotArea();
        Assert.Equal(496, plot.Width, 6);
        Assert.Equal(100 + 496, plot.X + plot.Width, 6);
        Assert.True(plot.Height < 462);
        var body = ColorBarRoom.Slot.BodyOf(figure.SubPlots[0].ColorBar!, plot, V2Cell, 0);
        Assert.Equal(462, body.Height, 6);
        Assert.Equal(100 + 620 * 0.85, body.X, 6);
    }

    [Fact]
    public void ABarBelow_TakesAFractionAndAPadOfTheHeight()
    {
        var figure = Heatmap(Theme.MatplotlibV2, bar: b => b with { Orientation = ColorBarOrientation.Horizontal });

        AssertRect(new Rect(100, 72, 620, 462 * 0.70), figure.FirstPlotArea());
        AssertRect(new Rect(100, 72 + 462 * 0.85, 620, 620 / 20.0),
            ColorBarRoom.Slot.BodyOf(figure.SubPlots[0].ColorBar!, V2Cell, V2Cell, 0));
    }

    /// <summary>A shorter bar is centred along its slot, as <c>shrink</c> does.</summary>
    [Fact]
    public void AShrunkBar_IsCentredAlongItsSlot()
    {
        var bar = new ColorBar { Visible = true, Shrink = 0.5 };

        AssertRect(new Rect(100 + 620 * 0.85, 72 + 462 * 0.25, 231 / 20.0, 231), ColorBarRoom.Slot.BodyOf(bar, V2Cell, V2Cell, 0));
    }

    /// <summary>A width or a gap the author gives wins: the gap is then that many pixels, and the slot keeps its fraction.</summary>
    [Fact]
    public void AGivenWidthAndPadding_Win()
    {
        var figure = Heatmap(Theme.MatplotlibV2, bar: b => b with { Width = 30, Padding = 12 });

        AssertRect(new Rect(100, 72, 620 * 0.85 - 12, 462), figure.FirstPlotArea());
        AssertRect(new Rect(100 + 620 * 0.85, 72, 30, 462), ColorBarRoom.Slot.BodyOf(figure.SubPlots[0].ColorBar!, V2Cell, V2Cell, 0));
    }

    /// <summary>The library's own themes keep the bar beside the plot, in the margin: a plot whose margin holds the bar and its
    /// labels keeps its cell.</summary>
    [Fact]
    public void TheLibrarysThemes_KeepTheBarInTheMargin()
    {
        Func<FigureBuilder, FigureBuilder> roomy = fb => fb.WithSubPlotSpacing(sp => sp with { MarginRight = 200 });
        Figure Built(bool bar)
        {
            var builder = roomy(Plt.Create().WithSize(FigureWidth, FigureHeight).WithTheme(Theme.Default));
            return builder.AddSubPlot(1, 1, 1, ax => { var heat = ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }); if (bar) heat.WithColorBar(); }).Build();
        }

        var figure = Built(bar: true);
        var plot = figure.FirstPlotArea();

        var body = ColorBarRoom.Margin.BodyOf(figure.SubPlots[0].ColorBar!, plot, plot, 0);
        AssertRect(new Rect(plot.X + plot.Width + 10, plot.Y, 20, plot.Height), body);
        Assert.Equal(Built(bar: false).FirstPlotArea(), plot);
    }

    /// <summary>A renderer made outside a draw is told no cell: it takes the cell its plot area was kept from.</summary>
    [Fact]
    public void WithoutACell_TheBarStandsBesideTheCellThePlotWasKeptFrom()
    {
        var bar = new ColorBar { Visible = true };

        AssertRect(new Rect(100 + 620 * 0.85, 72, 462 / 20.0, 462), ColorBarRoom.Slot.BodyOf(bar, new Rect(100, 72, 496, 462), null, 0));
    }

    [Fact]
    public void TheThemeCarriesTheRule_AndTheJsonTheGivenWidthAndGap()
    {
        Assert.True(Theme.MatplotlibV2.Looks.ColorBar.TakesRoom);
        Assert.True(Theme.MatplotlibClassic.Looks.ColorBar.TakesRoom);
        Assert.False(Theme.Default.Looks.ColorBar.TakesRoom);
        Assert.True(Theme.MatplotlibV2.Cloned().Looks.ColorBar.TakesRoom);

        var back = Heatmap(Theme.MatplotlibV2, bar: b => b with { Width = 20 }).RoundTripped();
        Assert.True(back.Theme.Looks.ColorBar.TakesRoom);
        Assert.Equal(20, back.SubPlots[0].ColorBar!.GivenWidth);
        Assert.Null(back.SubPlots[0].ColorBar!.GivenPadding);
        Assert.Null(Heatmap(Theme.MatplotlibV2).RoundTripped().SubPlots[0].ColorBar!.GivenWidth);
    }

    /// <summary>A bar given no width that its slot cannot hold at its aspect is the largest box of that aspect the slot holds
    /// (<c>set_box_aspect</c>): a cell 100 wide and 2000 tall has a slot 15 wide, so the bar is 15 wide and 300 long, centred.</summary>
    [Fact]
    public void ABarItsSlotCannotHold_IsTheLargestBoxOfItsAspect()
    {
        var bar = new ColorBar { Visible = true };

        AssertRect(new Rect(85, 850, 15, 300), ColorBarRoom.Slot.BodyOf(bar, default, new Rect(0, 0, 100, 2000), 0));
    }

    /// <summary>An aspect of 0 or less asks for <see cref="ColorBar.Width"/> as the thickness, under every theme.</summary>
    [Fact]
    public void AnAspectOfZero_TakesTheWidth()
    {
        var bar = new ColorBar { Visible = true, Aspect = 0 };

        Assert.Equal(20, ColorBarRoom.Slot.BodyOf(bar, V2Cell, V2Cell, 0).Width, 6);
        Assert.Equal(20, ColorBarRoom.Slot.BodyOf(bar with { Orientation = ColorBarOrientation.Horizontal }, V2Cell, V2Cell, 0).Height, 6);
    }

    /// <summary>A bar below whose renderer was told no cell takes the cell its plot area was kept from, with or without a gap of
    /// its own; it reaches past no side, as its labels stand below it in the slot.</summary>
    [Fact]
    public void ABarBelow_WithoutACell_TakesTheCellThePlotWasKeptFrom()
    {
        var bar = new ColorBar { Visible = true, Orientation = ColorBarOrientation.Horizontal };
        var given = bar with { Padding = 20 };

        AssertRect(new Rect(100, 72 + 462 * 0.85, 620, 31), ColorBarRoom.Slot.BodyOf(bar, new Rect(100, 72, 620, 462 * 0.70), null, 0));
        Assert.Equal(72 + 462 * 0.85, ColorBarRoom.Slot.BodyOf(given, new Rect(100, 72, 620, 462 * 0.85 - 20), null, 0).Y, 6);
        Assert.Equal(0, ColorBarRoom.Slot.ReachBeside(bar, V2Cell, new Axes().Series, new SvgRenderContext(), Theme.MatplotlibV2, 0));
    }

    private static Figure Narrow(int columns)
    {
        var builder = Plt.Create().WithSize(240 * columns, 600).WithTheme(Theme.MatplotlibV2).TightLayout();
        for (int c = 1; c <= columns; c++)
            builder.AddSubPlot(1, columns, c, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 40000 } }).WithColorBar(b => b with { Label = "counts" }));
        return builder.Build();
    }

    /// <summary>A narrow cell's slot cannot hold the bar's labels: a tight layout widens the right margin to what they reach past
    /// the cell it ends up with, and the pad.</summary>
    [Fact]
    public void ATightLayout_WidensTheRightMarginForWhatTheLabelsReachPastTheSlot()
    {
        var figure = Narrow(1);
        var ctx = new SvgRenderContext();
        var spacing = new ConstrainedLayoutEngine().Compute(figure, ctx);
        var cell = new FigureGrid(figure).Cells(spacing.MarginTop, spacing)[0];

        double reach = ColorBarRoom.Slot.ReachBeside(figure.SubPlots[0].ColorBar!, cell, figure.SubPlots[0].Series, ctx, figure.Theme, 0);
        Assert.True(reach > 0);
        Assert.True(spacing.MarginRight >= reach + ConstrainedLayoutEngine.PadRight - 0.05, $"margin {spacing.MarginRight}, reach {reach}");
    }

    /// <summary>The bar of an axes with a neighbour on its right reaches into the gutter, which is widened by it.</summary>
    [Fact]
    public void ATightLayout_WidensTheGutterForTheBarOfAnInteriorAxes()
    {
        var figure = Narrow(2);
        var ctx = new SvgRenderContext();
        var spacing = new ConstrainedLayoutEngine().Compute(figure, ctx);
        var cell = new FigureGrid(figure).Cells(spacing.MarginTop, spacing)[0];

        Assert.True(spacing.HorizontalGap >= ColorBarRoom.Slot.ReachBeside(figure.SubPlots[0].ColorBar!, cell, figure.SubPlots[0].Series, ctx, figure.Theme, 0) - 0.05);
    }

    /// <summary>Under a tight layout the figure keeps room past the cell for what the bar's labels reach beyond its slot.</summary>
    [Fact]
    public void ATightLayout_KeepsTheBarsLabelsInsideTheFigure()
    {
        var figure = Plt.Create().WithSize(FigureWidth, FigureHeight).WithTheme(Theme.MatplotlibV2).TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 40000 } }).WithColorBar(b => b with { Label = "counts" }))
            .Build();
        var ctx = new SvgRenderContext();
        var plot = figure.FirstPlotArea();
        var cell = new Rect(plot.X, plot.Y, plot.Width / 0.8, plot.Height);

        double reach = ColorBarRoom.Slot.ReachBeside(figure.SubPlots[0].ColorBar!, cell, figure.SubPlots[0].Series, ctx, figure.Theme, 0);
        Assert.True(cell.X + cell.Width + reach <= FigureWidth + 1e-6, $"the labels reach {reach} past a cell that ends at {cell.X + cell.Width}");
    }
}
