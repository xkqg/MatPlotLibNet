// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Models.Series;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A series says how it is drawn in its legend entry (<c>ChartSeries.LegendSwatch</c>) and the legend draws what it
/// says: the legend holds no list of series types. A series that says nothing is a filled rectangle; the kinds that are
/// lines, markers, error bars or timelines of states say so, with the values they hold.</summary>
public class LegendSwatchCapabilityTests
{
    /// <summary>The swatch of each kind that is not a patch; every other shipped series is a patch.</summary>
    private static readonly Dictionary<Type, LegendSwatchKind> NotPatches = new()
    {
        [typeof(LineSeries)] = LegendSwatchKind.LineMarker,
        [typeof(StepSeries)] = LegendSwatchKind.LineMarker,
        [typeof(SignalSeries)] = LegendSwatchKind.Line,
        [typeof(SignalXYSeries)] = LegendSwatchKind.Line,
        [typeof(EcdfSeries)] = LegendSwatchKind.Line,
        [typeof(RegressionSeries)] = LegendSwatchKind.Line,
        [typeof(SparklineSeries)] = LegendSwatchKind.SolidLine,
        [typeof(ScatterSeries)] = LegendSwatchKind.Scatter,
        [typeof(TernaryScatterSeries)] = LegendSwatchKind.Scatter,
        [typeof(PolarScatterSeries)] = LegendSwatchKind.Scatter,
        [typeof(Scatter3DSeries)] = LegendSwatchKind.Scatter3D,
        [typeof(TernaryLineSeries)] = LegendSwatchKind.Line,
        [typeof(PolarLineSeries)] = LegendSwatchKind.Line,
        [typeof(Line3DSeries)] = LegendSwatchKind.Line,
        [typeof(StreamingLineSeries)] = LegendSwatchKind.LineMarker,
        [typeof(StreamingSignalSeries)] = LegendSwatchKind.LineMarker,
        [typeof(ErrorBarSeries)] = LegendSwatchKind.ErrorBar,
        [typeof(StateTimelineSeries)] = LegendSwatchKind.Stripes,
        [typeof(BarSeries)] = LegendSwatchKind.EdgedPatch,
        [typeof(HistogramSeries)] = LegendSwatchKind.EdgedPatch,
        [typeof(CountSeries)] = LegendSwatchKind.EdgedPatch,
        [typeof(WaterfallSeries)] = LegendSwatchKind.EdgedPatch,
        [typeof(BrokenBarSeries)] = LegendSwatchKind.EdgedPatch,
        [typeof(AreaSeries)] = LegendSwatchKind.EdgedPatch,
        [typeof(StackedAreaSeries)] = LegendSwatchKind.EdgedPatch,
        [typeof(PieSeries)] = LegendSwatchKind.EdgedPatch,
        [typeof(DonutSeries)] = LegendSwatchKind.EdgedPatch,
        [typeof(PolarBarSeries)] = LegendSwatchKind.EdgedPatch,
        [typeof(GanttSeries)] = LegendSwatchKind.EdgedPatch,
        [typeof(ViolinSeries)] = LegendSwatchKind.EdgedPatch,
    };

    [Theory]
    [MemberData(nameof(DrawnSeriesFixtures.TypeNames), MemberType = typeof(DrawnSeriesFixtures))]
    public void EveryShippedSeries_SaysItsSwatch_APatchUnlessItIsInTheTable(string type)
    {
        var series = DrawnSeriesFixtures.Instance(type);
        var expected = NotPatches.GetValueOrDefault(series.GetType(), LegendSwatchKind.Patch);

        Assert.True(((ChartSeries)series).LegendSwatch.Kind == expected, $"{type} says {((ChartSeries)series).LegendSwatch.Kind}, expected {expected}.");
    }

    [Fact]
    public void ALine_SaysItsOwnWidthStyleMarkerAndMarkerSize()
    {
        var line = new LineSeries([1.0], [2.0]) { LineWidth = 3, LineStyle = LineStyle.Dashed, Marker = MarkerStyle.Square, MarkerSize = 9 };

        Assert.Equal(new LegendSwatch(LegendSwatchKind.LineMarker, new GivenStroke(3, LineStyle.Dashed), null, MarkerStyle.Square, 9), line.LegendSwatch);
    }

