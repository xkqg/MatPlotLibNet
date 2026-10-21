// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>Mirrored tick labels are reserved by the owner that draws them. <c>WithXTicksMirrored</c> puts the X tick
/// labels above the top spine and <c>WithYTicksMirrored</c> the Y tick labels past the right spine; the Cartesian
/// renderer measures both bands with the tick drawing's own rule (<see cref="AxisTickExtensions.TickLabelBand"/>), the
/// axes title rises above the top band, the end labels start past the right band, and both layout paths reserve them.
/// A drawn secondary axis owns its edge, as matplotlib's <c>twinx</c>/<c>twiny</c> move the host's ticks away; a drawn
/// secondary X axis is then the top band (its tick labels, then its title by
/// <see cref="AxisTickExtensions.TopAxisLabelOffset"/>), measured and reserved by the same rule.</summary>
public class MirroredTickLayoutTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0];
    private static readonly Rect Area = new(80, 60, 400, 300);

    private static RecordingRenderContext Draw(Axes axes)
    {
        var ctx = new RecordingRenderContext();
        new CartesianAxesRenderer(axes, Area, ctx, Theme.Default).Render();
        return ctx;
    }

    private static EdgeBands Measure(Axes axes) =>
        AxesRenderer.Create(axes, new Rect(0, 0, 0, Area.Height), new RecordingRenderContext(), Theme.Default).MeasureEdgeBands();

    private static List<dynamic> Texts(RecordingRenderContext ctx) =>
        ctx.OfKind("DrawText").Select(c => (dynamic)c.Args!).ToList();

    /// <summary>The labels drawn past the right spine, left-aligned (the mirrored Y and secondary Y tick labels).</summary>
    private static List<dynamic> RightLabels(RecordingRenderContext ctx) =>
        Texts(ctx).Where(a => (double)a.position.X > Area.X + Area.Width && (TextAlignment)a.alignment == TextAlignment.Left).ToList();

    /// <summary>The labels drawn above the top spine (the mirrored X and secondary X tick labels).</summary>
    private static List<dynamic> TopLabels(RecordingRenderContext ctx) =>
        Texts(ctx).Where(a => (double)a.position.Y < Area.Y).ToList();

    private static double Widest(RecordingRenderContext ctx, IEnumerable<dynamic> labels) =>
        labels.Max(a => ctx.MeasureText((string)a.text, (Font)a.font).Width);

    // ── the rule ────────────────────────────────────────────────────────────────

    [Fact]
    public void TickLabelBand_IsTheMarkThePadAndTheLabel()
    {
        Assert.Equal(30, new TickConfig { Length = 4, Pad = 6 }.TickLabelBand(20), 9);
    }

    [Fact]
    public void TickLabelBand_IsZero_WhenNoLabelIsDrawn()
    {
        Assert.Equal(0, new TickConfig { Length = 4, Pad = 6 }.TickLabelBand(0));
    }

    // ── the renderer measures what it draws ─────────────────────────────────────

    [Fact]
    public void MirroredYTicks_TheMeasuredRightBandIsTheDrawnOne()
    {
        var axes = new Axes();
        axes.Plot(X, [100_000.0, 500_000.0, 900_000.0]);
        axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Mirror = true, Length = 6, Pad = 5 };

        var measured = Measure(axes);
        var ctx = Draw(axes);

        var labels = RightLabels(ctx);
        Assert.NotEmpty(labels);
        Assert.Equal(6 + 5 + Widest(ctx, labels), measured.Right, 6);
        Assert.Equal(0, measured.Top);
    }

    [Fact]
    public void MirroredYTicks_OnALogAxis_AreMeasuredWithTheLogTicksAndLabels()
    {
        // Measured BEFORE the drawing installs the log locator and formatter: the measurement must find them itself.
        var axes = new Axes();
        axes.Plot(X, [1.0, 1_000.0, 100_000.0]);
        axes.YAxis.Scale = AxisScale.Log;
        axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Mirror = true };

        var measured = Measure(axes);
        var ctx = Draw(axes);

        var ticks = axes.YAxis.MajorTicks;
        Assert.Equal(ticks.Length + ticks.Pad + Widest(ctx, RightLabels(ctx)), measured.Right, 6);
    }

    [Fact]
    public void MirroredYTicks_WithTheAxisOwnLocator_AreMeasuredWithItsTicks()
    {
        // A locator without a formatter: the labels keep the uniform format, before and after the drawing.
        var axes = new Axes();
        axes.Plot(X, [0.0, 1.25, 2.5]);
        axes.YAxis.TickLocator = new MatPlotLibNet.Rendering.TickLocators.MultipleLocator(0.25);
        axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Mirror = true };

        var measured = Measure(axes);
        var ctx = Draw(axes);

        var ticks = axes.YAxis.MajorTicks;
        Assert.Equal(ticks.Length + ticks.Pad + Widest(ctx, RightLabels(ctx)), measured.Right, 6);
    }

    [Fact]
    public void ADateScaleOnTheYAxis_InstallsNoDateLocator()
    {
        // The date locator a scale brings belongs to the X axis only; a date-scaled Y axis keeps the default placement.
        var axes = new Axes();
        axes.Plot(X, [1.0, 2.0, 3.0]);
        axes.YAxis.Scale = AxisScale.Date;

        Draw(axes);

        Assert.Null(axes.YAxis.TickLocator);
        Assert.Null(axes.YAxis.TickFormatter);
    }

    [Fact]
    public void MirroredXTicks_TheMeasuredTopBandIsTheDrawnOne()
    {
        var axes = new Axes();
        axes.Plot(X, [1.0, 2.0, 3.0]);
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Mirror = true, Length = 7, Pad = 2, LabelSize = 20 };

        var measured = Measure(axes);
        var ctx = Draw(axes);

        var labels = TopLabels(ctx);
        Assert.NotEmpty(labels);
        double tallest = labels.Max(a => ctx.MeasureText((string)a.text, (Font)a.font).Height);
        Assert.Equal(7 + 2 + tallest, measured.Top, 6);
        Assert.Equal(0, measured.Right);
    }

    [Fact]
    public void NoMirroredTicks_MeasureNothing()
    {
        var axes = new Axes();
        axes.Plot(X, [1.0, 2.0, 3.0]);

        Assert.Equal(default, Measure(axes));
    }

    [Fact]
    public void HiddenMirroredTicks_MeasureNothing()
    {
        var axes = new Axes();
        axes.Plot(X, [1.0, 2.0, 3.0]);
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Mirror = true, Visible = false };
        axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Mirror = true, Visible = false };

        Assert.Equal(default, Measure(axes));
        Assert.Empty(RightLabels(Draw(axes)));
    }

    [Fact]
    public void AxisLessSeries_DrawNoTicks_AndMeasureNone()
    {
        var axes = new Axes();
        axes.Pie([1.0, 2.0, 3.0]);
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Mirror = true };
        axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Mirror = true };

        Assert.Equal(default, Measure(axes));
    }

    [Fact]
    public void PolarAxes_DrawNoMirroredTicks_AndMeasureNone()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.PolarPlot([1.0, 2.0, 3.0], [0.0, 1.0, 2.0])
            .WithXTicksMirrored().WithYTicksMirrored()).Build();

        Assert.Equal(default, Measure(figure.SubPlots[0]));
    }

    // ── a drawn secondary axis owns its edge ────────────────────────────────────

    [Fact]
    public void ADrawnSecondaryYAxis_OwnsTheRightEdge_TheMirroredLabelsGiveWay()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [0.0, 0.5, 1.0])
            .WithYTicksMirrored()
            .WithSecondaryYAxis(y2 => y2.Plot(X, [1_000.0, 3_000.0, 5_000.0]))).Build();
        var axes = figure.SubPlots[0];

        Assert.Equal(0, Measure(axes).Right);
        var labels = RightLabels(Draw(axes));
        Assert.NotEmpty(labels);
        // Only the secondary axis' labels stand there: no primary label (0 .. 1) overlaps them.
        Assert.All(labels, a => Assert.True(double.Parse((string)a.text, System.Globalization.CultureInfo.InvariantCulture) >= 500,
            $"a primary tick label '{a.text}' is drawn on the secondary axis' edge"));
    }

    [Fact]
    public void ASecondaryYAxisWithoutSeries_DrawsNothing_SoTheMirroredLabelsStay()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [0.0, 0.5, 1.0])
            .WithYTicksMirrored()
            .WithSecondaryYAxis(_ => { })).Build();
        var axes = figure.SubPlots[0];

        Assert.True(Measure(axes).Right > 0);
        Assert.NotEmpty(RightLabels(Draw(axes)));
    }

    [Fact]
    public void ADrawnSecondaryXAxis_OwnsTheTopEdge_TheMirroredLabelsGiveWay()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot([0.0, 0.5, 1.0], X)
            .WithXTicksMirrored()
            .WithSecondaryXAxis(x2 => x2.PlotXSecondary([1_000.0, 3_000.0, 5_000.0], X))).Build();
        var axes = figure.SubPlots[0];

        var ctx = Draw(axes);
        var labels = TopLabels(ctx);
        Assert.NotEmpty(labels);
        Assert.All(labels, a => Assert.True(double.Parse((string)a.text, System.Globalization.CultureInfo.InvariantCulture) >= 500,
            $"a primary tick label '{a.text}' is drawn on the secondary axis' edge"));
        // The top band is the secondary axis' own, not the mirrored primary labels'.
        var ticks = axes.SecondaryXAxis!.MajorTicks;
        Assert.Equal(ticks.Length + ticks.Pad + labels.Max(a => ctx.MeasureText((string)a.text, (Font)a.font).Height),
            Measure(axes).Top, 6);
    }

    // ── a drawn secondary X axis is the top band ────────────────────────────────

    private const string TopTitle = "Top axis";

    private static Axes SecondaryX(string? label = null, string? title = null, Func<TickConfig, TickConfig>? ticks = null,
                                   TextStyle? labelStyle = null)
    {
        var axes = new Axes { Title = title };
        axes.Plot([0.0, 0.5, 1.0], X);
        axes.PlotXSecondary([1_000.0, 3_000.0, 5_000.0], X);
        var axis = axes.SecondaryXAxis!;
        axis.Label = label;
        axis.LabelStyle = labelStyle;
        if (ticks is not null) axis.MajorTicks = ticks(axis.MajorTicks);
        return axes;
    }

    /// <summary>The secondary X tick labels: drawn above the top spine, centred, and not the secondary title.</summary>
    private static List<dynamic> SecondaryTickLabels(RecordingRenderContext ctx) =>
        TopLabels(ctx).Where(a => (string)a.text != TopTitle && (string)a.text != "A Title").ToList();

    private static double Tallest(RecordingRenderContext ctx, IEnumerable<dynamic> labels) =>
        labels.Max(a => ctx.MeasureText((string)a.text, (Font)a.font).Height);

    [Fact]
    public void TopAxisLabelOffset_IsTheBandTheGapAndTheTitleDescent()
    {
        // The title's baseline stands the 6 px gap plus its descent (a fifth of its size) above the tick labels.
        Assert.Equal(30 + 6 + 2, new TickConfig { Length = 4, Pad = 6 }.TopAxisLabelOffset(20, 10), 9);
        Assert.Equal(6 + 3, new TickConfig { Length = 4, Pad = 6 }.TopAxisLabelOffset(0, 15), 9);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MeasureAxisTicks_IsWhatTheDrawingReturns(bool alongX)
    {
        var side = alongX ? TickSide.Top : TickSide.Right;
        double[] ticks = [1.0, 10.0, 1_000.0];
        var config = new TickConfig { LabelSize = 17 };
        var draw = new TickDrawContext
        {
            Side = side, AxisEdge = 10, BaseFont = ThemedFontProvider.TickFont(Theme.Default),
            UniformFormat = AxesRenderer.BuildUniformTickFormatter(ticks), Foreground = Colors.Black,
        };
        var transform = new DataTransform(0, 1_000, 0, 1_000, Area);
        var ctx = new RecordingRenderContext();

        double measured = ctx.MeasureAxisTicks(ticks, config, draw);

        Assert.True(measured > 0);
        Assert.Equal(ctx.RenderAxisTicks(ticks, 0, transform, config, draw), measured, 9);
        Assert.Equal(0, ctx.MeasureAxisTicks(ticks, config with { Visible = false }, draw));
    }

    [Fact]
    public void ADrawnSecondaryXAxis_TheMeasuredTopBandIsItsTickLabels()
    {
        var axes = SecondaryX(ticks: t => t with { Length = 8, Pad = 5, LabelSize = 18 });

        var measured = Measure(axes);
        var ctx = Draw(axes);

        Assert.Equal(8 + 5 + Tallest(ctx, SecondaryTickLabels(ctx)), measured.Top, 6);
    }

    [Theory]
    [InlineData(3.5, 3.5, null, null)]
    [InlineData(8.0, 5.0, 18.0, null)]
    [InlineData(12.0, 9.0, 24.0, 4.0)]
    public void TheSecondaryXTitle_StandsTheGapAndItsDescentAboveTheTickLabels(double length, double pad, double? labelSize,
                                                                              double? stylePad)
    {
        var style = stylePad is { } p ? new TextStyle { Pad = p, FontSize = 16 } : null;
        var axes = SecondaryX(TopTitle, ticks: t => t with { Length = length, Pad = pad, LabelSize = labelSize }, labelStyle: style);

        var ctx = Draw(axes);

        var title = Texts(ctx).Single(a => (string)a.text == TopTitle);
        double size = ((Font)title.font).Size;
        double expected = Area.Y - (length + pad + Tallest(ctx, SecondaryTickLabels(ctx)) + 6 + 0.2 * size) - (stylePad ?? 0);
        Assert.Equal(expected, (double)title.position.Y, 6);
    }

    [Fact]
    public void ADrawnSecondaryXAxisWithATitle_TheMeasuredTopBandReachesTheTopOfThatTitle()
    {
        var axes = SecondaryX(TopTitle, ticks: t => t with { Length = 8, Pad = 5, LabelSize = 18 },
                              labelStyle: new TextStyle { Pad = 3, FontSize = 20 });

        var measured = Measure(axes);
        var ctx = Draw(axes);

        var title = Texts(ctx).Single(a => (string)a.text == TopTitle);
        double titleTop = (double)title.position.Y - ctx.MeasureText(TopTitle, (Font)title.font).Height;
        Assert.Equal(Area.Y - titleTop, measured.Top, 6);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ADrawnSecondaryXAxis_TheAxesTitleRisesByItsBand(bool withAxisTitle)
    {
        static double TitleY(RecordingRenderContext ctx) =>
            (double)Texts(ctx).Single(a => (string)a.text == "A Title").position.Y;
        var plain = new Axes { Title = "A Title" };
        plain.Plot([0.0, 0.5, 1.0], X);
        var axes = SecondaryX(withAxisTitle ? TopTitle : null, "A Title");

        double band = Measure(axes).Top;

        Assert.True(band > 0);
        Assert.Equal(TitleY(Draw(plain)) - band, TitleY(Draw(axes)), 6);
    }

    [Fact]
    public void ASecondaryXAxisWithoutSeries_DrawsNothing_SoItMeasuresNothing_AndTheTitleStays()
    {
        static double TitleY(RecordingRenderContext ctx) =>
            (double)Texts(ctx).Single(a => (string)a.text == "A Title").position.Y;
        var plain = new Axes { Title = "A Title" };
        plain.Plot([0.0, 0.5, 1.0], X);
        var axes = new Axes { Title = "A Title" };
        axes.Plot([0.0, 0.5, 1.0], X);
        axes.TwinY();
        axes.SecondaryXAxis!.Label = TopTitle;

        Assert.Equal(0, Measure(axes).Top);
        var ctx = Draw(axes);
        Assert.DoesNotContain(Texts(ctx), a => (string)a.text == TopTitle);
        Assert.Equal(TitleY(Draw(plain)), TitleY(ctx), 6);
    }

    // ── the title rises above the top band ──────────────────────────────────────

    [Fact]
    public void MirroredXTicks_TheAxesTitleRisesByTheTopBand()
    {
        static Axes Titled(bool mirror)
        {
            var axes = new Axes { Title = "A Title" };
            axes.Plot(X, [1.0, 2.0, 3.0]);
            axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Mirror = mirror };
            return axes;
        }
        static double TitleY(RecordingRenderContext ctx) =>
            (double)Texts(ctx).Single(a => (string)a.text == "A Title").position.Y;

        var mirrored = Titled(true);
        double band = Measure(mirrored).Top;

        Assert.True(band > 0);
        Assert.Equal(TitleY(Draw(Titled(false))) - band, TitleY(Draw(mirrored)), 6);
    }

    // ── the end labels start past the right band ────────────────────────────────

    [Fact]
    public void MirroredYTicks_TheEndLabelsStartPastTheMirroredTickLabels()
    {
        var axes = new Axes();
        var line = axes.Plot(X, [100_000.0, 500_000.0, 900_000.0]);
        line.Label = "Revenue";
        line.LabelPlacement = MatPlotLibNet.Models.Series.SeriesLabelPlacement.LineEnd;
        axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Mirror = true };

        var ctx = Draw(axes);
        var ticks = RightLabels(ctx).Where(a => (string)a.text != "Revenue").ToList();
        var label = Texts(ctx).Single(a => (string)a.text == "Revenue");

        double tickBand = axes.YAxis.MajorTicks.Length + axes.YAxis.MajorTicks.Pad + Widest(ctx, ticks);
        Assert.Equal(Area.X + Area.Width + tickBand + DirectLabelMetrics.Gap, (double)label.position.X, 6);
    }

    // ── both layout paths reserve the bands ─────────────────────────────────────

    private static IReadOnlyList<Rect> PlotAreas(Figure figure) =>
        new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas;

    private static EdgeBands Bands(Figure figure, int axes = 0) =>
        AxesRenderer.Create(figure.SubPlots[axes], new Rect(0, 0, 0, figure.Height), new SvgRenderContext(), figure.Theme)
            .MeasureEdgeBands();

    private static double TitleHeight(Figure figure, string title) =>
        new SvgRenderContext().MeasureText(title, ThemedFontProvider.TitleFont(figure.Theme)).Height;

    [Fact]
    public void TightLayout_MirroredXTicksAndATitle_TheTopMarginHoldsTheBandThenTheTitle()
    {
        var figure = Plt.Create().WithSize(600, 400).TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0]).WithXTicksMirrored().WithTitle("A Title")).Build();

        double expected = Bands(figure).Top + TitleHeight(figure, "A Title") + 8 + 8;
        Assert.Equal(expected, PlotAreas(figure)[0].Y, 6);
    }

    [Fact]
    public void TightLayout_MirroredXTicksWithoutATitle_TheTopMarginHoldsTheBand()
    {
        var figure = Plt.Create().WithSize(600, 400).TightLayout().WithSubPlotSpacing(s => s with { MarginTop = 5 })
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0]).WithXTicksMirrored()).Build();

        Assert.Equal(Bands(figure).Top + 8, PlotAreas(figure)[0].Y, 6);
    }

    [Fact]
    public void DefaultLayout_MirroredXTicksAndATitleInTheTopRow_WidenTheTopMargin()
    {
        var figure = Plt.Create().WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0]).WithXTicksMirrored().WithTitle("A Title")).Build();

        double expected = Bands(figure).Top + TitleHeight(figure, "A Title") + 8 + 8;
        Assert.True(expected > 40, $"precondition: the band and the title ({expected:F2}) exceed the default top margin");
        Assert.Equal(expected, PlotAreas(figure)[0].Y, 6);
    }

    private static Figure Rows(bool mirror) => Plt.Create().WithSize(600, 600)
        .AddSubPlot(2, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0]))
        .AddSubPlot(2, 1, 2, ax =>
        {
            ax.Plot(X, [1.0, 2.0, 3.0]).WithTitle("Lower");
            if (mirror) ax.WithXTicksMirrored();
        })
        .Build();

    private static double RowGap(IReadOnlyList<Rect> a) => a[1].Y - (a[0].Y + a[0].Height);

    [Fact]
    public void DefaultLayout_MirroredXTicksInALowerRow_WidenTheRowGapByTheBand()
    {
        var mirrored = Rows(true);

        Assert.Equal(Bands(mirrored, 1).Top, RowGap(PlotAreas(mirrored)) - RowGap(PlotAreas(Rows(false))), 6);
    }

    private static FigureBuilder WithSecondaryX(FigureBuilder figure) => figure.AddSubPlot(1, 1, 1, ax => ax
        .Plot([0.0, 0.5, 1.0], X).WithTitle("A Title")
        .WithSecondaryXAxis(x2 => x2.PlotXSecondary([1_000.0, 3_000.0, 5_000.0], X).SetXLabel(TopTitle)));

    [Fact]
    public void TightLayout_ASecondaryXAxisAndATitle_TheTopMarginHoldsTheAxisThenTheTitle()
    {
        var figure = WithSecondaryX(Plt.Create().WithSize(600, 400).TightLayout()).Build();

        double band = Bands(figure).Top;
        Assert.True(band > 0);
        Assert.Equal(band + TitleHeight(figure, "A Title") + 8 + 8, PlotAreas(figure)[0].Y, 6);
    }

    [Fact]
    public void DefaultLayout_ASecondaryXAxisAndATitleInTheTopRow_WidenTheTopMargin()
    {
        var figure = WithSecondaryX(Plt.Create().WithSize(600, 400)).Build();

        double expected = Bands(figure).Top + TitleHeight(figure, "A Title") + 8 + 8;
        Assert.True(expected > 40, $"precondition: the axis and the title ({expected:F2}) exceed the default top margin");
        Assert.Equal(expected, PlotAreas(figure)[0].Y, 6);
    }

    private static Figure SecondaryXRows(bool secondary) => Plt.Create().WithSize(600, 600)
        .AddSubPlot(2, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0]))
        .AddSubPlot(2, 1, 2, ax =>
        {
            ax.Plot(X, [1.0, 2.0, 3.0]).WithTitle("Lower");
            if (secondary) ax.WithSecondaryXAxis(x2 => x2.PlotXSecondary([1_000.0, 3_000.0, 5_000.0], X));
        })
        .Build();

    [Fact]
    public void TightLayout_ATallSecondaryXAxisInALowerRow_TheRowGapIsNotCutBelowIt()
    {
        // The measured band, its title and the axes title above it exceed the 120 px gap cap: measured content lifts the
        // cap, as it does on the right edge, or the upper row's X tick labels would sit on the lower row's titles.
        var figure = Plt.Create().WithSize(600, 900).TightLayout()
            .AddSubPlot(2, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0]))
            .AddSubPlot(2, 1, 2, ax => ax.Plot(X, [1.0, 2.0, 3.0]).WithTitle("Lower")
                .WithSecondaryXAxis(x2 => x2.PlotXSecondary([1_000.0, 3_000.0, 5_000.0], X).SetXLabel(TopTitle)))
            .Build();
        var axis = figure.SubPlots[1].SecondaryXAxis!;
        axis.MajorTicks = axis.MajorTicks with { Length = 14, Pad = 11, LabelSize = 28 };
        axis.LabelStyle = new TextStyle { FontSize = 30 };

        double lowerTop = Bands(figure, 1).Top + TitleHeight(figure, "Lower") + 8 + 8;
        Assert.True(lowerTop > 120, $"precondition: the lower row's top need ({lowerTop:F2}) exceeds the gap cap");
        Assert.True(RowGap(PlotAreas(figure)) >= lowerTop, $"row gap {RowGap(PlotAreas(figure)):F2} < {lowerTop:F2}");
    }

    [Fact]
    public void DefaultLayout_ASecondaryXAxisInALowerRow_WidensTheRowGapByItsBand()
    {
        var secondary = SecondaryXRows(true);

        Assert.True(Bands(secondary, 1).Top > 0);
        Assert.Equal(Bands(secondary, 1).Top, RowGap(PlotAreas(secondary)) - RowGap(PlotAreas(SecondaryXRows(false))), 6);
    }

    private static Figure Columns(bool mirror) => Plt.Create().WithSize(800, 400)
        .AddSubPlot(1, 2, 1, ax =>
        {
            ax.Plot(X, [100_000.0, 500_000.0, 900_000.0]);
            if (mirror) ax.WithYTicksMirrored();
        })
        .AddSubPlot(1, 2, 2, ax => ax.Plot(X, [1.0, 2.0, 3.0]))
        .Build();

    private static double Gutter(IReadOnlyList<Rect> a) => a[1].X - (a[0].X + a[0].Width);

    [Fact]
    public void DefaultLayout_MirroredYTicksInAnInteriorColumn_WidenTheGutterByTheBand()
    {
        var mirrored = Columns(true);

        double band = Bands(mirrored).Right + ConstrainedLayoutEngine.PadRight;
        Assert.Equal(band, Gutter(PlotAreas(mirrored)) - Gutter(PlotAreas(Columns(false))), 6);
    }

    [Fact]
    public void RightBand_TheAxisSideIsTheWiderOfTheSecondaryAxisAndTheMirroredTicks()
    {
        Assert.Equal(30, new RightBand(10, 30, 0).Axis);
        Assert.Equal(40, new RightBand(40, 30, 0).Axis);
        Assert.Equal(30 + ConstrainedLayoutEngine.PadRight, new RightBand(10, 30, 0).Width, 9);
        Assert.Equal(40 + 5 + ConstrainedLayoutEngine.PadRight, new RightBand(40, 30, 5).Width, 9);
        Assert.Equal(0, new RightBand(0, 0, 0).Width);
    }
}
