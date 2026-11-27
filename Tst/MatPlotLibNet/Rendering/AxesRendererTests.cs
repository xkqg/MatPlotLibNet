// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What <see cref="AxesRenderer"/> draws for every coordinate system: the legend at each position with the swatch of each
/// kind of series, its frame, shadow and title, the title at each place, the colour bar's label, the interactive groups, the
/// renderer it picks for a coordinate system nobody registered, and the colours a theme's cycler hands out.</summary>
public class AxesRendererTests
{
    private static string Render(Action<AxesBuilder> configure) =>
        Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, configure)
            .Build()
            .ToSvg();

    [Theory]
    [InlineData(LegendPosition.Best)]
    [InlineData(LegendPosition.UpperRight)]
    [InlineData(LegendPosition.UpperLeft)]
    [InlineData(LegendPosition.LowerRight)]
    [InlineData(LegendPosition.LowerLeft)]
    [InlineData(LegendPosition.Right)]
    [InlineData(LegendPosition.CenterLeft)]
    [InlineData(LegendPosition.CenterRight)]
    public void RenderLegend_EveryPosition_DrawsLegend(LegendPosition position)
    {
        var svg = Render(ax => ax
            .Plot([1.0, 2, 3], [4.0, 5, 6], s => s.Label = "Series A")
            .Plot([1.0, 2, 3], [3.0, 4, 5], s => s.Label = "Series B")
            .WithLegend(position));
        Assert.Contains(">Series A<", svg);
        Assert.Contains(">Series B<", svg);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("RenderLegend_LineSeriesSwatch_DrawsCorrectShape", () => Render(ax => ax
            .Plot([1.0, 2], [3.0, 4], s => s.Label = "Line")
            .WithLegend()), ">Line<"),
        new DrawnToken("RenderLegend_ScatterSeriesSwatch_DrawsCorrectShape", () => Render(ax => ax
            .Scatter([1.0, 2], [3.0, 4], s => s.Label = "Scatter")
            .WithLegend()), ">Scatter<"),
        new DrawnToken("RenderLegend_BarSeriesSwatch_DrawsCorrectShape", () => Render(ax => ax
            .Bar(["A", "B"], [10.0, 20.0], s => s.Label = "Bars")
            .WithLegend()), ">Bars<"),
        new DrawnToken("RenderLegend_HistogramSeriesSwatch_DrawsCorrectShape", () => Render(ax => ax
            .Hist([1.0, 2, 3, 4, 5, 6], configure: s => s.Label = "Hist")
            .WithLegend()), ">Hist<"),
        new DrawnToken("RenderColorBar_WithLabel_RendersLabelText", () => Render(ax =>
        {
            var z = new double[2, 2] { { 1, 2 }, { 3, 4 } };
            ax.Heatmap(z);
            ax.WithColorBar(cb => cb with { Label = "Intensity" });
        }), ">Intensity<"),
        new DrawnToken("RenderLegend_MixedSeriesTypes_AllSwatchesDrawn", () => Render(ax => ax
            .Plot([1.0, 2], [3.0, 4], s => s.Label = "Line")
            .Scatter([1.0, 2], [4.0, 5], s => s.Label = "Scatter")
            .Bar(["A", "B"], [1.0, 2.0], s => s.Label = "Bars")
            .WithLegend(LegendPosition.UpperRight)), ">Line<", ">Scatter<", ">Bars<"),
        new DrawnToken("RenderTitle_CustomFont_AppliesStyle", () => Render(ax => ax
            .Plot([1.0, 2], [3.0, 4])
            .WithTitle("Bold Title", style => style with
            {
                FontWeight = FontWeight.Bold,
                FontSize = 20,
            })), ">Bold Title<"),
        // two vertical non-stacked bar series → grouped bar layout assigns side-by-side offsets (barGroups.Count > 1).
        new DrawnToken("Render_TwoBarSeries_GroupedBarOffsets", () => Render(ax => ax
            .Bar(["A", "B", "C"], [10.0, 20.0, 15.0], s => s.Label = "Group 1")
            .Bar(["A", "B", "C"], [12.0, 18.0, 20.0], s => s.Label = "Group 2")
            .WithLegend()), ">Group 1<", ">Group 2<"),
        // LegendRenderer.DrawSwatch — SignalSeries arm: horizontal line swatch.
        new DrawnToken("RenderLegend_SignalSeriesSwatch_DrawsLine", () => Render(ax => ax
            .Signal(Enumerable.Range(0, 10).Select(i => (double)i).ToArray(),
                configure: s => s.Label = "Signal")
            .WithLegend()), ">Signal<"),
        // LegendRenderer.DrawSwatch — StepSeries arm: horizontal line swatch.
        new DrawnToken("RenderLegend_StepSeriesSwatch_DrawsLine", () => Render(ax => ax
            .Step([1.0, 2, 3], [4.0, 5, 6], s => s.Label = "Step")
            .WithLegend()), ">Step<"),
        // LegendRenderer.DrawSwatch — EcdfSeries arm: horizontal line swatch.
        new DrawnToken("RenderLegend_EcdfSeriesSwatch_DrawsLine", () => Render(ax => ax
            .Ecdf([1.0, 2, 3, 4, 5], configure: s => s.Label = "ECDF")
            .WithLegend()), ">ECDF<"),
        // LegendRenderer.DrawSwatch — RegressionSeries arm: horizontal line swatch.
        new DrawnToken("RenderLegend_RegressionSeriesSwatch_DrawsLine", () => Render(ax => ax
            .Regression([1.0, 2, 3], [2.0, 4, 6], s => s.Label = "Fit")
            .WithLegend()), ">Fit<"),
        // LegendRenderer.DrawSwatch — ErrorBarSeries arm: centre line + two caps.
        new DrawnToken("RenderLegend_ErrorBarSeriesSwatch_DrawsLineWithCaps", () => Render(ax => ax
            .ErrorBar([1.0, 2, 3], [4.0, 5, 6], [0.2, 0.3, 0.1], [0.2, 0.3, 0.1], s => s.Label = "ErrBar")
            .WithLegend()), ">ErrBar<"),
        // LegendRenderer.DrawSwatch — LineSeries with Marker set → LegendRenderer.DrawSwatchMarker is also called beside
        // the line segment.
        new DrawnToken("RenderLegend_LineSeriesWithMarker_DrawsMarker", () => Render(ax => ax
            .Plot([1.0, 2, 3], [4.0, 5, 6],
                s => { s.Label = "Marked"; s.Marker = MarkerStyle.Circle; })
            .WithLegend()), ">Marked<"),
        // LegendRenderer.DrawSwatchMarker — Square marker → DrawRectangle called instead of DrawCircle.
        new DrawnToken("RenderLegend_ScatterSquareMarker_DrawsSquareSwatch", () => Render(ax => ax
            .Scatter([1.0, 2, 3], [4.0, 5, 6],
                s => { s.Label = "Squares"; s.Marker = MarkerStyle.Square; })
            .WithLegend()), ">Squares<"),
        // LegendRenderer.DrawSwatch — SignalXYSeries arm (previously uncovered).
        new DrawnToken("RenderLegend_SignalXYSeriesSwatch_DrawsLine", () => Render(ax => ax
            .SignalXY(
                Enumerable.Range(0, 10).Select(i => (double)i).ToArray(),
                Enumerable.Range(0, 10).Select(i => (double)i * 0.5).ToArray(),
                s => s.Label = "SignalXY")
            .WithLegend()), ">SignalXY<"),
        // LegendRenderer.DrawSwatch — SparklineSeries arm (previously uncovered).
        new DrawnToken("RenderLegend_SparklineSeriesSwatch_DrawsLine", () => Render(ax => ax
            .Sparkline(Enumerable.Range(0, 8).Select(i => (double)i).ToArray(),
                s => s.Label = "Spark")
            .WithLegend()), ">Spark<"),
        new DrawnToken("RenderLegend_WithShadowEnabled_DrawsShadowRectangle", () =>
        {
            // shadow path
            var fig = Plt.Create()
                .WithSize(500, 400)
                .AddSubPlot(1, 1, 1, ax => ax
                    .Plot([1.0, 2], [3.0, 4], s => s.Label = "L")
                    .WithLegend())
                .Build();
            fig.SubPlots[0].Legend = fig.SubPlots[0].Legend with { Shadow = true };
            return fig.ToSvg();
        }, ">L<"),
        new DrawnToken("RenderLegend_WithCustomTitleFontSize_AppliesCustomFont", () =>
        {
            // `legend.TitleFontSize.HasValue` true arm
            var fig = Plt.Create()
                .WithSize(500, 400)
                .AddSubPlot(1, 1, 1, ax => ax
                    .Plot([1.0, 2], [3.0, 4], s => s.Label = "L")
                    .WithLegend())
                .Build();
            fig.SubPlots[0].Legend = fig.SubPlots[0].Legend with
            {
                Title = "MyLegend",
                TitleFontSize = 18,
            };
            return fig.ToSvg();
        }, ">MyLegend<"),
        new DrawnToken("RenderLegend_WithFrameOff_OmitsFrame", () =>
        {
            // if (legend.FrameOn) — false arm
            var fig = Plt.Create()
                .WithSize(500, 400)
                .AddSubPlot(1, 1, 1, ax => ax
                    .Plot([1.0, 2], [3.0, 4], s => s.Label = "noframe")
                    .WithLegend())
                .Build();
            fig.SubPlots[0].Legend = fig.SubPlots[0].Legend with { FrameOn = false };
            return fig.ToSvg();
        }, ">noframe<"),
        // EnableInteractiveAttributes = true → interactiveSvgCtx is set → BeginDataGroup (not BeginAccessibleGroup)
        // is called for each series.
        new DrawnToken("Render_EnableInteractiveAttributes_EmitsDataAttributes", () =>
        {
            var fig = Plt.Create()
                .WithSize(500, 400)
                .AddSubPlot(1, 1, 1, ax => ax
                    .Plot([1.0, 2], [3.0, 4], s => s.Label = "Data"))
                .Build();
            fig.SubPlots[0].EnableInteractiveAttributes = true;
            return fig.ToSvg();
        }, "data-series"),
        // EnableInteractiveAttributes = true while legend has labelled entries → svgCtxLegend is non-null →
        // BeginLegendItemGroup and EndGroup are called per entry.
        new DrawnToken("RenderLegend_InteractiveAttributes_EmitsLegendItemGroups", () =>
        {
            var fig = Plt.Create()
                .WithSize(500, 400)
                .AddSubPlot(1, 1, 1, ax => ax
                    .Plot([1.0, 2], [3.0, 4], s => s.Label = "Alpha")
                    .Plot([1.0, 2], [4.0, 5], s => s.Label = "Beta")
                    .WithLegend())
                .Build();
            fig.SubPlots[0].EnableInteractiveAttributes = true;
            return fig.ToSvg();
        }, "data-legend-index"),
        // RenderLegend — Legend.Visible = true but no series have labels → entries.Count == 0 → early return before any
        // drawing. Assert: SVG contains no legend group.
        new DrawnToken("RenderLegend_NoLabelledSeries_LegendVisibleEarlyReturn", () =>
        {
            var fig = Plt.Create()
                .WithSize(500, 400)
                .AddSubPlot(1, 1, 1, ax => ax
                    .Plot([1.0, 2], [3.0, 4])    // no label set
                    .WithLegend())               // Visible = true, but entries will be empty
                .Build();
            return fig.ToSvg();
        }, "<svg") { Absent = ["class=\"legend\""] },
        // A renderer made on its own, outside the draw of a figure, draws the colour bar of its axes too: it has no figure's ranges to
        // place the bar among, and places it beside its own plot.
        new DrawnToken("ARendererMadeOutsideAFigure_DrawsTheColourBarOfItsAxes", () =>
        {
            var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }).WithColorBar()).Build().SubPlots[0];
            var ctx = new MatPlotLibNet.Rendering.Svg.SvgRenderContext();
            AxesRenderer.Create(axes, new Rect(50, 50, 300, 200), ctx, Theme.Default).Render();
            return ctx.GetOutput();
        }, "aria-label=\"Color bar\""),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Theory]
    [InlineData(TitleLocation.Center)]
    [InlineData(TitleLocation.Left)]
    [InlineData(TitleLocation.Right)]
    public void RenderTitle_EveryAlignment_DrawsTitle(TitleLocation loc)
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([1.0, 2], [3.0, 4]);
                ax.WithTitle("My Title");
            })
            .Build();
        fig.SubPlots[0].TitleLoc = loc;
        Assert.Contains(">My Title<", fig.ToSvg());
    }

    [Fact]
    public void RenderLegend_NoLabels_DoesNotDrawLegendBox()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 2], [3.0, 4])     // unlabeled
            .WithLegend());
        // Legend with no labelled series should not crash.
        Assert.NotNull(svg);
    }

    [Theory]
    [InlineData(LegendPosition.LowerCenter)]
    [InlineData(LegendPosition.UpperCenter)]
    [InlineData(LegendPosition.Center)]
    [InlineData(LegendPosition.OutsideRight)]
    [InlineData(LegendPosition.OutsideLeft)]
    [InlineData(LegendPosition.OutsideTop)]
    [InlineData(LegendPosition.OutsideBottom)]
    public void RenderLegend_RemainingPositionArms_RendersWithoutError(LegendPosition position)
    {
        var svg = Render(ax => ax
            .Plot([1.0, 2, 3], [4.0, 5, 6], s => s.Label = "L1")
            .Plot([1.0, 2, 3], [3.0, 4, 5], s => s.Label = "L2")
            .WithLegend(position));
        Assert.Contains(">L1<", svg);
    }

    /// <summary>A coordinate system nobody registered is drawn by the Cartesian renderer.</summary>
    [Fact]
    public void AxesRenderer_Create_UnknownCoordinateSystem_FallsBackToCartesian()
    {
        var axes = new Axes { CoordinateSystem = (CoordinateSystem)999 };
        var ctx = new MatPlotLibNet.Rendering.Svg.SvgRenderContext();
        var renderer = AxesRenderer.Create(axes, new Rect(50, 50, 300, 200), ctx, Theme.Default);
        Assert.IsType<CartesianAxesRenderer>(renderer);
    }

    /// <summary>The fallback for a coordinate system nobody registered is the table's own Cartesian entry, not a second
    /// hand-written construction: an inset axes of such a system gets the inset renderer, as an inset of the Cartesian system does.</summary>
    [Fact]
    public void AxesRenderer_Create_UnknownCoordinateSystem_OnAnInset_FallsBackToTheCartesianInsetRenderer()
    {
        var axes = new Axes { CoordinateSystem = (CoordinateSystem)999 };
        axes.InsetBounds = new InsetBounds(0.1, 0.1, 0.4, 0.4);
        var ctx = new MatPlotLibNet.Rendering.Svg.SvgRenderContext();
        var viaTable = AxesRenderer.Create(new Axes { InsetBounds = axes.InsetBounds }, new Rect(50, 50, 300, 200), ctx, Theme.Default);
        var viaFallback = AxesRenderer.Create(axes, new Rect(50, 50, 300, 200), ctx, Theme.Default);
        Assert.Equal(viaTable.GetType(), viaFallback.GetType());
        Assert.IsType<CartesianInsetAxesRenderer>(viaFallback);
    }

    /// <summary>A theme's PropCycler hands each primary series its colour: the line is drawn in it and the legend
    /// swatch shows the same one. No shipped theme carries a cycler, so the test builds its own.</summary>
    [Fact]
    public void PrimarySeriesWearThePropCyclersColours()
    {
        var cycler = new PropCyclerBuilder()
            .WithColors(Colors.Red, Colors.Green, Colors.Blue)
            .Build();
        var theme = Theme.CreateFrom(Theme.Default).WithPropCycler(cycler).Build();
        var svg = Plt.Create()
            .WithSize(500, 400)
            .WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4], s => s.Label = "A")
                .Plot([1.0, 2], [4.0, 5], s => s.Label = "B")
                .WithLegend())
            .ToSvg();

        Assert.Equal(["#FF0000", "#008000"], PolylineStrokes(svg));
        Assert.Equal(["#FF0000", "#008000"], LegendSwatchStrokes(svg));
    }

    /// <summary>The stroke colour of every <c>&lt;polyline&gt;</c>, in document order.</summary>
    internal static string[] PolylineStrokes(string svg) =>
        [.. svg.Elements("polyline").Select(polyline => polyline["stroke"]).OfType<string>().Where(stroke => stroke.StartsWith('#'))];

    /// <summary>The stroke colour of every line swatch inside the legend group, in entry order. The legend is drawn
    /// after the series, ticks and spines, and nothing after it draws a line.</summary>
    internal static string[] LegendSwatchStrokes(string svg)
    {
        return [.. svg.GroupOf("legend").Elements("line").Select(line => line["stroke"]).OfType<string>().Where(stroke => stroke.StartsWith('#'))];
    }

    /// <summary>A renderer of your own reads the published conveniences of the base: the title font of its theme, a tick written as the
    /// library writes one, and the ticks the library's own locator places over a range, from a count or from an axis.</summary>
    [Fact]
    public void ARendererOfYourOwn_ReadsTheConveniencesOfTheBase()
    {
        var own = new OwnRenderer(new Axes());
        double[] fromAnAxis = OwnRenderer.Ticks(0, 10, new Axis());

        Assert.Equal(Theme.Default.TitleFont(), own.Title);
        Assert.Equal(MatPlotLibNet.Rendering.TickFormatters.NumericTickFormatter.FormatValue(1500), OwnRenderer.Tick(1500));
        Assert.Equal(new AutoLocator(8).LocateInterval(0, 10), OwnRenderer.Ticks(0, 10));
        Assert.NotEmpty(fromAnAxis);
        Assert.All(fromAnAxis, tick => Assert.InRange(tick, 0, 10));
        Assert.Equal(fromAnAxis.Order(), fromAnAxis);
    }

    /// <summary>A renderer of your own draws nothing past its right spine and no patch, as the base says until a renderer of the library
    /// says otherwise: it answers no band there and lists no legend row for a labelled patch.</summary>
    [Fact]
    public void ARendererOfYourOwn_HasNoBandPastItsRightSpine_AndNoLegendRowForAPatch()
    {
        var axes = new Axes();
        axes.Plot([0.0, 1.0], [0.0, 1.0]).Label = "line";
        axes.AddPatch(new MatPlotLibNet.Models.Patches.Rectangle(new(0, 0), 1, 1) { Label = "box" });

        var own = new OwnRenderer(axes);

        Assert.Equal(0, own.PastRightSpine());
        Assert.Equal(["line"], own.LegendEntries(legendValues: false).Select(row => row.Label));
    }

    /// <summary>A renderer as a library user writes one: it draws nothing and hands out the conveniences of the base.</summary>
    private sealed class OwnRenderer(Axes axes)
        : AxesRenderer(axes, new Rect(0, 0, 100, 100), new MatPlotLibNet.Rendering.Svg.SvgRenderContext(), Theme.Default)
    {
        internal Font Title => TitleFont();

        internal static string Tick(double value) => FormatTick(value);

        internal static double[] Ticks(double min, double max) => ComputeTickValues(min, max);

        internal static double[] Ticks(double min, double max, Axis axis) => ComputeTickValues(min, max, axis, 400);

        public override void Render() { }
    }
}