    [Fact]
    public void ALineGivenNoWidthOrStyle_SaysNone_SoTheSlotDecides()
    {
        var swatch = new LineSeries([1.0], [2.0]).LegendSwatch;

        Assert.Equal(default, swatch.Stroke);
    }

    [Fact]
    public void ALineThatNamesNoMarker_SaysNone_SoTheSlotMayName_One() =>
        Assert.Null(new LineSeries([1.0], [2.0]).LegendSwatch.Marker);

    [Fact]
    public void AStep_SaysTheRestingMarkerSizeOfALine()
    {
        var step = new StepSeries([1.0], [2.0]) { Marker = MarkerStyle.Diamond };

        Assert.Equal(LineSeries.RestingMarkerSize, step.LegendSwatch.MarkerSize);
        Assert.Equal(MarkerStyle.Diamond, step.LegendSwatch.Marker);
    }

    [Fact]
    public void ARegression_SaysItsOwnWidth_WhichIsNotTheThemes()
    {
        var swatch = new RegressionSeries([1.0, 2.0], [1.0, 2.0]) { LineWidth = 4 }.LegendSwatch;

        Assert.Equal(new GivenStroke(4, null), swatch.Stroke);
        Assert.Equal(LineWidthRule.TwoPixelLine, swatch.WidthRule);
    }

    [Fact]
    public void AScatter_SaysItsMarkerAndItsSize_InPointsAcross()
    {
        var swatch = new ScatterSeries([1.0], [2.0]) { Marker = MarkerStyle.Triangle, MarkerSize = 81 }.LegendSwatch;

        Assert.Equal(MarkerStyle.Triangle, swatch.Marker);
        Assert.Equal(9, swatch.MarkerSize);   // 81 points squared is 9 points across
    }

    [Fact]
    public void AnErrorBar_SaysItsLineWidth() =>
        Assert.Equal(2.5, new ErrorBarSeries([1.0], [2.0], [0.1], [0.1]) { LineWidth = 2.5 }.LegendSwatch.Stroke.Width);

    [Fact]
    public void ASparkline_SaysItsLineWidth() =>
        Assert.Equal(2, new SparklineSeries([1.0, 2.0]) { LineWidth = 2 }.LegendSwatch.Stroke.Width);

    [Fact]
    public void ASeriesThatSaysNothing_IsAPatch() =>
        Assert.Equal(LegendSwatch.Patch, new HeatmapSeries(new double[1, 1]).LegendSwatch);

    /// <summary>An area says the edge of a fill drawn as <c>fill_between</c> while it was given no line width, and is a plain patch
    /// once it draws a line of its own.</summary>
    [Fact]
    public void AnArea_SaysAnEdgedPatchUntilItIsGivenALineWidth()
    {
        var area = new AreaSeries([0.0, 1.0], [1.0, 2.0]) { EdgeColor = Colors.Red };

        Assert.Equal(LegendSwatch.EdgedPatch, area.LegendSwatch);
        Assert.Equal(new GivenPatchEdge(Colors.Red, null), ((IEdgedPatch)area).GivenEdge);
        Assert.Equal(LegendSwatch.Patch, new AreaSeries([0.0, 1.0], [1.0, 2.0]) { LineWidth = 2 }.LegendSwatch);
    }

    /// <summary>A bar's handle is an edged patch, and the edge it is drawn with is the ONE value the bar states for its patches
    /// (<c>IEdgedPatch.GivenEdge</c>, which the legend reads from the bar): the handle holds no copy of it.</summary>
    [Fact]
    public void ABar_HasAnEdgedHandle_WhoseEdgeIsTheOneItGivesItsPatches()
    {
        var bar = new BarSeries(["a"], [1.0]) { EdgeColor = Colors.Red, LineWidth = 2 };

        Assert.Equal(LegendSwatch.EdgedPatch, bar.LegendSwatch);
        Assert.Equal(new GivenPatchEdge(Colors.Red, 2, 1.0), ((IEdgedPatch)bar).GivenEdge);
    }

