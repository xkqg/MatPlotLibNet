// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>Where the figure-level colour bar stands under the library's own themes, apart from its ticks: the bar of an axes drawn
/// against the bounding box of the plot areas, by the same renderer and the same rule (<see cref="ColorBarRoomExtensions"/>), so it
/// has the wedges and the edges it asks for and its tick labels are in the theme's tick font; a bar below stands under what the
/// bottom row draws under its plots, and every layout keeps the room for the bar and its labels.</summary>
public class FigureColorBarGeometryTests
{
    private static readonly List<Rect> OnePlot = [new Rect(50, 50, 300, 200)];

    private static RecordingRenderContext Draw(ColorBar cb)
    {
        var figure = new Figure { Width = 400, Height = 300, Theme = Theme.Default };
        var ctx = new RecordingRenderContext();
        new ChartRenderer().RenderFigureColorBar(figure, OnePlot, cb, ctx);
        return ctx;
    }

    private static Rect Frame(RecordingRenderContext ctx) => ctx.Of<DrawRectangleCall>()[^1].Rect;

    /// <summary>A vertical bar stands the gap beside the box of the plots, as the bar of an axes stands beside its plot: the layout
    /// keeps the room for it, so it is not pulled in over the plots.</summary>
    [Fact]
    public void AVerticalBar_StandsTheGapBesideTheBoxOfThePlots()
    {
        var frame = Frame(Draw(new ColorBar { Visible = true, Label = "Value" }));

        Assert.Equal(new Rect(360, 50, 20, 200), frame);
    }

    [Fact]
    public void AHorizontalBar_RunsUnderTheBoundingBoxOfThePlots()
    {
        var frame = Frame(Draw(new ColorBar { Visible = true, Orientation = ColorBarOrientation.Horizontal }));

        // 300 px long, 300 / 20 = 15 px tall, 10 px under the plots, centred.
        Assert.Equal(new Rect(50, 260, 300, 15), frame);
    }

    [Theory]
    [InlineData(ColorBarOrientation.Vertical)]
    [InlineData(ColorBarOrientation.Horizontal)]
    public void TheFigureBar_DrawsTheWedgesItIsExtendedWith(ColorBarOrientation orientation)
    {
        var ctx = Draw(new ColorBar { Visible = true, Orientation = orientation, Extend = ColorBarExtend.Both });

        // A wedge at each end, 50 gradient steps and the frame.
        Assert.Equal(53, ctx.Count<DrawRectangleCall>());
    }

    [Theory]
    [InlineData(ColorBarOrientation.Vertical)]
    [InlineData(ColorBarOrientation.Horizontal)]
    public void TheFigureBar_DrawsItsEdgesWhenAskedTo(ColorBarOrientation orientation)
    {
        var ctx = Draw(new ColorBar { Visible = true, Orientation = orientation, DrawEdges = true });

        Assert.Equal(50, ctx.Count<DrawLineCall>());
    }

    [Theory]
    [InlineData(ColorBarOrientation.Vertical)]
    [InlineData(ColorBarOrientation.Horizontal)]
    public void TheFigureBarsTickLabels_AreInTheThemesTickFont(ColorBarOrientation orientation)
    {
        var ctx = Draw(new ColorBar { Visible = true, Orientation = orientation });

        var fonts = ctx.Of<DrawTextCall>().Select(c => c.Font).Distinct().ToArray();
        Assert.Equal([Theme.Default.TickFont()], fonts);
    }

    /// <summary>A tick stands over the gradient, never over a wedge: on a bar extended at its start the lowest value stands
    /// where the gradient starts, past the wedge.</summary>
    [Fact]
    public void AnExtendedFigureBarsTicks_StandOverTheGradient()
    {
        var ctx = Draw(new ColorBar { Visible = true, Orientation = ColorBarOrientation.Horizontal, Extend = ColorBarExtend.Min });

        double lowest = ctx.Of<DrawTextCall>().Min(c => c.Position.X);
        // The bar runs from 50 to 350; the wedge takes its first 30 px, and the range starts at a tick (0).
        Assert.Equal(80, lowest, 9);
    }

