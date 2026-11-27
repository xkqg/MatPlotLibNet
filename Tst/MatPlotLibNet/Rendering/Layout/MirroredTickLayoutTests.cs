// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>Mirrored tick labels are reserved by the owner that draws them. <c>WithXTicksMirrored</c> puts the X tick
/// labels above the top spine and <c>WithYTicksMirrored</c> the Y tick labels past the right spine; the Cartesian
/// renderer measures both bands with the tick drawing's own rule (<see cref="AxisTickExtensions.TickLabelBand"/>), the
/// axes title rises above the top band, the end labels start past the right band, and both layout paths reserve them.
/// A drawn secondary axis owns its edge, as matplotlib's <c>twinx</c>/<c>twiny</c> move the host's ticks away; a drawn
/// secondary X axis is then the top band (its tick labels, then its title by
/// <see cref="TitleClearance.TitleOffset"/> beside the top edge), measured and reserved by the same rule.</summary>
public class MirroredTickLayoutTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0];
    private static readonly Rect Area = new(80, 60, 400, 300);

    /// <summary>The bands measured for the plot area the axes is drawn in (<see cref="Draw"/>): whether the X tick labels
    /// turn depends on its width.</summary>
    /// <summary>The right edge of a mirrored Y axis is measured as the length and the pad of its ticks and the widest label the drawing then puts there.</summary>
    private static void AssertTheMirroredLabelsAreMeasured(Axes axes)
    {
        var measured = Measure(axes);
        var ctx = axes.Drawn(Area);

        var ticks = axes.YAxis.MajorTicks;
        Assert.Equal(ticks.Length + ticks.Pad + Widest(ctx, RightLabels(ctx)), measured.Right, 6);
    }

    private static EdgeBands Measure(Axes axes) =>
        AxesRenderer.Create(axes, Area, new RecordingRenderContext(), Theme.Default).MeasureEdgeBands();

    /// <summary>The labels drawn past the right spine, left-aligned (the mirrored Y and secondary Y tick labels).</summary>
    private static List<DrawTextCall> RightLabels(RecordingRenderContext ctx) =>
        ctx.Of<DrawTextCall>().Where(a => (double)a.Position.X > Area.X + Area.Width && a.Alignment == TextAlignment.Left).ToList();

    /// <summary>The labels drawn above the top spine (the mirrored X and secondary X tick labels).</summary>
    private static List<DrawTextCall> TopLabels(RecordingRenderContext ctx) =>
        ctx.Of<DrawTextCall>().Where(a => (double)a.Position.Y < Area.Y).ToList();

    private static double Widest(RecordingRenderContext ctx, IEnumerable<DrawTextCall> labels) =>
        labels.Max(a => ctx.MeasureText(a.Text, a.Font).Width);

    /// <summary>The band the primary Y tick labels take left of the spine as they are drawn: the mark, the pad and the
    /// widest label.</summary>
    private static double DrawnLeftBand(Axes axes)
    {
        var ctx = axes.Drawn(Area);
        var ticks = axes.YAxis.MajorTicks;
        return ticks.Length + ticks.Pad + Widest(ctx, LeftLabels(ctx));
    }

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
        var ctx = axes.Drawn(Area);

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

        AssertTheMirroredLabelsAreMeasured(axes);
    }

    [Fact]
    public void MirroredYTicks_WithTheAxisOwnLocator_AreMeasuredWithItsTicks()
    {
        // A locator without a formatter: the labels keep the uniform format, before and after the drawing.
        var axes = new Axes();
        axes.Plot(X, [0.0, 1.25, 2.5]);
        axes.YAxis.TickLocator = new MatPlotLibNet.Rendering.TickLocators.MultipleLocator(0.25);
        axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Mirror = true };

        AssertTheMirroredLabelsAreMeasured(axes);
    }

    [Fact]
    public void ADateScaleOnTheYAxis_InstallsNoDateLocator()
    {
        // The date locator a scale brings belongs to the X axis only; a date-scaled Y axis keeps the default placement.
        var axes = new Axes();
        axes.Plot(X, [1.0, 2.0, 3.0]);
        axes.YAxis.Scale = AxisScale.Date;

        axes.Drawn(Area);

        Assert.Null(axes.YAxis.TickLocator);
        Assert.Null(axes.YAxis.TickFormatter);
    }

    [Fact]
    public void MirroredXTicks_TheMeasuredTopBandIsTheDrawnOne()
    {
        // Nine labels 20 px high on a 400 px plot crowd each other: they are drawn turned 30 degrees, above the plot as
        // below it, and the band is the turned labels' (it used to be measured upright, 7 + 2 + 20 = 29).
        var axes = new Axes();
        axes.Plot(X, [1.0, 2.0, 3.0]);
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Mirror = true, Length = 7, Pad = 2, LabelSize = 20 };

        var measured = Measure(axes);
        var ctx = new RecordingRenderContext();
        var drawn = new DrawnBands(axes, Area, ctx);
        drawn.Render();

        var labels = TopLabels(ctx);
        Assert.NotEmpty(labels);
        Assert.All(labels, a => Assert.Equal(30.0, a.Rotation));
        Assert.True(drawn.Top > 7 + 2 + 20, $"the turned labels reach past the upright band ({drawn.Top:F2})");
        Assert.Equal(drawn.Top, measured.Top, 6);
        Assert.Equal(0, measured.Right);
    }

    [Fact]
    public void NoMirroredTicks_MeasureNoTopOrRightBand_AndTheDrawnLeftBand()
    {
        // Nothing is drawn past the top or the right spine; the numbers on the left are measured as they are drawn (the
        // left band, the primary Y axis' slot in the container), and so are the X tick labels below the bottom spine.
        var axes = new Axes();
        axes.Plot(X, [1.0, 2.0, 3.0]);

        var bands = Measure(axes);

        Assert.Equal(0, bands.Top);
        Assert.Equal(0, bands.Right);
        Assert.Equal(DrawnLeftBand(axes), bands.Left, 6);
        Assert.Equal(0, bands.BottomTitle);
        Assert.True(bands.Bottom > 0);
    }

    [Fact]
    public void HiddenMirroredTicks_MeasureNothing()
    {
        var axes = new Axes();
        axes.Plot(X, [1.0, 2.0, 3.0]);
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Mirror = true, Visible = false };
        axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Mirror = true, Visible = false };

        Assert.Equal(default, Measure(axes));
        Assert.Empty(RightLabels(axes.Drawn(Area)));
    }

    /// <summary>The series that draw without axes (<c>ChartSeries.DrawsWithoutAxes</c>), one verb each.</summary>
    public static TheoryData<string> AxisLessKinds =>
        ["pie", "donut", "radar", "funnel", "gauge", "progressBar", "statTile", "sparkline"];

    /// <summary>One series of the kind on the axes, the axes' decoration left as it is, or with every axis hidden by the
    /// verb that hides them.</summary>
    private static void AddAxisLess(AxesBuilder ax, string kind, bool hideAllAxes = false)
    {
        switch (kind)
        {
            case "pie": ax.Pie([1.0, 2.0, 3.0]); break;
            case "donut": ax.Donut([1.0, 2.0, 3.0]); break;
            case "radar": ax.Radar(["a", "b", "c"], [1.0, 2.0, 3.0]); break;
            case "funnel": ax.Funnel(["a", "b"], [3.0, 2.0]); break;
            case "gauge": ax.Gauge(0.4); break;
            case "progressBar": ax.ProgressBar(0.4); break;
            case "statTile": ax.StatTile(42); break;
            case "sparkline": ax.Sparkline([1.0, 3.0, 2.0]); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "not a kind of this table");
        }

        if (hideAllAxes) ax.HideAllAxes();
    }

    private static Axes AxisLessAxes(string kind) =>
        Plt.Create().WithSize(400, 300).AddSubPlot(1, 1, 1, ax => AddAxisLess(ax, kind)).Build().SubPlots[0];

    [Theory]
    [MemberData(nameof(AxisLessKinds))]
    public void AxisLessSeries_DrawNoTicks_AndMeasureNone(string kind)
    {
        var axes = AxisLessAxes(kind);
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Mirror = true };
        axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Mirror = true };

        Assert.Equal(default, Measure(axes));
    }

    /// <summary>The picture of an axis-less series is the picture with every axis hidden: no grid, no ticks, no frame, no band
    /// reserved for them, whether or not the author asked.</summary>
    [Theory]
    [MemberData(nameof(AxisLessKinds))]
    public void AxisLessSeries_DrawTheSameAsWithEveryAxisHidden(string kind)
    {
        string Svg(bool hide) =>
            Plt.Create().WithSize(400, 300).AddSubPlot(1, 1, 1, ax => AddAxisLess(ax, kind, hide)).ToSvg();

        Assert.Equal(Svg(hide: true), Svg(hide: false));
    }

    [Fact]
    public void ALineBesideAStatTile_IsNotAxisLess_ItNeedsTheAxes()
    {
        var axes = new Axes();
        axes.Plot(X, [1.0, 2.0, 3.0]);
        axes.StatTile(42);

        Assert.NotEqual(default, Measure(axes));
    }

    [Fact]
    public void AnAxesWithNoSeries_IsNotAxisLess()
    {
        var axes = new Axes();
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Mirror = true };
        axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Mirror = true };

        Assert.NotEqual(default, Measure(axes));
    }

    /// <summary>A polar axes draws no mirrored ticks; what it measures past its square is the rise of its title above it, a
    /// twentieth of the square's side (300 of the 400 by 300 area: 15).</summary>
    [Fact]
    public void PolarAxes_DrawNoMirroredTicks_AndMeasureOnlyTheirTitlesRise()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.PolarPlot([1.0, 2.0, 3.0], [0.0, 1.0, 2.0])
            .WithXTicksMirrored().WithYTicksMirrored()).Build();

        Assert.Equal(new EdgeBands(0.05 * 300, 0, 0, 0), Measure(figure.SubPlots[0]));
    }

    // ── the X axis title below the tick labels ──────────────────────────────────

    /// <summary>The renderer measures the X axis title where <see cref="AxesRenderer"/> draws it: its top at the baseline
    /// less its ascent (four fifths of its size), charged its full height, in its own style and past its style's pad; at
    /// the fixed offset when no tick label is drawn.</summary>
    [Theory]
    [InlineData(false, null, null)]
    [InlineData(false, 22.0, 6.0)]
    [InlineData(true, null, null)]
    [InlineData(true, 18.0, 4.0)]
    public void AnXTitle_TheMeasuredBottomTitleEndsWhereTheDrawnTitleEnds(bool hiddenTicks, double? size, double? pad)
    {
        var axes = new Axes();
        axes.Plot(X, [1.0, 2.0, 3.0]);
        axes.XAxis.Label = "Time";
        if (size is not null) axes.XAxis.LabelStyle = new TextStyle { FontSize = size, Pad = pad };
        if (hiddenTicks) axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Visible = false };

        var measured = Measure(axes);
        var ctx = axes.Drawn(Area);

        var title = ctx.Of<DrawTextCall>().Single(a => a.Text == "Time");
        var font = title.Font;
        double top = (double)title.Position.Y - 0.8 * font.Size - (Area.Y + Area.Height);
        Assert.Equal(top + ctx.MeasureText("Time", font).Height, measured.BottomTitle, 6);
        Assert.Equal(hiddenTicks, measured.Bottom == 0);
    }

    [Fact]
    public void AxisLessSeries_DrawTheirXTitleAtTheFixedOffset_AndMeasureIt()
    {
        var axes = new Axes();
        axes.Pie([1.0, 2.0, 3.0]);
        axes.XAxis.Label = "Share";

        var measured = Measure(axes);
        var ctx = axes.Drawn(Area);

        var title = ctx.Of<DrawTextCall>().Single(a => a.Text == "Share");
        var font = title.Font;
        Assert.Equal(Area.Y + Area.Height + 35, (double)title.Position.Y, 6);
        Assert.Equal(35 - 0.8 * font.Size + ctx.MeasureText("Share", font).Height, measured.BottomTitle, 6);
        Assert.Equal(0, measured.Bottom);
    }

    [Theory]
    [InlineData(CoordinateSystem.Polar)]
    [InlineData(CoordinateSystem.ThreeD)]
    public void PolarAnd3DAxes_DrawTheirXTitleElsewhere_AndMeasureNothingBelow(CoordinateSystem system)
    {
        // A 3-D box draws its X title along its edge, a polar axes not at all: neither draws below the bottom spine.
        var axes = new Axes { CoordinateSystem = system };
        axes.Plot(X, [1.0, 2.0, 3.0]);
        axes.XAxis.Label = "Time";

        var bands = Measure(axes);
        Assert.False(bands.DrawsBelow);
        Assert.Equal(default, bands with { Top = 0 });
    }

    // ── a drawn secondary axis owns its edge ────────────────────────────────────

    [Fact]
    public void ADrawnRightYAxis_OwnsTheRightEdge_TheMirroredLabelsGiveWay()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [0.0, 0.5, 1.0])
            .WithYTicksMirrored()
            .WithAddRightYAxis(y2 => y2.Plot(X, [1_000.0, 3_000.0, 5_000.0]))).Build();
        var axes = figure.SubPlots[0];

        Assert.Equal(0, Measure(axes).Right);
        var labels = RightLabels(axes.Drawn(Area));
        Assert.NotEmpty(labels);
        // Only the secondary axis' labels stand there: no primary label (0 .. 1) overlaps them.
        Assert.All(labels, a => Assert.True(double.Parse(a.Text, System.Globalization.CultureInfo.InvariantCulture) >= 500,
            $"a primary tick label '{a.Text}' is drawn on the secondary axis' edge"));
    }

    [Fact]
    public void ARightYAxisWithoutSeries_DrawsNothing_SoTheMirroredLabelsStay()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [0.0, 0.5, 1.0])
            .WithYTicksMirrored()
            .WithAddRightYAxis(_ => { })).Build();
        var axes = figure.SubPlots[0];

        Assert.True(Measure(axes).Right > 0);
        Assert.NotEmpty(RightLabels(axes.Drawn(Area)));
    }

    [Fact]
    public void ADrawnSecondaryXAxis_OwnsTheTopEdge_TheMirroredLabelsGiveWay()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot([0.0, 0.5, 1.0], X)
            .WithXTicksMirrored()
            .WithSecondaryXAxis(x2 => x2.PlotXSecondary([1_000.0, 3_000.0, 5_000.0], X))).Build();
        var axes = figure.SubPlots[0];

        var ctx = axes.Drawn(Area);
        var labels = TopLabels(ctx);
        Assert.NotEmpty(labels);
        Assert.All(labels, a => Assert.True(double.Parse(a.Text, System.Globalization.CultureInfo.InvariantCulture) >= 500,
            $"a primary tick label '{a.Text}' is drawn on the secondary axis' edge"));
        // The top band is the secondary axis' own, not the mirrored primary labels'.
        var ticks = axes.SecondaryXAxis!.MajorTicks;
        Assert.Equal(ticks.Length + ticks.Pad + labels.Max(a => ctx.MeasureText(a.Text, a.Font).Height),
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
    private static List<DrawTextCall> SecondaryTickLabels(RecordingRenderContext ctx) =>
        TopLabels(ctx).Where(a => a.Text != TopTitle && a.Text != "A Title").ToList();

    private static double Tallest(RecordingRenderContext ctx, IEnumerable<DrawTextCall> labels) =>
        labels.Max(a => ctx.MeasureText(a.Text, a.Font).Height);

    /// <summary>The clearance of a horizontal title beside <paramref name="edge"/> over tick labels <paramref name="tallest"/> high,
    /// with the 6 px gap the library's themes keep.</summary>
    private static TitleClearance XTitle(EdgeTerms edge, TickConfig config, double tallest, double size) =>
        TitleClearance.Beside(edge, config.TickLabelBand(tallest), TitleClearance.XLabelGap, size);

    [Fact]
    public void ATitleAboveATopEdge_StandsItsBaselineTheBandAndTheGapAway()
    {
        // The title's baseline stands the 6 px gap above the tick labels, as matplotlib sets its top X label (va='baseline').
        Assert.Equal(30 + 6, XTitle(EdgeTerms.Top, new TickConfig { Length = 4, Pad = 6 }, 20, 10).TitleOffset(), 9);
        Assert.Equal(6, XTitle(EdgeTerms.Top, new TickConfig { Length = 4, Pad = 6 }, 0, 15).TitleOffset(), 9);
    }

    [Fact]
    public void ATitleBelowABottomEdge_StandsTheBandTheGapAndItsAscentAway_OrTheFixed35WhenNoLabelIsDrawn()
    {
        // The title's baseline stands the 6 px gap plus its ascent (four fifths of its size) below the tick labels.
        Assert.Equal(30 + 6 + 8, XTitle(EdgeTerms.Bottom, new TickConfig { Length = 4, Pad = 6 }, 20, 10).TitleOffset(), 9);
        Assert.Equal(35, XTitle(EdgeTerms.Bottom, new TickConfig { Length = 4, Pad = 6 }, 0, 15).TitleOffset(), 9);
    }

    [Fact]
    public void TheRoomOfATitleBelowABottomEdge_IsTheTitleTopWhereTheOffsetPutsIt_ThenItsPadAndItsHeight()
    {
        var config = new TickConfig { Length = 4, Pad = 6 };

        Assert.Equal(30 + 6 + 3 + 12, XTitle(EdgeTerms.Bottom, config, 20, 10).TitleExtent(12, 3), 9);
        Assert.Equal(35 - 8 + 3 + 12, XTitle(EdgeTerms.Bottom, config, 0, 10).TitleExtent(12, 3), 9);
        // The title's top is the baseline the renderer draws it at (TitleOffset) less its ascent, both arms.
        foreach (double tallest in new[] { 0.0, 20.0 })
            Assert.Equal(XTitle(EdgeTerms.Bottom, config, tallest, 10).TitleOffset() - 0.8 * 10, XTitle(EdgeTerms.Bottom, config, tallest, 10).TitleExtent(12, 0) - 12, 9);
    }

    [Fact]
    public void TheRoomOfATitleBelowABottomEdge_IsWhereTheTitleIsDrawn_WhenAPadPullsTheLabelsInsideTheEdge()
    {
        // A pad of -60 puts the far side of a 20 px label 35 px inside the edge: the band is not positive, so the title is drawn at
        // its fixed place (35 px below the edge) and the room the layout keeps must reach that title, not the band's.
        var clearance = XTitle(EdgeTerms.Bottom, new TickConfig { Length = 4, Pad = -60 }, 20, 10);

        Assert.True(clearance.TickLabelBand <= 0);
        Assert.Equal(35, clearance.TitleOffset(), 9);
        Assert.Equal(clearance.TitleOffset() - 0.8 * 10, clearance.TitleExtent(12, 0) - 12, 9);
    }

    [Fact]
    public void TheRoomOfATitleAboveATopEdge_IsItsOffsetLessItsDescent_ThenItsPadAndItsHeight_WhateverTheBand()
    {
        // A top edge has no fixed place: with or without labels, and with a band pulled inside the edge, the title's baseline stands the
        // band and the gap away, its descent dipping into the gap, and its room reaches its far side.
        foreach (var config in new[] { new TickConfig { Length = 4, Pad = 6 }, new TickConfig { Length = 4, Pad = -60 } })
            foreach (double tallest in new[] { 0.0, 20.0 })
            {
                var clearance = XTitle(EdgeTerms.Top, config, tallest, 10);
                Assert.Equal(clearance.TickLabelBand + 6 - 2 + 3 + 12, clearance.TitleExtent(12, 3), 9);
            }
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
            Side = side, AxisEdge = 10, BaseFont = Theme.Default.TickFont(),
            UniformFormat = ticks.UniformFormat(), Foreground = Colors.Black, Theme = Theme.Default,
        };
        var transform = new DataTransform(0, 1_000, 0, 1_000, Area);
        var ctx = new RecordingRenderContext();

        var run = new TickRun(ticks, transform, draw.Edge);
        double measured = ctx.MeasureAxisTicks(run, config, draw);

        Assert.True(measured > 0);
        Assert.Equal(ctx.RenderAxisTicks(run, config, draw), measured, 9);
        Assert.Equal(0, ctx.MeasureAxisTicks(run, config with { Visible = false }, draw));
    }

    [Fact]
    public void ADrawnSecondaryXAxis_TheMeasuredTopBandIsItsTickLabels()
    {
        var axes = SecondaryX(ticks: t => t with { Length = 8, Pad = 5, LabelSize = 18 });

        var measured = Measure(axes);
        var ctx = axes.Drawn(Area);

        Assert.Equal(8 + 5 + Tallest(ctx, SecondaryTickLabels(ctx)), measured.Top, 6);
    }

    [Theory]
    [InlineData(3.5, 3.5, null, null)]
    [InlineData(8.0, 5.0, 18.0, null)]
    [InlineData(12.0, 9.0, 24.0, 4.0)]
    public void TheSecondaryXTitle_StandsItsBaselineTheGapAboveTheTickLabels(double length, double pad, double? labelSize,
                                                                             double? stylePad)
    {
        var style = stylePad is { } p ? new TextStyle { Pad = p, FontSize = 16 } : null;
        var axes = SecondaryX(TopTitle, ticks: t => t with { Length = length, Pad = pad, LabelSize = labelSize }, labelStyle: style);

        var ctx = axes.Drawn(Area);

        var title = ctx.Of<DrawTextCall>().Single(a => a.Text == TopTitle);
        double expected = Area.Y - (length + pad + Tallest(ctx, SecondaryTickLabels(ctx)) + 6) - (stylePad ?? 0);
        Assert.Equal(expected, (double)title.Position.Y, 6);
    }

    [Fact]
    public void ADrawnSecondaryXAxisWithATitle_TheMeasuredTopBandReachesTheTopOfThatTitle()
    {
        var axes = SecondaryX(TopTitle, ticks: t => t with { Length = 8, Pad = 5, LabelSize = 18 },
                              labelStyle: new TextStyle { Pad = 3, FontSize = 20 });

        var measured = Measure(axes);
        var ctx = axes.Drawn(Area);

        var title = ctx.Of<DrawTextCall>().Single(a => a.Text == TopTitle);
        var font = title.Font;
        // The top of the title's box: its part above the baseline (the descent faces the tick labels below it).
        double titleTop = (double)title.Position.Y - (ctx.MeasureText(TopTitle, font).Height - FacingInk.Descent * font.Size);
        Assert.Equal(Area.Y - titleTop, measured.Top, 6);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ADrawnSecondaryXAxis_TheAxesTitleRisesByItsBand(bool withAxisTitle)
    {
        static double TitleY(RecordingRenderContext ctx) =>
            (double)ctx.Of<DrawTextCall>().Single(a => a.Text == "A Title").Position.Y;
        var plain = new Axes { Title = "A Title" };
        plain.Plot([0.0, 0.5, 1.0], X);
        var axes = SecondaryX(withAxisTitle ? TopTitle : null, "A Title");

        double band = Measure(axes).Top;

        Assert.True(band > 0);
        Assert.Equal(TitleY(plain.Drawn(Area)) - band, TitleY(axes.Drawn(Area)), 6);
    }

    [Fact]
    public void ASecondaryXAxisWithoutSeries_DrawsNothing_SoItMeasuresNothing_AndTheTitleStays()
    {
        static double TitleY(RecordingRenderContext ctx) =>
            (double)ctx.Of<DrawTextCall>().Single(a => a.Text == "A Title").Position.Y;
        var plain = new Axes { Title = "A Title" };
        plain.Plot([0.0, 0.5, 1.0], X);
        var axes = new Axes { Title = "A Title" };
        axes.Plot([0.0, 0.5, 1.0], X);
        axes.TwinY();
        axes.SecondaryXAxis!.Label = TopTitle;

        Assert.Equal(0, Measure(axes).Top);
        var ctx = axes.Drawn(Area);
        Assert.DoesNotContain(ctx.Of<DrawTextCall>(), a => a.Text == TopTitle);
        Assert.Equal(TitleY(plain.Drawn(Area)), TitleY(ctx), 6);
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
            (double)ctx.Of<DrawTextCall>().Single(a => a.Text == "A Title").Position.Y;

        var mirrored = Titled(true);
        double band = Measure(mirrored).Top;

        Assert.True(band > 0);
        Assert.Equal(TitleY(Titled(false).Drawn(Area)) - band, TitleY(mirrored.Drawn(Area)), 6);
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

        var ctx = axes.Drawn(Area);
        var ticks = RightLabels(ctx).Where(a => a.Text != "Revenue").ToList();
        var label = ctx.Of<DrawTextCall>().Single(a => a.Text == "Revenue");

        double tickBand = axes.YAxis.MajorTicks.Length + axes.YAxis.MajorTicks.Pad + Widest(ctx, ticks);
        Assert.Equal(Area.X + Area.Width + tickBand + DirectLabelMetrics.Gap, (double)label.Position.X, 6);
    }

    // ── both layout paths reserve the bands ─────────────────────────────────────

    private static IReadOnlyList<Rect> PlotAreas(Figure figure) =>
        new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas;

    /// <summary>The bands the layout measures for an axes: at the width its plot is drawn (the X tick labels turn by it),
    /// over the height of the figure.</summary>
    private static EdgeBands Bands(Figure figure, int axes = 0)
    {
        var area = PlotAreas(figure)[axes];
        return AxesRenderer.Create(figure.SubPlots[axes], new Rect(area.X, area.Y, area.Width, figure.Height),
            new SvgRenderContext(), figure.Theme).MeasureEdgeBands();
    }

    /// <summary>The pad a tight layout keeps under the default theme (matplotlib v2): <c>tight_layout</c>'s 1.08 of its 10 pt font.</summary>
    private const double TightPad = 1.08 * 10 * 100 / 72;

    /// <summary>The room a title takes above its lift: under the matplotlib themes the part of its box above its baseline (matplotlib's
    /// tight layout keeps room to the top of the title's box; its descent stands in the gap below the baseline), under the library's line
    /// rule its whole line.</summary>
    private static double TitleAscent(Figure figure, string title)
    {
        var box = new SvgRenderContext().ExtentOf(TextLabel.Of(title), figure.Theme.TitleFont());
        return figure.Theme.Looks.Text.MeasuresLikeAgg ? box.Ascent : box.Height;
    }

    [Fact]
    public void TightLayout_MirroredXTicksAndATitle_TheTopMarginHoldsTheBandThenTheTitle()
    {
        var figure = Plt.Create().WithSize(600, 400).TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0]).WithXTicksMirrored().WithTitle("A Title")).Build();

        double expected = Bands(figure).Top + TitleAscent(figure, "A Title") + figure.Theme.TitleGapUnder + TightPad;
        Assert.Equal(expected, PlotAreas(figure)[0].Y, 6);
    }

    [Fact]
    public void TightLayout_MirroredXTicksWithoutATitle_TheTopMarginHoldsTheBand()
    {
        var figure = Plt.Create().WithSize(600, 400).TightLayout().WithSubPlotSpacing(s => s with { MarginTop = 5 })
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0]).WithXTicksMirrored()).Build();

        Assert.Equal(Bands(figure).Top + TightPad, PlotAreas(figure)[0].Y, 6);
    }

    /// <summary>The library's own themes keep the layout's pad above the title of the top row.</summary>
    [Fact]
    public void DefaultLayout_MirroredXTicksAndATitleInTheTopRow_WidenTheTopMargin()
    {
        var figure = Plt.Create().WithSize(600, 400).WithTheme(Theme.Default)
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0]).WithXTicksMirrored().WithTitle("A Title")).Build();

        double expected = Bands(figure).Top + TitleAscent(figure, "A Title") + figure.Theme.TitleGapUnder + 8;
        Assert.True(expected > 40, $"precondition: the band and the title ({expected:F2}) exceed the default top margin");
        Assert.Equal(expected, PlotAreas(figure)[0].Y, 6);
    }

    /// <summary>The matplotlib themes lay the top row out as fixed subplot parameters do: nothing above the title.</summary>
    [Fact]
    public void DefaultLayout_MirroredXTicksAndATitleInTheTopRow_UnderTheMatplotlibThemes_KeepNoPadAboveTheTitle()
    {
        var figure = Plt.Create().WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0]).WithXTicksMirrored().WithTitle("A Title")).Build();

        double expected = Bands(figure).Top + TitleAscent(figure, "A Title") + figure.Theme.TitleGapUnder;
        Assert.True(expected > 0.12 * 400, $"precondition: the band and the title ({expected:F2}) exceed the theme's top margin");
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
        Assert.Equal(band + TitleAscent(figure, "A Title") + figure.Theme.TitleGapUnder + TightPad, PlotAreas(figure)[0].Y, 6);
    }

    [Fact]
    public void DefaultLayout_ASecondaryXAxisAndATitleInTheTopRow_WidenTheTopMargin()
    {
        var figure = WithSecondaryX(Plt.Create().WithSize(600, 400).WithTheme(Theme.Default)).Build();

        double expected = Bands(figure).Top + TitleAscent(figure, "A Title") + figure.Theme.TitleGapUnder + 8;
        Assert.True(expected > 40, $"precondition: the axis and the title ({expected:F2}) exceed the default top margin");
        Assert.Equal(expected, PlotAreas(figure)[0].Y, 6);
    }

    [Fact]
    public void DefaultLayout_ASecondaryXAxisAndATitleInTheTopRow_UnderTheMatplotlibThemes_KeepNoPadAboveTheTitle()
    {
        var figure = WithSecondaryX(Plt.Create().WithSize(600, 400)).Build();

        double expected = Bands(figure).Top + TitleAscent(figure, "A Title") + figure.Theme.TitleGapUnder;
        Assert.True(expected > 0.12 * 400, $"precondition: the axis and the title ({expected:F2}) exceed the theme's top margin");
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

        double lowerTop = Bands(figure, 1).Top + TitleAscent(figure, "Lower") + figure.Theme.TitleGapUnder + 8;
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

    // ── category names on the Y axis: the left band ─────────────────────────────

    private static Axes HorizontalBars(Func<TickConfig, TickConfig>? ticks = null)
    {
        var axes = new Axes();
        axes.Barh(["North", "South-east coastal region", "West"], [1.0, 2.0, 3.0]);
        if (ticks is not null) axes.YAxis.MajorTicks = ticks(axes.YAxis.MajorTicks);
        return axes;
    }

    /// <summary>The names drawn left of the Y spine, right-aligned: the Y tick labels.</summary>
    private static List<DrawTextCall> LeftLabels(RecordingRenderContext ctx) =>
        ctx.Of<DrawTextCall>().Where(a => (double)a.Position.X < Area.X && a.Alignment == TextAlignment.Right).ToList();

    [Theory]
    [InlineData(3.5, 3.5, null)]
    [InlineData(8.0, 5.0, 20.0)]
    public void CategoryNamesOnTheYAxis_TheMeasuredLeftBandIsTheDrawnOne(double length, double pad, double? labelSize)
    {
        var axes = HorizontalBars(t => t with { Length = length, Pad = pad, LabelSize = labelSize });

        var measured = Measure(axes);
        var ctx = axes.Drawn(Area);

        var labels = LeftLabels(ctx);
        Assert.Equal(["North", "South-east coastal region", "West"], labels.Select(a => a.Text));
        Assert.Equal(length + pad + Widest(ctx, labels), measured.Left, 6);
    }

    /// <summary>The numbers of a numeric Y axis are measured as they are drawn, as category names are: the primary Y axis
    /// is left slot 0 of the container, and the layout reserves the band it draws, not an estimate.</summary>
    [Fact]
    public void ANumericYAxis_MeasuresTheLeftBandItDraws()
    {
        var vertical = new Axes();
        vertical.Bar(["North", "South-east coastal region", "West"], [1.0, 2.0, 30_000.0]);

        Assert.Equal(DrawnLeftBand(vertical), Measure(vertical).Left, 6);
        Assert.True(Measure(vertical).Left > vertical.YAxis.MajorTicks.Length + vertical.YAxis.MajorTicks.Pad);
    }

    [Fact]
    public void HiddenCategoryNamesOnTheYAxis_MeasureNoLeftBand()
    {
        var axes = HorizontalBars(t => t with { Visible = false });

        Assert.Equal(0, Measure(axes).Left);
        Assert.Empty(LeftLabels(axes.Drawn(Area)));
    }

    [Fact]
    public void ALocatorOnTheYAxis_TakesThePlaceOfTheCategoryNames()
    {
        // The axis' own locator wins, as it does on X: its ticks carry numbers, measured as they are drawn.
        var axes = HorizontalBars();
        axes.YAxis.TickLocator = new MatPlotLibNet.Rendering.TickLocators.MultipleLocator(1);

        Assert.Equal(DrawnLeftBand(axes), Measure(axes).Left, 6);
        Assert.DoesNotContain(LeftLabels(axes.Drawn(Area)), a => a.Text == "West");
    }

    [Fact]
    public void ALogScaleOnTheYAxis_TakesThePlaceOfTheCategoryNames()
    {
        // A scale that brings its own ticks wins, as it does on X.
        var axes = HorizontalBars();
        axes.YAxis.Scale = AxisScale.Log;

        Assert.Equal(DrawnLeftBand(axes), Measure(axes).Left, 6);
        Assert.DoesNotContain(LeftLabels(axes.Drawn(Area)), a => a.Text == "West");
    }

    [Fact]
    public void AFormatterOnTheYAxis_NamesTheCategoryTicks()
    {
        // The axis' own formatter is kept, the rule a scale's ticks follow: the ticks stand on the categories.
        var axes = HorizontalBars();
        axes.YAxis.TickFormatter = new MatPlotLibNet.Rendering.TickFormatters.CategoryFormatter(["n", "s", "w"]);

        var ctx = axes.Drawn(Area);

        Assert.Equal(["n", "s", "w"], LeftLabels(ctx).Select(a => a.Text));
        Assert.Equal(axes.YAxis.MajorTicks.Length + axes.YAxis.MajorTicks.Pad + Widest(ctx, LeftLabels(ctx)), Measure(axes).Left, 6);
    }

    [Fact]
    public void MirroredCategoryNames_AreMeasuredOnTheRightEdgeToo()
    {
        var axes = HorizontalBars(t => t with { Mirror = true });

        var measured = Measure(axes);
        var ctx = axes.Drawn(Area);

        var right = RightLabels(ctx);
        Assert.Equal(["North", "South-east coastal region", "West"], right.Select(a => a.Text));
        Assert.Equal(measured.Left, measured.Right, 6);
    }

    private static Figure LeftColumn(bool names) => Plt.Create().WithSize(600, 400)
        .AddSubPlot(1, 1, 1, ax =>
        {
            if (names) ax.Bar(["North", "South-east coastal region", "West"], [1.0, 2.0, 3.0], s => s.Orientation = MatPlotLibNet.Models.Series.BarOrientation.Horizontal);
            else ax.Bar(["North", "South-east coastal region", "West"], [1.0, 2.0, 3.0]);
        })
        .Build();

    [Fact]
    public void DefaultLayout_CategoryNamesOnTheYAxis_WidenTheLeftMarginToTheBandAndThePad()
    {
        var named = LeftColumn(true);
        double resolved = PlotAreas(LeftColumn(false))[0].X;   // upright bars: the resolved margin, unwidened

        double band = Bands(named).Left;
        Assert.True(band + ConstrainedLayoutEngine.PadLeft > resolved,
            $"precondition: the band ({band:F2}) and the pad exceed the default left margin ({resolved:F2})");
        Assert.Equal(band + ConstrainedLayoutEngine.PadLeft, PlotAreas(named)[0].X, 6);
        // The numbers of upright bars are measured too, and fit the resolved margin, so it is not widened.
        double numbers = Bands(LeftColumn(false)).Left;
        Assert.True(numbers > 0 && numbers + ConstrainedLayoutEngine.PadLeft < resolved,
            $"the numbers' band ({numbers:F2}) and the pad fit the resolved margin ({resolved:F2})");
    }

    [Fact]
    public void DefaultLayout_ShortCategoryNamesOnTheYAxis_KeepTheResolvedLeftMargin()
    {
        // A band narrower than the resolved margin leaves it as it is: the default path only ever widens.
        var figure = Plt.Create().WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Bar(["a", "b"], [1.0, 2.0], s => s.Orientation = MatPlotLibNet.Models.Series.BarOrientation.Horizontal))
            .Build();

        Assert.True(Bands(figure).Left > 0);
        Assert.Equal(PlotAreas(LeftColumn(false))[0].X, PlotAreas(figure)[0].X, 6);
    }

    private static Figure HiddenAxesTiles(int columns)
    {
        // A mark that fills its figure edge to edge (the package icon) asks for no margin and no gap and hides its axes.
        var figure = Plt.Create().WithSize(128 * columns, 128)
            .WithSubPlotSpacing(s => s with
            {
                MarginLeft = 0, MarginRight = 0, MarginTop = 0, MarginBottom = 0, HorizontalGap = 0, VerticalGap = 0,
            });
        for (int column = 1; column <= columns; column++)
            figure = figure.AddSubPlot(1, columns, column, ax => ax.Plot(X, [1.0, 2.0, 3.0]).HideAllAxes());
        return figure.Build();
    }

    [Fact]
    public void DefaultLayout_NothingDrawnLeftOfTheSpine_KeepsTheMarginTheCallerAsked()
    {
        // The pad that holds the outermost ink off the figure's edge has no ink to hold off when the Y axis draws no tick
        // label and no title: the left margin stays the 0 the caller asked for, as it was before the container.
        Assert.Equal(0, PlotAreas(HiddenAxesTiles(1))[0].X, 6);
    }

    [Fact]
    public void DefaultLayout_NothingDrawnLeftOfAnInteriorSpine_KeepsTheGutterTheCallerAsked()
    {
        var areas = PlotAreas(HiddenAxesTiles(2));

        Assert.Equal(areas[0].X + areas[0].Width, areas[1].X, 6);
    }

    [Fact]
    public void DefaultLayout_ATitleOnAYAxisWithHiddenTicks_StillWidensTheLeftMargin()
    {
        // The skip is for nothing drawn: a title stands at its fixed place without tick labels, and it is reserved.
        var titled = Plt.Create().WithSize(128, 128)
            .WithSubPlotSpacing(s => s with { MarginLeft = 0 })
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0]).SetYLabel("Rate"))
            .Build();
        titled.SubPlots[0].YAxis.MajorTicks = titled.SubPlots[0].YAxis.MajorTicks with { Visible = false };

        Assert.True(PlotAreas(titled)[0].X > ConstrainedLayoutEngine.PadLeft,
            $"a title with hidden ticks left the margin at {PlotAreas(titled)[0].X:F2}");
    }

    // ── category names on the X axis: the bottom band ───────────────────────────────

    private static Axes UprightBars(Func<TickConfig, TickConfig>? ticks = null)
    {
        var axes = new Axes();
        axes.Bar(["North", "South", "West"], [1.0, 2.0, 3.0]);
        if (ticks is not null) axes.XAxis.MajorTicks = ticks(axes.XAxis.MajorTicks);
        return axes;
    }

    /// <summary>The names drawn below the bottom spine, centred under their ticks: the X tick labels (the lowest Y label
    /// also stands a little below the spine, right-aligned left of it).</summary>
    private static List<DrawTextCall> BottomLabels(RecordingRenderContext ctx) =>
        ctx.Of<DrawTextCall>().Where(a => (double)a.Position.Y > Area.Y + Area.Height && a.Alignment == TextAlignment.Center).ToList();

    [Theory]
    [InlineData(3.5, 3.5, null)]
    [InlineData(8.0, 5.0, 24.0)]
    public void CategoryNamesOnTheXAxis_TheMeasuredBottomBandIsTheDrawnOne(double length, double pad, double? labelSize)
    {
        var axes = UprightBars(t => t with { Length = length, Pad = pad, LabelSize = labelSize });

        var measured = Measure(axes);
        var ctx = axes.Drawn(Area);

        var labels = BottomLabels(ctx);
        Assert.Equal(["North", "South", "West"], labels.Select(a => a.Text));
        Assert.Equal(length + pad + Tallest(ctx, labels), measured.Bottom, 6);
        // The numbers on the Y axis are measured as they are drawn; the names below take no part in that band.
        Assert.Equal(DrawnLeftBand(axes), measured.Left, 6);
    }

    /// <summary>The X tick labels of a numeric axis (numbers, dates, the values of bars that lie down) are measured by the
    /// renderer that draws them, as category names are: the mark, the pad and the tallest label drawn.</summary>
    [Theory]
    [InlineData("line", 3.5, 3.5, null)]
    [InlineData("line", 8.0, 5.0, 15.0)]
    [InlineData("lying", 3.5, 3.5, null)]
    [InlineData("dates", 6.0, 2.0, 6.0)]
    [InlineData("log", 3.5, 3.5, null)]
    public void ANumericXAxis_TheMeasuredBottomBandIsTheDrawnOne(string kind, double length, double pad, double? labelSize)
    {
        // Labels drawn upright; turned labels are measured turned (TurnedXTickLabels_TheMeasuredBandsAreTheDrawnOnes).
        var axes = new Axes();
        switch (kind)
        {
            case "lying": axes.Barh(["North", "South", "West"], [1.0, 2.0, 3.0]); break;
            case "dates":
                axes.Plot([new DateTime(2020, 1, 1).ToOADate(), new DateTime(2026, 1, 1).ToOADate()], [1.0, 2.0]);
                axes.XAxis.Scale = AxisScale.Date;
                break;
            case "log": axes.Plot([1.0, 10.0, 100.0], [1.0, 2.0, 3.0]); axes.XAxis.Scale = AxisScale.Log; break;
            default: axes.Plot(X, [1.0, 2.0, 3.0]); break;
        }
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Length = length, Pad = pad, LabelSize = labelSize };

        var measured = Measure(axes);
        var ctx = axes.Drawn(Area);

        var labels = BottomLabels(ctx);
        Assert.NotEmpty(labels);
        Assert.All(labels, a => Assert.Equal(0.0, a.Rotation));
        Assert.Equal(length + pad + Tallest(ctx, labels), measured.Bottom, 6);
    }

    // ── turned X tick labels: measured as they are drawn, at the width they are drawn at ──

    /// <summary>The bands the drawing took below and above the plot: the drawn X tick labels' band
    /// (<see cref="AxisTickExtensions.TickLabelBand"/> of what <see cref="AxisTickExtensions.RenderAxisTicks"/> returned)
    /// and the top band the axes title rose by.</summary>
    private sealed class DrawnBands(Axes axes, Rect area, IRenderContext ctx) : CartesianAxesRenderer(axes, area, ctx, Theme.Default)
    {
        public double Bottom => Axes.XAxis.MajorTicks.TickLabelBand(MeasuredXTickMaxHeight);
        public double Top => MeasuredTopTickBand;
    }

    private static readonly string[] Crowded = [.. Enumerable.Range(0, 24).Select(i => "Category " + i)];

    private static Axes TurnedAxes(string kind)
    {
        var axes = new Axes();
        switch (kind)
        {
            case "names": axes.Bar(Crowded, [.. Crowded.Select((_, i) => (double)i)]); break;
            case "rotation45":
                axes.Plot([0.0, 10_000.0, 20_000.0], [1.0, 2.0, 3.0]);
                axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { LabelRotation = 45 };
                break;
            default:
                axes.Plot([.. Enumerable.Range(0, 40).Select(i => new DateTime(2024, 1, 1).AddDays(i * 9).ToOADate())],
                    [.. Enumerable.Range(0, 40).Select(i => (double)(i % 7))]);
                axes.XAxis.Scale = AxisScale.Date;
                break;
        }
        if (kind == "mirrored") axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Mirror = true };
        return axes;
    }

    [Theory]
    [InlineData("names")]
    [InlineData("dates")]
    [InlineData("rotation45")]
    [InlineData("mirrored")]
    public void TurnedXTickLabels_TheMeasuredBandsAreTheDrawnOnes(string kind)
    {
        var axes = TurnedAxes(kind);

        var measured = Measure(axes);
        var ctx = new RecordingRenderContext();
        var drawn = new DrawnBands(axes, Area, ctx);
        drawn.Render();

        var turned = ctx.Of<DrawTextCall>().Where(a => (double)a.Position.Y > Area.Y + Area.Height && a.Rotation != 0).ToList();
        Assert.NotEmpty(turned);
        double upright = axes.XAxis.MajorTicks.TickLabelBand(Tallest(ctx, turned));
        Assert.True(drawn.Bottom > upright, $"precondition: the turned labels ({drawn.Bottom:F2}) reach past the upright band ({upright:F2})");
        Assert.Equal(drawn.Bottom, measured.Bottom, 9);
        Assert.Equal(drawn.Top, measured.Top, 9);
        if (kind == "mirrored") Assert.True(drawn.Top > upright, $"the mirrored labels are turned too ({drawn.Top:F2})");
    }

    [Fact]
    public void CrowdedXTickLabels_TurnOnlyWhereThePlotIsTooNarrowForThem_AndAreMeasuredSo()
    {
        // The same axes measured for a plot ten times wider: its names stand upright there, and the band is the upright one.
        var axes = TurnedAxes("names");
        var wide = new Rect(Area.X, Area.Y, Area.Width * 10, Area.Height);

        var narrow = Measure(axes);
        var broad = AxesRenderer.Create(axes, wide, new RecordingRenderContext(), Theme.Default).MeasureEdgeBands();
        var ctx = axes.Drawn(wide);

        var labels = ctx.Of<DrawTextCall>().Where(a => Crowded.Contains(a.Text)).ToList();
        Assert.All(labels, a => Assert.Equal(0.0, a.Rotation));
        Assert.Equal(axes.XAxis.MajorTicks.TickLabelBand(Tallest(ctx, labels)), broad.Bottom, 9);
        Assert.True(narrow.Bottom > broad.Bottom, $"the narrow plot turns its names ({narrow.Bottom:F2} against {broad.Bottom:F2})");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HiddenXTickLabels_MeasureNoBottomBand(bool names)
    {
        var axes = names ? UprightBars(t => t with { Visible = false }) : new Axes();
        if (!names)
        {
            axes.Plot(X, [1.0, 2.0, 3.0]);
            axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Visible = false };
        }

        Assert.Equal(0, Measure(axes).Bottom);
        Assert.Empty(BottomLabels(axes.Drawn(Area)));
    }

    [Fact]
    public void HiddenXTickLabels_TheMirroredYTickLabelsAreStillMeasured()
    {
        // With no X tick label to place, the Y ticks are measured over the data range on their own.
        var axes = new Axes();
        axes.Plot(X, [100_000.0, 500_000.0, 900_000.0]);
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Visible = false };
        axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Mirror = true };

        var measured = Measure(axes);
        var ctx = axes.Drawn(Area);

        Assert.Equal(0, measured.Bottom);
        var ticks = axes.YAxis.MajorTicks;
        Assert.Equal(ticks.Length + ticks.Pad + Widest(ctx, RightLabels(ctx)), measured.Right, 6);
    }

    [Fact]
    public void ALocatorOnTheXAxis_TakesThePlaceOfTheCategoryNames_AndItsNumbersAreTheBottomBand()
    {
        var axes = UprightBars(t => t with { LabelSize = 19 });
        axes.XAxis.TickLocator = new MatPlotLibNet.Rendering.TickLocators.MultipleLocator(1);

        var ctx = axes.Drawn(Area);

        Assert.DoesNotContain(BottomLabels(ctx), a => a.Text == "West");
        var ticks = axes.XAxis.MajorTicks;
        Assert.Equal(ticks.Length + ticks.Pad + Tallest(ctx, BottomLabels(ctx)), Measure(axes).Bottom, 6);
    }

    [Fact]
    public void UprightBarsWithMirroredYTicks_MeasureTheBottomAndTheRightBand()
    {
        // The names on X and the numbers mirrored on Y are measured over the one data range the drawing uses.
        var axes = UprightBars(t => t with { LabelSize = 18 });
        axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Mirror = true };

        var measured = Measure(axes);
        var ctx = axes.Drawn(Area);

        var ticks = axes.XAxis.MajorTicks;
        Assert.Equal(ticks.Length + ticks.Pad + Tallest(ctx, BottomLabels(ctx)), measured.Bottom, 6);
        Assert.Equal(axes.YAxis.MajorTicks.Length + axes.YAxis.MajorTicks.Pad + Widest(ctx, RightLabels(ctx)), measured.Right, 6);
    }

    [Fact]
    public void MirroredCategoryNamesOnTheXAxis_AreTheMeasuredTopBand()
    {
        var axes = UprightBars(t => t with { Mirror = true, LabelSize = 20 });

        var measured = Measure(axes);
        var ctx = axes.Drawn(Area);

        var top = TopLabels(ctx);
        Assert.Equal(["North", "South", "West"], top.Select(a => a.Text));
        Assert.Equal(measured.Bottom, measured.Top, 6);
    }

    private static Figure BottomRow(bool names, string? xTitle) => Plt.Create().WithSize(500, 350)
        .AddSubPlot(1, 1, 1, ax =>
        {
            if (names) ax.Bar(["North", "South", "West"], [1.0, 2.0, 3.0]);
            else ax.Plot(X, [1.0, 2.0, 3.0]);
            if (xTitle is not null) ax.SetXLabel(xTitle);
        })
        .Build();

    private static double MarginBottom(Figure figure)
    {
        var area = PlotAreas(figure)[0];
        return figure.Height - (area.Y + area.Height);
    }

    /// <summary>The theme's resolved bottom margin (matplotlib's 0.11 of the height, 38 px at 350) does not grow with the tick labels and the
    /// X axis title; the default path widens it to the need (the band, the 6 px gap, the title's height and the pad), the rule the left edge
    /// follows for names on the Y axis, whether the band is of category names or of numbers.</summary>
    [Theory]
    [InlineData(true, "Region")]
    [InlineData(false, "Time")]
    public void DefaultLayout_ABandAndAnXTitle_WidenTheBottomMarginToTheBandTheTitleAndThePad(bool names, string title)
    {
        var titled = BottomRow(names, title);
        double titleHeight = new SvgRenderContext().MeasureText(title, titled.Theme.LabelFont()).Height;

        double need = Bands(titled).Bottom + titled.Theme.XLabelGapUnder + titleHeight + 5;
        Assert.True(need > MarginBottom(BottomRow(false, null)),
            $"precondition: the band and the title ({need:F2}) exceed the default bottom margin ({MarginBottom(BottomRow(false, null)):F2})");
        Assert.Equal(need, MarginBottom(titled), 6);
    }

    [Fact]
    public void DefaultLayout_CategoryNamesWithoutATitle_KeepTheResolvedBottomMargin()
    {
        // A band narrower than the resolved margin leaves it as it is: the default path only ever widens.
        Assert.True(Bands(BottomRow(true, null)).Bottom > 0);
        Assert.Equal(MarginBottom(BottomRow(false, null)), MarginBottom(BottomRow(true, null)), 6);
    }

    [Fact]
    public void DefaultLayout_ANumericXAxisWhoseTitleFits_KeepsTheResolvedBottomMargin()
    {
        // 0.11 of 700 px is 77 px, more than the labels and the title need: the default path only ever widens.
        Figure Tall(string? title) => Plt.Create().WithSize(500, 700)
            .AddSubPlot(1, 1, 1, ax => { ax.Plot(X, [1.0, 2.0, 3.0]); if (title is not null) ax.SetXLabel(title); }).Build();

        Assert.Equal(77, MarginBottom(Tall(null)), 6);
        Assert.Equal(77, MarginBottom(Tall("Time")), 6);
    }

    /// <summary>With no X tick label drawn the renderer sets the title's baseline at the fixed 35 px
    /// (<see cref="EdgeTerms.TitleWithoutTickLabels"/>, through <see cref="TitleClearance.TitleOffset"/>); the layout reserves the title where it is drawn, its full
    /// height from its top (35 px less its ascent), and the pad.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HiddenXTicksAndAnXTitle_TheBottomMarginHoldsTheTitleAtItsFixedOffset(bool tight)
    {
        // The tight layout only widens the figure's own margin (50 px unless set): a small one shows the need itself.
        var builder = tight
            ? Plt.Create().WithSize(500, 350).TightLayout().WithSubPlotSpacing(s => s with { MarginBottom = 5 })
            : Plt.Create().WithSize(500, 350);
        var figure = builder.AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0]).SetXLabel("Time")).Build();
        var axis = figure.SubPlots[0].XAxis;
        axis.MajorTicks = axis.MajorTicks with { Visible = false };
        var font = figure.Theme.LabelFont();
        double titleHeight = new SvgRenderContext().MeasureText("Time", font).Height;

        Assert.Equal(0, Bands(figure).Bottom);
        Assert.Equal(35 - 0.8 * font.Size + titleHeight + (tight ? TightPad : 5), MarginBottom(figure), 6);
    }

    /// <summary>The tight layout keeps the floor it has always kept below drawn tick labels (15 px for the mark and the
    /// pad, the label's height, the pad), for numbers and category names alike; a title below them is the band, the gap,
    /// the title and the pad when that is more.</summary>
    [Fact]
    public void TightLayout_WithoutAnXTitle_NumbersAndNamesKeepTheSameFloor()
    {
        Figure Tight(bool names) => Plt.Create().WithSize(500, 350).TightLayout().WithSubPlotSpacing(s => s with { MarginBottom = 5 })
            .AddSubPlot(1, 1, 1, ax => { if (names) ax.Bar(["North", "South", "West"], [1.0, 2.0, 3.0]); else ax.Plot(X, [1.0, 2.0, 3.0]); })
            .Build();
        var numbers = Tight(false);
        double floor = 15 + new SvgRenderContext().MeasureText("0", numbers.Theme.TickFont()).Height + TightPad;

        Assert.Equal(floor, MarginBottom(numbers), 6);
        Assert.Equal(floor, MarginBottom(Tight(true)), 6);
    }

    /// <summary>A polar or 3-D axes measures nothing below its bottom spine (it draws its X title elsewhere or not at all);
    /// the tight layout gives it the Cartesian rule on an estimated band: the mark, the pad and a label, the gap and the
    /// title, the pad; with hidden ticks the title at the fixed place a Cartesian axes draws it (35 px to its baseline, its
    /// ascent of four fifths of its size above that), then the title and the pad (at least the 30 px the tight layout
    /// never goes below).</summary>
    [Theory]
    [InlineData(CoordinateSystem.ThreeD, true)]
    [InlineData(CoordinateSystem.ThreeD, false)]
    [InlineData(CoordinateSystem.Polar, true)]
    public void TightLayout_PolarAnd3DAxesWithAnXTitle_KeepRoomForItByTheCartesianRule(CoordinateSystem system, bool ticks)
    {
        var figure = Plt.Create().WithSize(500, 350).TightLayout().WithSubPlotSpacing(s => s with { MarginBottom = 5 })
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0]).SetXLabel("Time")).Build();
        var axes = figure.SubPlots[0];
        axes.CoordinateSystem = system;
        if (!ticks) axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Visible = false };
        var ctx = new SvgRenderContext();
        double label = ctx.MeasureText("0", figure.Theme.TickFont()).Height;
        double title = ctx.MeasureText("Time", figure.Theme.LabelFont()).Height;
        var major = axes.XAxis.MajorTicks;

        double size = figure.Theme.LabelFont().Size;
        double expected = ticks ? major.Length + major.Pad + label + figure.Theme.XLabelGapUnder + title + TightPad : Math.Max(30, 35 - 0.8 * size + title + TightPad);
        Assert.False(Bands(figure).DrawsBelow);
        Assert.Equal(expected, MarginBottom(figure), 6);
    }

    [Fact]
    public void TightLayout_ANumericXAxisAndAStyledTitle_TheBottomMarginHoldsTheTitleInItsStyle()
    {
        var figure = Plt.Create().WithSize(500, 350).TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0]).SetXLabel("Time")).Build();
        var axis = figure.SubPlots[0].XAxis;
        axis.LabelStyle = new TextStyle { FontSize = 24, Pad = 8 };
        axis.MajorTicks = axis.MajorTicks with { LabelSize = 20 };
        double titleHeight = new SvgRenderContext().MeasureText("Time", figure.Theme.LabelFont() with { Size = 24 }).Height;

        Assert.Equal(Bands(figure).Bottom + figure.Theme.XLabelGapUnder + 8 + titleHeight + TightPad, MarginBottom(figure), 6);
    }

    private static Figure NamedRows(bool names) => Plt.Create().WithSize(500, 600)
        .AddSubPlot(2, 1, 1, ax =>
        {
            if (names) ax.Bar(["North", "South", "West"], [1.0, 2.0, 3.0]).SetXLabel("Region");
            else ax.Plot(X, [1.0, 2.0, 3.0]);
        })
        .AddSubPlot(2, 1, 2, ax => ax.Plot(X, [1.0, 2.0, 3.0]).WithTitle("Lower"))
        .Build();

    [Fact]
    public void DefaultLayout_CategoryNamesInAnUpperRow_TheRowGapHoldsThemTheirTitleAndTheLowerTitle()
    {
        var named = NamedRows(true);
        double labelHeight = new SvgRenderContext().MeasureText("Region", named.Theme.LabelFont()).Height;
        double lowerTitle = TitleAscent(named, "Lower") + named.Theme.TitleGapUnder + 8;

        double need = Bands(named, 0).Bottom + named.Theme.XLabelGapUnder + labelHeight + 5 + lowerTitle;
        Assert.True(need > RowGap(PlotAreas(NamedRows(false))), "precondition: the need exceeds the default row gap");
        Assert.Equal(need, RowGap(PlotAreas(named)), 6);
    }

    [Fact]
    public void DefaultLayout_ANumericXAxisInAnUpperRow_TheRowGapHoldsItsTickLabelsAndTheLowerTitle()
    {
        // The default 40 px row gap left the lower title on the upper row's tick labels; the gap holds the upper row's
        // need (the band and the pad) and the room the lower title takes, the rule category names follow.
        var rows = NamedRows(false);

        double need = Bands(rows, 0).Bottom + 5 + TitleAscent(rows, "Lower") + rows.Theme.TitleGapUnder + 8;
        Assert.True(need > 40, $"precondition: the need ({need:F2}) exceeds the default row gap");
        Assert.Equal(need, RowGap(PlotAreas(rows)), 6);
    }

    [Fact]
    public void TightLayout_TallCategoryNamesInAnUpperRow_TheRowGapIsNotCutBelowThem()
    {
        // Names 28 px high on a long mark and pad, a title below them and the lower row's title exceed the 120 px gap cap:
        // measured content lifts the cap, as it does on the other edges, or the names would sit on the lower row's title.
        var figure = Plt.Create().WithSize(500, 900).TightLayout()
            .AddSubPlot(2, 1, 1, ax => ax.Bar(["North", "South", "West"], [1.0, 2.0, 3.0]).SetXLabel("Region"))
            .AddSubPlot(2, 1, 2, ax => ax.Plot(X, [1.0, 2.0, 3.0]).WithTitle("Lower"))
            .Build();
        var axis = figure.SubPlots[0].XAxis;
        axis.MajorTicks = axis.MajorTicks with { Length = 20, Pad = 16, LabelSize = 40 };
        axis.LabelStyle = new TextStyle { FontSize = 28, Pad = 6 };

        double labelHeight = new SvgRenderContext().MeasureText("Region",
            figure.Theme.LabelFont() with { Size = 28 }).Height;
        double upperBottom = Bands(figure, 0).Bottom + figure.Theme.XLabelGapUnder + 6 + labelHeight + 5;
        double lowerTop = TitleAscent(figure, "Lower") + figure.Theme.TitleGapUnder + 8;
        Assert.True(upperBottom + lowerTop > 120, $"precondition: the stacked need ({upperBottom + lowerTop:F2}) exceeds the gap cap");
        Assert.True(RowGap(PlotAreas(figure)) >= upperBottom + lowerTop - 1e-6,
            $"row gap {RowGap(PlotAreas(figure)):F2} < {upperBottom + lowerTop:F2}");
    }

    [Fact]
    public void RightBand_TheAxisSideIsTheWiderOfTheSecondaryAxisAndTheMirroredTicks()
    {
        Assert.Equal(30, new RightBand(10, 30, 0).Axis);
        Assert.Equal(40, new RightBand(40, 30, 0).Axis);
        Assert.Equal(30 + ConstrainedLayoutEngine.PadRight, new RightBand(10, 30, 0).WidthWith(ConstrainedLayoutEngine.PadRight), 9);
        Assert.Equal(40 + 5 + ConstrainedLayoutEngine.PadRight, new RightBand(40, 30, 5).WidthWith(ConstrainedLayoutEngine.PadRight), 9);
        Assert.Equal(0, new RightBand(0, 0, 0).WidthWith(ConstrainedLayoutEngine.PadRight));
        // What is drawn past the spine, without the pad: what a colour bar in the library's margin stands past.
        Assert.Equal(45, new RightBand(40, 30, 5).Past);
        Assert.Equal(0, new RightBand(0, 0, 0).Past);
    }
}