    /// <summary>The handle of an edged patch is the patch as it is drawn: matplotlib 3.11.2 (python) gives a hand-labelled violin body
    /// (<c>parts['bodies'][0].set_label('v')</c>) the handle <c>Rectangle</c> with face (0.75, 0.75, 0, 0.3), edge (0, 0, 0, 0.3), linewidth
    /// 1.0 under the classic style and face alpha 0.3 with edge (0, 0, 0, 0) under the default one; <c>bar([1], [1], alpha=.5, label='b')</c>
    /// gives face alpha 0.5 and edge (0, 0, 0, 0.5) under classic, face alpha 0.5 and no edge under the default style. The library's own
    /// themes keep a plain handle in the series' colour.</summary>
    [Theory]
    [InlineData("violin", "classic", "B0A0014C", "0000004C")]
    [InlineData("violin", "v2", "B0A0014C", "none")]
    [InlineData("violin", "library", "B0A001FF", "none")]
    [InlineData("bar", "classic", "B0A00180", "00000080")]
    [InlineData("bar", "v2", "B0A00180", "none")]
    [InlineData("bar", "library", "B0A001FF", "none")]
    public void TheHandleOfAViolinAndOfATranslucentBar_IsThePatchAsDrawn(string kind, string themeName, string face, string edge)
    {
        var ink = Color.FromHex("#B0A001");
        var theme = themeName.AsTheme();
        static string Hex(Color c) => $"{c.R:X2}{c.G:X2}{c.B:X2}{c.A:X2}";
        var figure = Plt.Create().WithTheme(theme).AddSubPlot(1, 1, 1, ax => (kind == "violin"
            ? ax.Violin([[1.0, 2, 2, 3]], v => { v.Color = ink; v.Label = "v"; })
            : ax.Bar(["a"], [1.0], b => { b.Color = ink; b.Alpha = 0.5; b.Label = "b"; })).WithLegend()).Build();
        var ctx = new RecordingRenderContext();
        new MatPlotLibNet.Rendering.ChartRenderer().Render(figure, ctx);

        var handle = ctx.Of<DrawRectangleCall>().Last(c => c.Fill is Color f && f.R == ink.R && f.G == ink.G && f.B == ink.B);
        Assert.Equal(face, Hex(handle.Fill!.Value));
        Assert.Equal(edge, (handle.Stroke) is { } s ? Hex(s) : "none");
        if (edge != "none") Assert.Equal(1.0.PointsInPixels, handle.StrokeThickness);
    }

    /// <summary>Every kind whose handle is an edged patch is a patch whose edge a theme decides, so the legend has the one edge to
    /// read; the handle itself holds nothing but its kind.</summary>
    [Theory]
    [MemberData(nameof(DrawnSeriesFixtures.TypeNames), MemberType = typeof(DrawnSeriesFixtures))]
    public void EveryEdgedHandle_StandsForAPatchWhoseEdgeAThemeDecides(string type)
    {
        var series = DrawnSeriesFixtures.Instance(type);
        var swatch = ((ChartSeries)series).LegendSwatch;
        if (swatch.Kind != LegendSwatchKind.EdgedPatch) return;

        Assert.True(series is IEdgedPatch, $"{type} has an edged handle but is no IEdgedPatch.");
        Assert.Equal(LegendSwatch.EdgedPatch, swatch);
    }