    /// <summary>The bar of an axes and the bar of the figure are one body: a bar under the same rectangle, extended both ways, with
    /// its edges drawn and a label, is drawn step for step, edge for edge, tick for tick and label for label the same.</summary>
    [Theory]
    [InlineData(ColorBarOrientation.Vertical)]
    [InlineData(ColorBarOrientation.Horizontal)]
    public void AFigureBar_IsTheAxesBarUnderTheSameRectangle(ColorBarOrientation orientation)
    {
        var plot = OnePlot[0];
        var cb = new ColorBar { Visible = true, Orientation = orientation, Extend = ColorBarExtend.Both, DrawEdges = true, Label = "Value" };
        var axesCtx = new RecordingRenderContext();
        MatPlotLibNet.Rendering.ColorBarRendering.ColorBarRendererFactory.Create(cb, ColorMaps.Viridis, 0, 1, plot, axesCtx, Theme.Default).Render();
        var figureCtx = Draw(cb);

        Assert.Equal(Body(axesCtx), Body(figureCtx));
    }

    private static List<CallRecord> Body(RecordingRenderContext ctx) =>
        [.. ctx.Calls.Where(c => c is DrawRectangleCall or DrawLineCall or DrawTextCall)];

    // ---- the layout keeps the room, and the bar below stands under what the bottom row draws --------------------------------

    private const double FigureWidth = 500, FigureHeight = 400;

    private static Action<FigureBuilder> LayoutOf(string layout) => layout switch
    {
        "tight" => fb => fb.TightLayout(),
        "constrained" => fb => fb.ConstrainedLayout(),
        _ => _ => { },
    };

