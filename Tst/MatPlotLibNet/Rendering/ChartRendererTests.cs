// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What <see cref="ChartRenderer"/> draws of a figure (its title, its subplots, its colour bar) and the layout it measures
/// without drawing.</summary>
public class ChartRendererTests
{
    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Verifies that a figure with a title includes the title text in the SVG output.
        new DrawnToken("Render_FigureWithTitle_ContainsTitleText", () => Plt.Create()
            .WithTitle("My Title")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "My Title"),
        // Verifies that a figure without a title does not contain stray title text.
        // The figure has no title, so "My Title" should not appear
        new DrawnToken("Render_FigureWithoutTitle_DoesNotContainTitleText", () => Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg()) { Absent = ["My Title"] },
        // Verifies that a single subplot renders axes with rect and line elements.
        // Axes frame is rendered as a rect, and tick marks as line elements
        new DrawnToken("Render_SingleSubplot_ProducesAxes", () => Plt.Create()
            .Plot([1.0, 2.0, 3.0], [10.0, 20.0, 15.0])
            .ToSvg(), "<rect", "<line"),
        // Verifies that two side-by-side subplots both have their titles in the SVG output.
        new DrawnToken("Render_TwoSubplots_ProducesBothTitles", () => Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax
                .WithTitle("Left Panel")
                .Plot([1.0, 2.0], [3.0, 4.0]))
            .AddSubPlot(1, 2, 2, ax => ax
                .WithTitle("Right Panel")
                .Plot([1.0, 2.0], [5.0, 6.0]))
            .ToSvg(), "Left Panel", "Right Panel"),
        // Verifies that a 2x2 grid of subplots renders all four subplot titles.
        new DrawnToken("Render_2x2Grid_AllSubplotsPresent", () => Plt.Create()
            .AddSubPlot(2, 2, 1, ax => ax
                .WithTitle("TopLeft")
                .Plot([1.0, 2.0], [1.0, 2.0]))
            .AddSubPlot(2, 2, 2, ax => ax
                .WithTitle("TopRight")
                .Plot([1.0, 2.0], [3.0, 4.0]))
            .AddSubPlot(2, 2, 3, ax => ax
                .WithTitle("BottomLeft")
                .Plot([1.0, 2.0], [5.0, 6.0]))
            .AddSubPlot(2, 2, 4, ax => ax
                .WithTitle("BottomRight")
                .Plot([1.0, 2.0], [7.0, 8.0]))
            .ToSvg(), "TopLeft", "TopRight", "BottomLeft", "BottomRight"),
        // Verifies that a custom background color appears as a rect with the specified hex color.
        new DrawnToken("Render_WithBackground_ContainsBackgroundRect", () => Plt.Create()
            .WithBackground(Color.FromHex("#AABBCC"))
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "<rect", "#AABBCC"),
        // Verifies that a line series renders as a polyline element.
        new DrawnToken("Render_LineSeries_ContainsPolyline", () => Plt.Create()
            .Plot([1.0, 2.0, 3.0], [10.0, 20.0, 15.0])
            .ToSvg(), "<polyline"),
        // Verifies that a bar series renders as rect elements.
        new DrawnToken("Render_BarSeries_ContainsRects", () => Plt.Create()
            .Bar(["A", "B", "C"], [10.0, 20.0, 15.0])
            .ToSvg(), "<rect"),
        // Verifies that enabling grid lines produces line elements in the SVG output.
        // Grid lines are rendered as <line> elements
        new DrawnToken("Render_WithGrid_ContainsGridLines", () =>
        {
            var figure = new Figure();
            var axes = figure.AddSubPlot();
            axes.Plot([1.0, 2.0, 3.0], [10.0, 20.0, 15.0]);
            axes.Grid = axes.Grid with { Visible = true };
            return figure.ToSvg();
        }, "<line"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>ComputeLayout direct call — exercises the title-aware
    /// plotAreaTop computation + per-subplot legend bounds collection.</summary>
    [Fact]
    public void ComputeLayout_WithTitle_ReturnsPerSubplotLegendBounds()
    {
        var figure = Plt.Create()
            .WithSize(400, 300)
            .WithTitle("My Chart")
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "S")
                .WithLegend())
            .Build();
        var renderer = new ChartRenderer();
        var ctx = new SvgRenderContext();
        var layout = renderer.ComputeLayout(figure, ctx);
        Assert.Equal(figure.SubPlots.Count, layout.PlotAreas.Count);
    }

    /// <summary>ComputeLayout WITHOUT title — plotAreaTop = MarginTop.</summary>
    [Fact]
    public void ComputeLayout_NoTitle_TightTopMargin()
    {
        var figure = Plt.Create()
            .WithSize(400, 300)
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();
        var renderer = new ChartRenderer();
        var ctx = new SvgRenderContext();
        var layout = renderer.ComputeLayout(figure, ctx);
        Assert.Single(layout.PlotAreas);
    }

    /// <summary>ComputeLayout with a math-mode title (ContainsMath true arm).
    /// The richtext branch in title-height measurement runs.</summary>
    [Fact]
    public void ComputeLayout_WithMathTitle_UsesRichTextHeight()
    {
        var figure = Plt.Create()
            .WithSize(400, 300)
            .WithTitle(@"$\alpha + \beta$")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();
        var renderer = new ChartRenderer();
        var ctx = new SvgRenderContext();
        var layout = renderer.ComputeLayout(figure, ctx);
        Assert.Single(layout.PlotAreas);
    }

    /// <summary>The layout pass the interactive heads run on every paint measures and draws nothing: a Best legend finds its
    /// place among the marks without a dry-run drawing.</summary>
    [Fact]
    public void ComputeLayout_WithABestLegend_IssuesNoDrawCalls()
    {
        double[] ramp = [0, 1, 2, 3, 4, 5];
        var figure = Plt.Create().WithSize(500, 350)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(ramp, ramp, s => s.Label = "line")
                .Bar(["a", "b"], [1.0, 2.0], s => s.Label = "bars")
                .Scatter(ramp, ramp, s => s.Label = "dots")
                .Annotate("note", 2, 2)
                .WithLegend())
            .Build();
        var ctx = new RecordingRenderContext();

        new ChartRenderer().ComputeLayout(figure, ctx);

        Assert.DoesNotContain(ctx.Calls, call => call.Kind.StartsWith("Draw", StringComparison.Ordinal));
    }

    // ── What the figure carries of its own: a placed text and a placed image draw at their place, or not at all ──

    private static Figure Decorated(Action<Figure> place)
    {
        var figure = new Figure { Width = 400, Height = 300, Theme = Theme.Default };
        place(figure);
        return figure;
    }

    private static RecordingRenderContext Overlaid(Figure figure)
    {
        var ctx = new RecordingRenderContext();
        var renderer = new ChartRenderer();
        renderer.RenderFigureOverlays(figure, renderer.FrameOf(figure, ctx), ctx);
        return ctx;
    }

    private static RecordingRenderContext Underlaid(Figure figure)
    {
        var ctx = new RecordingRenderContext();
        new ChartRenderer().RenderFigureUnderlays(figure, ctx);
        return ctx;
    }

    [Fact]
    public void AFigureText_IsDrawnAtItsPlaceInTheFontAPlacedTextTakes()
    {
        var call = Overlaid(Decorated(figure => figure.AddText("note", 0.5, 0.5))).Of<DrawTextCall>().Single();

        Assert.Equal("note", call.Text);
        Assert.Equal(200, call.Position.X, 9);
        Assert.Equal(Theme.Default.DataTextFont(null, null), call.Font);
    }

    [Fact]
    public void AFigureTextWithNoPlaceOnTheFigure_DrawsNothing() =>
        Assert.Empty(Overlaid(Decorated(figure => figure.AddText("away", 1.5, 0.5))).Of<DrawTextCall>());

    [Fact]
    public void AFigureImage_IsDrawnOnePixelACell()
    {
        var cells = Underlaid(Decorated(figure => figure.AddImage(new double[,] { { 1, 2 }, { 3, 4 } }, 10, 20))).Rects().ToArray();

        Assert.Equal(4, cells.Length);
        Assert.All(cells, cell => Assert.Equal((1.0, 1.0), (cell.Width, cell.Height)));
    }

    [Fact]
    public void AFigureImageWithNoPlaceOnTheFigure_DrawsNothing() =>
        Assert.Empty(Underlaid(Decorated(figure => figure.AddImage(new double[,] { { 1 } }, double.NaN, 0))).Calls);
}