    /// <summary>Every kind that holds a given stroke and is drawn as a line has a line handle carrying that stroke, as matplotlib
    /// 3.11.2 draws the handle of every <c>Line2D</c> (a polar <c>plot</c>, a 3-D <c>plot</c>'s <c>Line3D</c>, seaborn 0.13.2's
    /// <c>kdeplot(fill=False)</c>) with <c>HandlerLine2D</c> (read with python:
    /// <c>Legend.get_legend_handler(Legend.get_default_handler_map(), line)</c>). The kinds whose stroke outlines a fill have the fill's
    /// handle: an area (<c>fill_between</c>, a <c>PolyCollection</c>) and a filled KDE (<c>kdeplot(fill=True)</c>).</summary>
    [Theory]
    [MemberData(nameof(GivenStrokeHolderTests.KindNames), MemberType = typeof(GivenStrokeHolderTests))]
    public void EveryKindDrawnAsALine_HasALineHandleCarryingItsGivenStroke(string kind)
    {
        var series = GivenStrokeHolderTests.Kinds[kind].Make();
        if (series is AreaSeries or KdeSeries { Fill: true }) return;
        var stroke = new GivenStroke(3, LineStyle.Dashed);
        series.GetType().GetProperty("Stroke", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(series, stroke);

        var swatch = series.LegendSwatch;

        Assert.True(swatch.Kind is LegendSwatchKind.Line or LegendSwatchKind.LineMarker, $"{kind} has a {swatch.Kind} handle.");
        Assert.Equal(stroke, swatch.Stroke);
    }

    /// <summary>An unfilled KDE is a line and has a line's handle; a filled one keeps the fill's.</summary>
    [Fact]
    public void AKde_HasALineHandleUnlessItIsFilled()
    {
        Assert.Equal(new LegendSwatch(LegendSwatchKind.Line, new GivenStroke(2, null), LineWidthRule.Line),
            Assert.Single(new KdeSeries([1.0, 2.0, 2.0]) { Fill = false, LineWidth = 2, Label = "k" }.LegendPartsUnder(Theme.Default)).Swatch);
        Assert.Equal(LegendSwatch.Patch, Assert.Single(new KdeSeries([1.0, 2.0, 2.0]) { Label = "k" }.LegendPartsUnder(Theme.Default)).Swatch);
    }

    /// <summary>A streaming line is drawn as a line with no marker of its own (the slot's cycled one, if any), so its handle is a line's at
    /// the resting marker size.</summary>
    [Fact]
    public void AStreamingLine_HasTheHandleOfTheLineItIsDrawnAs() =>
        Assert.Equal(new LegendSwatch(LegendSwatchKind.LineMarker, MarkerSize: LineSeries.RestingMarkerSize),
            new StreamingLineSeries(8).LegendSwatch);

    /// <summary>A streaming signal is drawn as a line too, a <see cref="LineSeries"/> with the width its slot works out by its kind's rule,
    /// so its handle is that line's: the width it was given, its rule, the resting marker size. It drew a filled patch before, where
    /// matplotlib's handle for a <c>plot</c> line is a <c>Line2D</c>.</summary>
    [Fact]
    public void AStreamingSignal_HasTheHandleOfTheLineItIsDrawnAs()
    {
        Assert.Equal(new LegendSwatch(LegendSwatchKind.LineMarker, new GivenStroke(3, null), LineWidthRule.OnePixelLine, MarkerSize: LineSeries.RestingMarkerSize),
            new StreamingSignalSeries(8) { LineWidth = 3 }.LegendSwatch);
        Assert.Equal(new LegendSwatch(LegendSwatchKind.LineMarker, default, LineWidthRule.OnePixelLine, MarkerSize: LineSeries.RestingMarkerSize),
            new StreamingSignalSeries(8).LegendSwatch);
    }

    /// <summary>A line handle is a function of the stroke its series was given: the stroke, and the rule its kind draws the width by.</summary>
    [Fact]
    public void ALineHandle_IsMadeFromTheGivenStroke()
    {
        var stroke = new GivenStroke(2, LineStyle.Dotted);

        Assert.Equal(new LegendSwatch(LegendSwatchKind.Line, stroke), LegendSwatch.Line(stroke));
        Assert.Equal(new LegendSwatch(LegendSwatchKind.Line, stroke, LineWidthRule.TwoPixelLine), LegendSwatch.Line(new RegressionSeries([1.0, 2, 3], [1.0, 2, 3]) { LineWidth = 2, LineStyle = LineStyle.Dotted }));
        Assert.Equal(new LegendSwatch(LegendSwatchKind.Line, new GivenStroke(1, LineStyle.Dashed), LineWidthRule.Line), new SignalSeries([1.0, 2.0]) { LineWidth = 1, LineStyle = LineStyle.Dashed }.LegendSwatch);
    }
}