    /// <summary>Two heatmaps with an X axis title each, side by side under the library's default theme, and the figure's bar.</summary>
    private static Figure Pair(string layout, ColorBarOrientation orientation, Func<SubPlotSpacing, SubPlotSpacing>? spacing = null, bool bar = true)
    {
        var builder = Plt.Create().WithTheme(Theme.Default).WithSize(FigureWidth, FigureHeight);
        LayoutOf(layout)(builder);
        if (spacing is not null) builder.WithSubPlotSpacing(spacing);
        builder.AddSubPlot(1, 2, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }).SetXLabel("X axis"))
            .AddSubPlot(1, 2, 2, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }).SetXLabel("X axis"));
        if (bar) builder.WithColorBar(b => b with { Orientation = orientation, Label = "intensity" });
        return builder.Build();
    }

    private static XElement BarGroup(string svg) => svg.Each("g").First(e => (string?)e.Attribute("class") == "figure-colorbar");

    /// <summary>Every text the figure's bar draws: its value, the point it is written at and whether it is turned.</summary>
    private static List<(string Text, double X, double Y, bool Turned)> BarTexts(string svg) =>
        [.. BarGroup(svg).Each("text").Select(e => (e.Value,
            e.Number("x"), e.Number("y"),
            e.Attribute("transform") is not null))];

    /// <summary>The top of the bar's body: the y of the first rectangle of the bar's group.</summary>
    private static double BarTop(string svg) =>
        BarGroup(svg).Each("rect").First().Number("y");

    /// <summary>The bar below stood the gap under the plots, on their X tick labels and X axis titles, and its label stood past the
    /// figure's foot: it now stands under the titles and the layout keeps its whole stack inside the figure.</summary>
    [Theory]
    [InlineData("default")]
    [InlineData("tight")]
    [InlineData("constrained")]
    public void ABarBelow_StandsUnderTheXAxisTitlesOfTheBottomRow_AndItsLabelStaysInTheFigure(string layout)
    {
        var figure = Pair(layout, ColorBarOrientation.Horizontal);
        string svg = figure.ToSvg();

        double title = svg.Elements("text").Where(text => text.Text == "X axis").Select(text => text.Number("y")).Max();
        double label = BarTexts(svg).Single(t => t.Text == "intensity").Y;
        double descent = FacingInk.Descent * figure.Theme.LabelFont().Size;

        Assert.True(BarTop(svg) >= title + descent, $"the bar starts at {BarTop(svg)}, the X axis titles' baseline is at {title}: the bar covers them");
        Assert.True(label + descent <= FigureHeight, $"the bar's label ends at {label + descent} in a figure {FigureHeight} tall");
    }

    /// <summary>An axes of the bottom row with a bar of its own below it: the figure's bar stands under that bar's label, and the
    /// layout keeps both stacks inside the figure.</summary>
    [Fact]
    public void ABarBelow_StandsUnderTheOwnBarOfAnAxesOfTheBottomRow()
    {
        var figure = Plt.Create().WithTheme(Theme.Default).WithSize(FigureWidth, FigureHeight)
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })
                .WithColorBar(b => b with { Orientation = ColorBarOrientation.Horizontal, Label = "own" }))
            .WithColorBar(b => b with { Orientation = ColorBarOrientation.Horizontal, Label = "intensity" }).Build();
        string svg = figure.ToSvg();
        double descent = FacingInk.Descent * figure.Theme.LabelFont().Size;

        double own = svg.Elements("text").Where(text => text.Text == "own").Select(text => text.Number("y")).Single();
        double label = BarTexts(svg).Single(t => t.Text == "intensity").Y;

        Assert.True(BarTop(svg) >= own + descent, $"the figure's bar starts at {BarTop(svg)}, the axes' bar's label is at {own}");
        Assert.True(label + descent <= FigureHeight, $"the bar's label ends at {label + descent} in a figure {FigureHeight} tall");
    }

    /// <summary>Two rows: the bar below clears the bottom row, whose X axis title is the lowest thing drawn under the plots.</summary>
    [Fact]
    public void ABarBelowTwoRows_StandsUnderTheBottomRowsXAxisTitle()
    {
        var figure = Plt.Create().WithTheme(Theme.Default).WithSize(FigureWidth, FigureHeight)
            .AddSubPlot(2, 1, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }).SetXLabel("upper"))
            .AddSubPlot(2, 1, 2, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }).SetXLabel("lower"))
            .WithColorBar(b => b with { Orientation = ColorBarOrientation.Horizontal, Label = "intensity" }).Build();
        string svg = figure.ToSvg();
        double descent = FacingInk.Descent * figure.Theme.LabelFont().Size;

        double lower = svg.Elements("text").Where(text => text.Text == "lower").Select(text => text.Number("y")).Single();

        // The gap under the title's ink (its descent, as the band measures the foot of the text, is a little deeper than the one the
        // library keeps for the ink of a label).
        Assert.InRange(BarTop(svg), lower + descent + ColorBar.RestingPadding, lower + descent + ColorBar.RestingPadding + 5);
        Assert.True(BarTexts(svg).Single(t => t.Text == "intensity").Y + descent <= FigureHeight);
    }

    /// <summary>The bar beside the plots, its tick labels and its label stay inside the figure in every layout.</summary>
    [Theory]
    [InlineData("default")]
    [InlineData("tight")]
    [InlineData("constrained")]
    public void ABarBeside_ItsTickLabelsAndItsLabel_StayInTheFigure(string layout)
    {
        var figure = Pair(layout, ColorBarOrientation.Vertical);
        var measure = new SvgRenderContext();

        foreach (var (text, x, _, turned) in BarTexts(figure.ToSvg()))
        {
            var font = turned ? figure.Theme.LabelFont() : figure.Theme.TickFont();
            var size = measure.MeasureText(text, font);
            double right = turned ? x + size.Height / 2 : x + size.Width;
            Assert.True(right <= FigureWidth + 0.01, $"'{text}' ends at {right} in a figure {FigureWidth} wide");
        }
    }

    /// <summary>A default layout whose margins already hold the bar and its labels keeps the plots where they are without it.</summary>
    [Theory]
    [InlineData(ColorBarOrientation.Vertical)]
    [InlineData(ColorBarOrientation.Horizontal)]
    public void TheDefaultLayout_KeepsTheAuthorsMargins_WhereTheBarFitsInThem(ColorBarOrientation orientation)
    {
        Func<SubPlotSpacing, SubPlotSpacing> roomy = sp => sp with { MarginRight = 150, MarginBottom = 180 };
        var ctx = new SvgRenderContext();

        Assert.Equal(new ChartRenderer().ComputeLayout(Pair("default", orientation, roomy, bar: false), ctx).PlotAreas,
            new ChartRenderer().ComputeLayout(Pair("default", orientation, roomy), ctx).PlotAreas);
    }
}
