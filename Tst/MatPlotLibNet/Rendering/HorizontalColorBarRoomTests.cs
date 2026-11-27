// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A colour bar below its axes has tick labels and a label under it, and a tight or constrained layout keeps room for them,
/// as it does for the labels of a bar beside the axes (<see cref="ColorBarRoom.ReachBeside"/>): under the library's own
/// themes the bar stands in the bottom margin, so the margin holds the whole stack (the gap, the bar, its tick labels and its label);
/// under the matplotlib themes the bar stands in a slot inside the cell and the margin (or the gap to the row below) holds what
/// reaches past the cell (<see cref="ColorBarRoom.ReachBelow"/>). The labels were cut off by the figure's edge before.</summary>
public sealed class HorizontalColorBarRoomTests
{
    private const double Width = 500;

    /// <summary>The v2 theme with a large font, as a figure for a talk has: the bar's tick labels and label stand far enough below
    /// its slot to reach past the margin the tight layout keeps under a plot with no X axis drawn (50 px at the least).</summary>
    private static readonly Theme LargeV2 = new ThemeBuilder(Theme.MatplotlibV2).WithFont(f => f with { Size = 30 }).Build();

    private static Figure Heatmap(Theme theme, double height, Action<FigureBuilder>? layout = null, int rows = 1, bool hideAxes = false)
    {
        var builder = Plt.Create().WithTheme(theme).WithSize(Width, height);
        layout?.Invoke(builder);
        for (int row = 1; row <= rows; row++)
            builder.AddSubPlot(rows, 1, row, ax => (hideAxes ? ax.HideAllAxes() : ax).Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })
                .WithColorBar(b => b with { Orientation = ColorBarOrientation.Horizontal, Label = "intensity" }));
        return builder.Build();
    }

    /// <summary>The y (the baseline) of every text <paramref name="text"/> the figure draws.</summary>
    private static double[] BaselinesOf(Figure figure, string text) =>
        [.. figure.ToSvg().Elements("text").Where(element => element.Text == text).Select(element => element.Number("y"))];

    /// <summary>The lowest the bar's label ink goes: its baseline and the descent the library keeps below one.</summary>
    private static double InkBottom(double baseline, Theme theme) => baseline + FacingInk.Descent * theme.LabelFont().Size;

    /// <summary>The layout a test lays its figure out with: the default one, the tight one or the constrained one.</summary>
    private static Action<FigureBuilder> LayoutOf(string layout) => layout switch
    {
        "tight" => fb => fb.TightLayout(),
        "constrained" => fb => fb.ConstrainedLayout(),
        _ => _ => { },
    };

    [Theory]
    [InlineData("default")]
    [InlineData("tight")]
    [InlineData("constrained")]
    public void UnderTheLibrarysThemes_TheBottomMarginHoldsTheBarsLabel(string layout)
    {
        var figure = Heatmap(Theme.Default, 400, LayoutOf(layout));

        double[] baselines = BaselinesOf(figure, "intensity");

        Assert.Single(baselines);
        Assert.True(InkBottom(baselines[0], figure.Theme) <= 400, $"the label's ink reaches {InkBottom(baselines[0], figure.Theme)} in a figure 400 tall");
    }

    [Fact]
    public void UnderTheLibrarysThemes_AWideBarIsAsThickAsItsLengthOverItsAspect_AndTheMarginHoldsIt()
    {
        var figure = Plt.Create().WithTheme(Theme.Default).WithSize(1500, 400).TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })
                .WithColorBar(b => b with { Orientation = ColorBarOrientation.Horizontal, Label = "intensity" }))
            .Build();

        double[] baselines = BaselinesOf(figure, "intensity");

        Assert.Single(baselines);
        Assert.True(InkBottom(baselines[0], figure.Theme) <= 400, $"the label's ink reaches {InkBottom(baselines[0], figure.Theme)} in a figure 400 tall");
    }

    [Fact]
    public void UnderTheLibrarysThemes_AFigureWithNoBarKeepsItsBottomMargin()
    {
        var plain = Plt.Create().WithTheme(Theme.Default).WithSize(Width, 400).TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })).Build();
        var vertical = Plt.Create().WithTheme(Theme.Default).WithSize(Width, 400).TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }).WithColorBar()).Build();

        var ctx = new SvgRenderContext();
        Assert.Equal(new ConstrainedLayoutEngine().Compute(plain, ctx).MarginBottom, new ConstrainedLayoutEngine().Compute(vertical, ctx).MarginBottom);
    }

    [Fact]
    public void UnderTheMatplotlibThemes_ABarsLabelThatReachesPastItsSlotStaysInTheFigure()
    {
        var figure = Heatmap(LargeV2, 300, fb => fb.TightLayout(), hideAxes: true);

        double[] baselines = BaselinesOf(figure, "intensity");

        Assert.Single(baselines);
        Assert.True(InkBottom(baselines[0], figure.Theme) <= 300, $"the label's ink reaches {InkBottom(baselines[0], figure.Theme)} in a figure 300 tall");
    }

    [Fact]
    public void UnderTheMatplotlibThemes_TheMarginHoldsWhatReachesPastTheCell()
    {
        var figure = Heatmap(LargeV2, 300, fb => fb.TightLayout(), hideAxes: true);
        var ctx = new SvgRenderContext();
        var spacing = new ConstrainedLayoutEngine().Compute(figure, ctx);
        var cell = new FigureGrid(figure).Cells(spacing.MarginTop, spacing)[0];

        double reach = ColorBarRoom.Slot.ReachBelow(figure.SubPlots[0].ColorBar!, cell, figure.Theme, 0);

        Assert.True(reach > 0);
        Assert.True(spacing.MarginBottom >= reach + ConstrainedLayoutEngine.PadBottom - 0.05, $"margin {spacing.MarginBottom}, reach {reach}");
    }

    [Fact]
    public void UnderTheMatplotlibThemes_TheGapBelowAnUpperRowHoldsWhatReachesPastItsCell()
    {
        var figure = Heatmap(LargeV2, 500, fb => fb.TightLayout(), rows: 2, hideAxes: true);
        var ctx = new SvgRenderContext();
        var spacing = new ConstrainedLayoutEngine().Compute(figure, ctx);
        var cells = new FigureGrid(figure).Cells(spacing.MarginTop, spacing);

        double reach = ColorBarRoom.Slot.ReachBelow(figure.SubPlots[0].ColorBar!, cells[0], figure.Theme, 0);

        Assert.True(reach > 0);
        Assert.True(spacing.VerticalGap >= reach - 0.05, $"gap {spacing.VerticalGap}, reach {reach}");
    }

    [Fact]
    public void ABarBeside_ReachesNothingBelow_AndABarBelow_ReachesNothingBeside()
    {
        var ctx = new SvgRenderContext();
        var cell = new Rect(100, 72, 620, 462);
        var beside = new ColorBar { Visible = true, Label = "counts" };
        var below = new ColorBar { Visible = true, Label = "counts", Orientation = ColorBarOrientation.Horizontal };
        var axes = new Axes();

        Assert.Equal(0, ColorBarRoom.Slot.ReachBelow(beside, cell, Theme.MatplotlibV2, 0));
        Assert.Equal(0, ColorBarRoom.Slot.ReachBeside(below, cell, axes.Series, ctx, Theme.MatplotlibV2, 0));
    }

    /// <summary>A roomy slot holds the bar's tick labels and label, so nothing reaches past the cell and the margins stay as they were.</summary>
    [Fact]
    public void UnderTheMatplotlibThemes_ASlotThatHoldsTheLabels_ReachesNothingPastTheCell()
    {
        var cell = new Rect(100, 72, 620, 462);
        var below = new ColorBar { Visible = true, Orientation = ColorBarOrientation.Horizontal };

        Assert.Equal(0, ColorBarRoom.Slot.ReachBelow(below, cell, Theme.MatplotlibV2, 0));
    }

    /// <summary>With no plot width known the bar is <see cref="ColorBar.Width"/> thick, and so it is for an aspect of 0 or less, as
    /// the bar below is drawn (<see cref="ColorBarRoomExtensions"/>' body beside the plot).</summary>
    [Fact]
    public void UnderTheLibrarysThemes_ABarOfNoKnownWidthOrNoAspect_IsAsThickAsItsWidth()
    {
        var below = new ColorBar { Visible = true, Orientation = ColorBarOrientation.Horizontal, Width = 30, Padding = 12 };
        double stack = ColorBarRoom.Margin.ReachBelow(below, null, Theme.Default, 0);

        Assert.Equal(12 + 30 + below.TickTopBelow(Theme.Default) + Theme.Default.TickFont().Size + FacingInk.Descent * Theme.Default.TickFont().Size, stack, 6);
        Assert.Equal(stack, ColorBarRoom.Margin.ReachBelow(below with { Aspect = 0 }, new Rect(0, 0, 900, 300), Theme.Default, 0), 6);
        Assert.NotEqual(stack, ColorBarRoom.Margin.ReachBelow(below, new Rect(0, 0, 900, 300), Theme.Default, 0));
    }

    [Fact]
    public void UnderTheLibrarysThemes_ABarBeside_HasNoStackBelowThePlot()
    {
        Assert.Equal(0, ColorBarRoom.Margin.ReachBelow(new ColorBar { Visible = true }, new Rect(0, 0, 400, 300), Theme.Default, 0));
    }

    [Fact]
    public void UnderTheMatplotlibThemes_ARoomySlot_LeavesTheMarginsAsTheyWere()
    {
        Figure With(bool bar) => Plt.Create().WithTheme(Theme.MatplotlibV2).WithSize(500, 700).TightLayout()
            .AddSubPlot(1, 1, 1, ax => { var heat = ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }); if (bar) heat.WithColorBar(b => b with { Orientation = ColorBarOrientation.Horizontal }); })
            .Build();
        var ctx = new SvgRenderContext();

        Assert.Equal(new ConstrainedLayoutEngine().Compute(With(false), ctx).MarginBottom, new ConstrainedLayoutEngine().Compute(With(true), ctx).MarginBottom);
    }

    // ---- under the library's own themes the bar stands below what the axes draws below its plot ------------------------------

    private static System.Xml.Linq.XDocument Svg(Figure figure) => figure.ToSvg().Xml();

    /// <summary>The top of the bar's gradient: the y of the first rectangle of the bar's group.</summary>
    private static double BarTop(System.Xml.Linq.XDocument svg)
    {
        var bar = svg.Each("g").First(e => (string?)e.Attribute("class") == "colorbar");
        var first = bar.Each("rect").First();
        return first.Number("y");
    }

    private static Figure LabelledHeatmap(Theme theme, Action<FigureBuilder> layout, string? xTitle)
    {
        var builder = Plt.Create().WithTheme(theme).WithSize(Width, 400);
        layout(builder);
        return builder.AddSubPlot(1, 1, 1, ax =>
        {
            var heat = ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })
                .WithColorBar(b => b with { Orientation = ColorBarOrientation.Horizontal, Label = "intensity" });
            if (xTitle is not null) heat.SetXLabel(xTitle);
        }).Build();
    }

    /// <summary>The bar stood 10 px under the plot, on the tick labels of the axes and its X axis title: the body is now below the
    /// axes' own bottom band, the lowest of what the axes draws under its plot (here the title), by the gap.</summary>
    [Theory]
    [InlineData("default")]
    [InlineData("tight")]
    [InlineData("constrained")]
    public void UnderTheLibrarysThemes_TheBarStandsBelowTheXAxisTitleOfItsAxes(string layout)
    {
        var figure = LabelledHeatmap(Theme.Default, LayoutOf(layout), "X axis");

        double[] titles = BaselinesOf(figure, "X axis");
        var plot = figure.FirstPlotArea();
        double barTop = BarTop(Svg(figure));

        Assert.Single(titles);
        Assert.True(barTop >= titles[0] + FacingInk.Descent * figure.Theme.LabelFont().Size,
            $"the bar starts at {barTop}, the title's baseline is at {titles[0]}: the bar covers the title");
        Assert.True(barTop > plot.Y + plot.Height + 30, $"the bar starts at {barTop}, the plot ends at {plot.Y + plot.Height}");
    }

    [Theory]
    [InlineData("default")]
    [InlineData("tight")]
    [InlineData("constrained")]
    public void UnderTheLibrarysThemes_TheBarStandsBelowTheTickLabelsOfItsAxes_WhenItHasNoXAxisTitle(string layout)
    {
        var figure = LabelledHeatmap(Theme.Default, LayoutOf(layout), null);

        var plot = figure.FirstPlotArea();
        double barTop = BarTop(Svg(figure));
        // The X tick labels: centred under the plot, the first line of text below it (the Y tick labels end at the plot's left).
        double tickBaseline = figure.ToSvg().Elements("text")
            .Where(element => element["text-anchor"] == "middle" && element.Text.Length > 0 && element.Text.All(c => char.IsAsciiDigit(c) || c == '.'))
            .Select(element => element.Number("y"))
            .Where(y => y > plot.Y + plot.Height && y < plot.Y + plot.Height + 30).Max();

        Assert.True(barTop >= tickBaseline + FacingInk.Descent * figure.Theme.TickFont().Size,
            $"the bar starts at {barTop}, the X tick labels' baseline is at {tickBaseline}: the bar covers them");
    }

    /// <summary>The bar's label stays inside the figure with the title above it, so the margin holds the whole stack.</summary>
    [Theory]
    [InlineData("default")]
    [InlineData("tight")]
    [InlineData("constrained")]
    public void UnderTheLibrarysThemes_TheBottomMarginHoldsTheBarsLabel_BelowTheXAxisTitle(string layout)
    {
        var figure = LabelledHeatmap(Theme.Default, LayoutOf(layout), "X axis");

        double[] baselines = BaselinesOf(figure, "intensity");

        Assert.Single(baselines);
        Assert.True(InkBottom(baselines[0], figure.Theme) <= 400, $"the label's ink reaches {InkBottom(baselines[0], figure.Theme)} in a figure 400 tall");
    }

    // ---- every layout keeps the room, and only where the bar needs more than the author's margin -----------------------------

    /// <summary>A default layout whose margin already holds the bar's whole stack keeps the author's margin: the plot area is the
    /// one the figure has without a bar.</summary>
    [Fact]
    public void UnderTheLibrarysThemes_TheDefaultLayoutKeepsTheAuthorsMargin_WhereTheBarFitsInIt()
    {
        Figure With(bool bar) => Plt.Create().WithTheme(Theme.Default).WithSize(Width, 400)
            .WithSubPlotSpacing(sp => sp with { MarginBottom = 200 })
            .AddSubPlot(1, 1, 1, ax =>
            {
                var heat = ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }).SetXLabel("X axis");
                if (bar) heat.WithColorBar(b => b with { Orientation = ColorBarOrientation.Horizontal, Label = "intensity" });
            }).Build();
        var ctx = new SvgRenderContext();

        Assert.Equal(new ChartRenderer().ComputeLayout(With(false), ctx).PlotAreas[0], new ChartRenderer().ComputeLayout(With(true), ctx).PlotAreas[0]);
    }

    /// <summary>The default layout widens its margin for the stack only as far as the stack needs: the bar's label ends the pad above
    /// the figure's foot when the author's margin is smaller than the stack.</summary>
    [Fact]
    public void UnderTheLibrarysThemes_TheDefaultLayoutWidensItsMarginToTheStack_AndNoFurther()
    {
        var figure = LabelledHeatmap(Theme.Default, _ => { }, "X axis");

        double inkBottom = InkBottom(BaselinesOf(figure, "intensity").Single(), figure.Theme);

        Assert.Equal(400 - ConstrainedLayoutEngine.PadBottom, inkBottom, 6);
    }

    /// <summary>An inset is placed by its parent, and its bar stands under what the inset draws under its plot as a subplot's bar
    /// does: under its X axis title when it has one.</summary>
    [Fact]
    public void UnderTheLibrarysThemes_ABarOfAnInset_StandsBelowWhatTheInsetDrawsUnderItsPlot()
    {
        string Drawn(string? xTitle) => Plt.Create().WithTheme(Theme.Default).WithSize(Width, 400).TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax.AddInset(0.1, 0.1, 0.5, 0.5, inset =>
            {
                var heat = inset.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })
                    .WithColorBar(b => b with { Orientation = ColorBarOrientation.Horizontal });
                if (xTitle is not null) heat.SetXLabel(xTitle);
            })).Build().ToSvg();

        string titled = Drawn("inset title");
        double barTop = BarTop(titled.Xml());
        double titleBaseline = titled.Elements("text").Where(element => element.Text == "inset title").Select(element => element.Number("y")).Single();

        Assert.True(barTop >= titleBaseline + FacingInk.Descent * Theme.Default.LabelFont().Size,
            $"the bar starts at {barTop}, the inset's X axis title's baseline is at {titleBaseline}: the bar covers the title");
        Assert.True(barTop > BarTop(Drawn(null).Xml()));
    }

    /// <summary>A bar beside the axes and its labels stay inside the figure in the default layout: the right margin grows to the
    /// bar's whole stack where the author's margin is smaller.</summary>
    [Fact]
    public void UnderTheLibrarysThemes_TheDefaultLayoutHoldsABarBesideAndItsLabelsInTheFigure()
    {
        var figure = Plt.Create().WithTheme(Theme.Default).WithSize(Width, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }).WithColorBar(b => b with { Label = "intensity" }))
            .Build();
        var ctx = new SvgRenderContext();

        var plot = new ChartRenderer().ComputeLayout(figure, ctx).PlotAreas[0];
        double reach = ColorBarRoom.Margin.ReachBeside(figure.SubPlots[0].ColorBar!, null, figure.SubPlots[0].Series, ctx, figure.Theme, 0);

        Assert.True(reach > 0);
        Assert.True(plot.X + plot.Width + reach + ConstrainedLayoutEngine.PadRight <= Width + 1e-9,
            $"the plot ends at {plot.X + plot.Width} and the bar's stack reaches {reach} past it in a figure {Width} wide");
    }

    // ---- the rule a bar below stands under the axes' own marks by ---------------------------------------------------------

    private static readonly Rect Plot = new(60, 40, 420, 300);

    [Fact]
    public void UnderTheLibrarysThemes_TheBodyOfABarBelow_StandsTheGapBelowWhatTheAxesDrawsUnderItsPlot()
    {
        var below = new ColorBar { Visible = true, Orientation = ColorBarOrientation.Horizontal };

        var body = ColorBarRoom.Margin.BodyOf(below, Plot, null, 40);

        Assert.Equal(new Rect(60, 40 + 300 + 40 + ColorBar.RestingPadding, 420, 420 / 20.0), body);
        Assert.Equal(40 + 300 + ColorBar.RestingPadding, ColorBarRoom.Margin.BodyOf(below, Plot, null, 0).Y);   // nothing drawn under the plot: as before
    }

    [Fact]
    public void UnderTheLibrarysThemes_TheBandIsAddedToTheReachOfTheStackBelowThePlot()
    {
        var below = new ColorBar { Visible = true, Orientation = ColorBarOrientation.Horizontal, Label = "counts" };

        Assert.Equal(ColorBarRoom.Margin.ReachBelow(below, new Rect(0, 0, 420, 300), Theme.Default, 0) + 40, ColorBarRoom.Margin.ReachBelow(below, new Rect(0, 0, 420, 300), Theme.Default, 40), 9);
    }

    [Fact]
    public void TheClearance_IsTheAxesBottomExtent_ForABarBelowUnderTheLibrarysThemes_AndNothingElse()
    {
        var bands = new EdgeBands(0, 0, 0, Bottom: 22, BottomTitle: 44);
        var below = new ColorBar { Visible = true, Orientation = ColorBarOrientation.Horizontal };

        Assert.Equal(44, ColorBarRoom.Margin.ClearanceOf(below, new MeasuredBands(bands, 0)));
        Assert.Equal(0, ColorBarRoom.Slot.ClearanceOf(below, new MeasuredBands(bands, 0)));                              // the bar has its slot
        Assert.Equal(0, ColorBarRoom.Margin.ClearanceOf(below with { Orientation = ColorBarOrientation.Vertical }, new MeasuredBands(bands, 0)));   // beside
        Assert.Equal(0, ColorBarRoom.Margin.ClearanceOf(below, new MeasuredBands(default, 0)));                                 // nothing drawn under the plot
    }

    [Fact]
    public void ABarBelowRenderer_StandsUnderTheBandItIsGiven()
    {
        var ctx = new RecordingRenderContext();
        var below = new ColorBar { Visible = true, Orientation = ColorBarOrientation.Horizontal };

        ColorBarRendererFactory.Create(below, MatPlotLibNet.Styling.ColorMaps.ColorMaps.Viridis, 0, 1, MatPlotLibNet.Styling.ColorMaps.LinearNormalizer.Instance, Plot, ctx,
            Theme.Default, clearance: 40).Render();

        double top = ctx.Of<DrawRectangleCall>().Min(c => c.Rect.Y);
        Assert.Equal(40 + 300 + 40 + ColorBar.RestingPadding, top, 9);
    }
}
