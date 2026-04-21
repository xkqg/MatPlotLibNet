// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>
/// Phase Z.2 — high-leverage round-trip coverage for <see cref="ChartSerializer"/> +
/// <see cref="SeriesRegistry"/>. Pre-Z.2: 95.9%L / 76.2%B (cyclomatic 478, ~200
/// uncovered branches in per-series-type dispatch + axes-extras paths).
///
/// One <c>[Theory]</c> exercises every major series-type factory in
/// <see cref="SeriesRegistry"/>; the supporting facts cover axes-level extras
/// (spans, ref-lines, annotations, axis-breaks, insets, gridspec, lighting)
/// that the existing <see cref="ChartSerializerTests"/> /
/// <see cref="ChartSerializerCoverageTests"/> do not exercise jointly.
/// </summary>
public class ChartSerializerRoundTripTests
{
    private static readonly ChartSerializer S = new();

    private static Figure RoundTrip(Figure fig)
    {
        var json = S.ToJson(fig);
        return S.FromJson(json);
    }

    // ── Theory: each registered series type round-trips ─────────────────────

    /// <summary>One row per major series type. Each builds a minimal figure with
    /// that series, round-trips, and asserts the series count is preserved
    /// (which proves the type-specific factory in <see cref="SeriesRegistry"/>
    /// fired and produced a non-null series).</summary>
    [Theory]
    [InlineData("line")]
    [InlineData("scatter")]
    [InlineData("bar")]
    [InlineData("histogram")]
    [InlineData("pie")]
    [InlineData("box")]
    [InlineData("violin")]
    [InlineData("hexbin")]
    [InlineData("regression")]
    [InlineData("kde")]
    [InlineData("heatmap")]
    [InlineData("image")]
    [InlineData("histogram2d")]
    [InlineData("stem")]
    [InlineData("fillbetween")]
    [InlineData("step")]
    [InlineData("ecdf")]
    [InlineData("stackplot")]
    [InlineData("errorbar")]
    [InlineData("candlestick")]
    [InlineData("waterfall")]
    [InlineData("funnel")]
    [InlineData("gauge")]
    [InlineData("sparkline")]
    [InlineData("rugplot")]
    [InlineData("eventplot")]
    [InlineData("countplot")]
    [InlineData("polarline")]
    [InlineData("polarscatter")]
    [InlineData("polarbar")]
    [InlineData("scatter3d")]
    [InlineData("plot3d")]
    [InlineData("stem3d")]
    public void EachSeriesType_RoundTrips_PreservesSeriesCount(string seriesKind)
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => AddSeriesByKind(ax, seriesKind))
            .Build();
        Assert.Single(fig.SubPlots[0].Series);
        var rt = RoundTrip(fig);
        Assert.Single(rt.SubPlots[0].Series);
    }

    private static void AddSeriesByKind(AxesBuilder ax, string kind)
    {
        switch (kind)
        {
            case "line":         ax.Plot([1.0, 2, 3], [4.0, 5, 6]); break;
            case "scatter":      ax.Scatter([1.0, 2, 3], [4.0, 5, 6]); break;
            case "bar":          ax.Bar(["A", "B"], [1.0, 2.0]); break;
            case "histogram":    ax.Hist([1.0, 2, 3, 4, 5]); break;
            case "pie":          ax.Pie([30.0, 70.0]); break;
            case "box":          ax.BoxPlot([[1.0, 2, 3]]); break;
            case "violin":       ax.Violin([[1.0, 2, 3]]); break;
            case "hexbin":       ax.Hexbin([1.0, 2, 3], [1.0, 2, 3]); break;
            case "regression":   ax.Regression([1.0, 2, 3], [1.0, 2, 3]); break;
            case "kde":          ax.Kde([1.0, 2, 3, 4, 5]); break;
            case "heatmap":      ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }); break;
            case "image":        ax.Image(new double[,] { { 1, 2 }, { 3, 4 } }); break;
            case "histogram2d":  ax.Histogram2D([1.0, 2, 3], [1.0, 2, 3]); break;
            case "stem":         ax.Stem([1.0, 2], [3.0, 4]); break;
            case "fillbetween":  ax.FillBetween([1.0, 2], [3.0, 4]); break;
            case "step":         ax.Step([1.0, 2], [3.0, 4]); break;
            case "ecdf":         ax.Ecdf([1.0, 2, 3]); break;
            case "stackplot":    ax.StackPlot([1.0, 2], new double[][] { [1.0, 2], [3.0, 4] }); break;
            case "errorbar":     ax.ErrorBar([1.0, 2], [3.0, 4], [0.1, 0.2], [0.1, 0.2]); break;
            case "candlestick":  ax.Candlestick([10.0, 11], [12.0, 13], [9.0, 10], [11.0, 12]); break;
            case "waterfall":    ax.Waterfall(["A", "B"], [10.0, -5.0]); break;
            case "funnel":       ax.Funnel(["A", "B"], [100.0, 50.0]); break;
            case "gauge":        ax.Gauge(0.7); break;
            case "sparkline":    ax.Sparkline([1.0, 2, 3, 4, 5]); break;
            case "rugplot":      ax.Rugplot([1.0, 2, 3]); break;
            case "eventplot":    ax.Eventplot(new double[][] { [1.0, 2, 3] }); break;
            case "countplot":    ax.Countplot(["A", "A", "B"]); break;
            case "polarline":    ax.PolarPlot([1.0, 2], [0.0, 1.5]); break;
            case "polarscatter": ax.PolarScatter([1.0, 2], [0.0, 1.5]); break;
            case "polarbar":     ax.PolarBar([1.0, 2], [0.0, 1.5]); break;
            case "scatter3d":    ax.Scatter3D([1.0, 2], [3.0, 4], [5.0, 6]); break;
            case "plot3d":       ax.Plot3D([1.0, 2], [3.0, 4], [5.0, 6]); break;
            case "stem3d":       ax.Stem3D([1.0, 2], [3.0, 4], [5.0, 6]); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "unknown");
        }
    }

    // ── Axes-level extras: spans / refs / annotations / breaks / insets / lighting

    /// <summary>Round-trips horizontal+vertical SpanRegions with custom alpha/linestyle
    /// (lines 287-296 of ChartSerializer.cs).</summary>
    [Fact]
    public void RoundTrip_WithSpansHorizontalAndVertical_PreservesBoth()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 5], [1.0, 5])
                .AxHSpan(2, 3, sp => { sp.Alpha = 0.4; sp.LineStyle = LineStyle.Dashed; sp.LineWidth = 2.0; sp.Label = "h"; })
                .AxVSpan(1.5, 2.5, sp => { sp.Alpha = 0.5; sp.Label = "v"; }))
            .Build();
        var rt = RoundTrip(fig);
        Assert.Equal(2, rt.SubPlots[0].Spans.Count);
        Assert.Contains(rt.SubPlots[0].Spans, s => s.Orientation == Orientation.Horizontal && s.Label == "h");
        Assert.Contains(rt.SubPlots[0].Spans, s => s.Orientation == Orientation.Vertical && s.Label == "v");
    }

    /// <summary>Round-trips ReferenceLines (H+V) with custom line style + label
    /// (lines 260-268 of ChartSerializer.cs).</summary>
    [Fact]
    public void RoundTrip_WithReferenceLinesBothOrientations_Preserves()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 5], [1.0, 5])
                .AxHLine(3.0, r => { r.LineStyle = LineStyle.Dotted; r.LineWidth = 1.5; r.Label = "h-ref"; })
                .AxVLine(2.0, r => { r.Label = "v-ref"; }))
            .Build();
        var rt = RoundTrip(fig);
        Assert.Equal(2, rt.SubPlots[0].ReferenceLines.Count);
        Assert.Contains(rt.SubPlots[0].ReferenceLines, r => r.Orientation == Orientation.Horizontal && r.Label == "h-ref");
        Assert.Contains(rt.SubPlots[0].ReferenceLines, r => r.Orientation == Orientation.Vertical && r.Label == "v-ref");
    }

    /// <summary>Round-trips an Annotation with arrow target + box style (lines 245-258).</summary>
    [Fact]
    public void RoundTrip_WithAnnotationAndArrow_PreservesAllFields()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 5], [1.0, 5])
                .Annotate("hi", 2, 3, 4, 5, a =>
                {
                    a.ConnectionStyle = ConnectionStyle.Arc3;
                    a.ConnectionRad = 0.5;
                    a.ArrowHeadSize = 12;
                    a.BoxStyle = BoxStyle.Round;
                    a.BoxPadding = 6;
                    a.BoxCornerRadius = 8;
                }))
            .Build();
        var rt = RoundTrip(fig);
        Assert.Single(rt.SubPlots[0].Annotations);
        var ann = rt.SubPlots[0].Annotations[0];
        Assert.Equal("hi", ann.Text);
        Assert.Equal(ConnectionStyle.Arc3, ann.ConnectionStyle);
        Assert.Equal(0.5, ann.ConnectionRad);
        Assert.Equal(12, ann.ArrowHeadSize);
        Assert.Equal(BoxStyle.Round, ann.BoxStyle);
    }

    /// <summary>Round-trips XBreaks + YBreaks with explicit BreakStyle (lines 311-321).</summary>
    [Fact]
    public void RoundTrip_WithAxisBreaks_PreservesBothAxes()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 10], [1.0, 10]))
            .Build();
        fig.SubPlots[0].AddXBreak(3, 5, BreakStyle.Zigzag);
        fig.SubPlots[0].AddYBreak(6, 8, BreakStyle.Zigzag);
        var rt = RoundTrip(fig);
        Assert.Single(rt.SubPlots[0].XBreaks);
        Assert.Single(rt.SubPlots[0].YBreaks);
        Assert.Equal(3, rt.SubPlots[0].XBreaks[0].From);
        Assert.Equal(5, rt.SubPlots[0].XBreaks[0].To);
    }

    /// <summary>Round-trips an Inset axes with its own series (lines 298-309).</summary>
    [Fact]
    public void RoundTrip_WithInsetAndSeries_PreservesInsetTreeAndSeries()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 5], [1.0, 5])
                .AddInset(0.6, 0.6, 0.3, 0.3, inset => inset
                    .WithTitle("zoom")
                    .Plot([1.0, 2], [3.0, 4])))
            .Build();
        var rt = RoundTrip(fig);
        Assert.Single(rt.SubPlots[0].Insets);
        Assert.Equal("zoom", rt.SubPlots[0].Insets[0].Title);
        Assert.Single(rt.SubPlots[0].Insets[0].Series);
    }

    /// <summary>Round-trips a GridSpec figure with explicit GridPosition (lines 59-63, 208-211).</summary>
    [Fact]
    public void RoundTrip_WithGridSpecAndPositions_PreservesLayout()
    {
        var fig = Plt.Create()
            .WithGridSpec(2, 3)
            .AddSubPlot(new GridPosition(0, 1, 0, 2), ax => ax.Plot([1.0, 2], [3.0, 4]))
            .AddSubPlot(new GridPosition(1, 2, 0, 3), ax => ax.Bar(["A", "B"], [1.0, 2.0]))
            .Build();
        var rt = RoundTrip(fig);
        Assert.NotNull(rt.GridSpec);
        Assert.Equal(2, rt.GridSpec.Rows);
        Assert.Equal(3, rt.GridSpec.Cols);
        Assert.Equal(2, rt.SubPlots.Count);
        Assert.NotNull(rt.SubPlots[0].GridPosition);
        Assert.NotNull(rt.SubPlots[1].GridPosition);
    }

    /// <summary>Round-trips a DirectionalLight on a 3D axes — exercises the
    /// `directional:dx,dy,dz,ambient,diffuse` parsing branch (lines 119-121, 230-239).</summary>
    [Fact]
    public void RoundTrip_WithDirectionalLight_PreservesAllFiveComponents()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Surface(
                [0.0, 1.0],
                [0.0, 1.0],
                new double[,] { { 0, 1 }, { 1, 0 } }))
            .Build();
        fig.SubPlots[0].LightSource = new global::MatPlotLibNet.Rendering.Lighting.DirectionalLight(0.5, -0.7, 0.3, 0.25, 0.85);
        var rt = RoundTrip(fig);
        var dl = Assert.IsType<global::MatPlotLibNet.Rendering.Lighting.DirectionalLight>(rt.SubPlots[0].LightSource);
        Assert.Equal(0.5, dl.Dx);
        Assert.Equal(-0.7, dl.Dy);
        Assert.Equal(0.3, dl.Dz);
        Assert.Equal(0.25, dl.Ambient);
        Assert.Equal(0.85, dl.Diffuse);
    }

    /// <summary>Round-trips a SecondaryYAxis with line + scatter on it
    /// (lines 270-285 — TwinX dispatch through CreateSecondaryLine/Scatter).</summary>
    [Fact]
    public void RoundTrip_WithSecondaryYAxisAndSeries_PreservesAxisAndSeries()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .WithSecondaryYAxis(s => s
                    .SetYLabel("right")
                    .Plot([1.0, 2], [50.0, 60])
                    .Scatter([1.5], [55.0])))
            .Build();
        var rt = RoundTrip(fig);
        Assert.NotNull(rt.SubPlots[0].SecondaryYAxis);
        Assert.Equal("right", rt.SubPlots[0].SecondaryYAxis!.Label);
        Assert.Equal(2, rt.SubPlots[0].SecondarySeries.Count);
    }

    /// <summary>Round-trips a multi-subplot figure with shared X-axis keys
    /// (lines 324-339 — sharing-by-key resolution loop).</summary>
    [Fact]
    public void RoundTrip_WithSharedXAxisByKey_PreservesShareReference()
    {
        var fig = Plt.Create()
            .AddSubPlot(2, 1, 1, ax => ax.Plot([1.0, 5], [1.0, 5]))
            .AddSubPlot(2, 1, 2, ax => ax.Plot([1.0, 5], [2.0, 4]))
            .Build();
        fig.SubPlots[0].Key = "top";
        fig.SubPlots[1].Key = "bot";
        fig.SubPlots[1].ShareXWith = fig.SubPlots[0];
        var rt = RoundTrip(fig);
        Assert.NotNull(rt.SubPlots[1].ShareXWith);
        Assert.Equal("top", rt.SubPlots[1].ShareXWith!.Key);
    }

    /// <summary>Forwards-compat: extra unknown JSON properties are ignored, no exception.</summary>
    [Fact]
    public void FromJson_WithExtraUnknownFields_IgnoresAndDeserializes()
    {
        const string json = """{"width":800,"height":600,"futureUnknownField":42,"anotherFuture":{"nested":true}}""";
        var fig = S.FromJson(json);
        Assert.Equal(800, fig.Width);
        Assert.Equal(600, fig.Height);
    }

    /// <summary>Unknown series type discriminator → factory returns null,
    /// AddSeriesFromDto silently skips. Round-trip succeeds with one fewer series.</summary>
    [Fact]
    public void FromJson_WithUnknownSeriesType_SkipsGracefully()
    {
        const string json = """
        {
            "width": 800, "height": 600,
            "subPlots": [
                {
                    "series": [
                        {"type": "futureSeriesType", "label": "ignored"}
                    ]
                }
            ]
        }
        """;
        var fig = S.FromJson(json);
        Assert.Empty(fig.SubPlots[0].Series);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Phase Ω.1 — SeriesRegistry: round-trip with FULLY-POPULATED optional
    // properties, flipping every `if (dto.X.HasValue)` arm in each factory.
    // Pre-Ω.1: SeriesRegistry 99.1L / 72.8B (89 uncovered lines).
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_HexbinFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Hexbin([1.0, 2, 3], [1.0, 2, 3], s =>
            {
                s.GridSize = 30;
                s.MinCount = 5;
                s.ColorMap = global::MatPlotLibNet.Styling.ColorMaps.ColorMaps.Viridis;
            }))
            .Build();
        var rt = RoundTrip(fig);
        var hb = (HexbinSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(30, hb.GridSize);
        Assert.Equal(5, hb.MinCount);
        Assert.NotNull(hb.ColorMap);
    }

    [Fact]
    public void RoundTrip_RegressionFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Regression([1.0, 2, 3], [1.0, 2, 3], s =>
            {
                s.Degree = 3;
                s.ShowConfidence = true;
                s.ConfidenceLevel = 0.99;
                s.LineWidth = 4.0;
                s.Color = Colors.Red;
                s.BandColor = Colors.Blue;
                s.BandAlpha = 0.5;
                s.LineStyle = LineStyle.Dashed;
            }))
            .Build();
        var rt = RoundTrip(fig);
        var rs = (RegressionSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(3, rs.Degree);
        Assert.True(rs.ShowConfidence);
        Assert.Equal(0.99, rs.ConfidenceLevel);
        Assert.Equal(4.0, rs.LineWidth);
        Assert.Equal(Colors.Red, rs.Color);
        Assert.Equal(LineStyle.Dashed, rs.LineStyle);
    }

    [Fact]
    public void RoundTrip_KdeFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Kde([1.0, 2, 3, 4, 5], s =>
            {
                s.Bandwidth = 0.5;
                s.Alpha = 0.7;
                s.LineWidth = 3.0;
                s.Color = Colors.Green;
                s.LineStyle = LineStyle.Dotted;
            }))
            .Build();
        var rt = RoundTrip(fig);
        var ks = (KdeSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(0.5, ks.Bandwidth);
        Assert.Equal(0.7, ks.Alpha);
        Assert.Equal(LineStyle.Dotted, ks.LineStyle);
    }

    [Fact]
    public void RoundTrip_HeatmapWithColorMap_FlipsColorMapArm()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }, s =>
            {
                s.ColorMap = global::MatPlotLibNet.Styling.ColorMaps.ColorMaps.Plasma;
            }))
            .Build();
        var rt = RoundTrip(fig);
        var hs = (HeatmapSeries)rt.SubPlots[0].Series[0];
        Assert.NotNull(hs.ColorMap);
    }

    [Fact]
    public void RoundTrip_SurfaceFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Surface(
                [0.0, 1.0],
                [0.0, 1.0],
                new double[,] { { 0, 1 }, { 1, 0 } },
                s => { s.ShowWireframe = true; s.RowStride = 2; s.ColStride = 3; s.Alpha = 0.6; }))
            .Build();
        var rt = RoundTrip(fig);
        var ss = (SurfaceSeries)rt.SubPlots[0].Series[0];
        Assert.True(ss.ShowWireframe);
        Assert.Equal(2, ss.RowStride);
        Assert.Equal(3, ss.ColStride);
        Assert.Equal(0.6, ss.Alpha);
    }

    [Fact]
    public void RoundTrip_WireframeFullyPopulated_FlipsColorAndLineWidthArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Wireframe(
                [0.0, 1.0],
                [0.0, 1.0],
                new double[,] { { 0, 1 }, { 1, 0 } },
                s => { s.Color = Colors.Magenta; s.LineWidth = 2.5; }))
            .Build();
        var rt = RoundTrip(fig);
        var ws = (WireframeSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(Colors.Magenta, ws.Color);
        Assert.Equal(2.5, ws.LineWidth);
    }

    [Fact]
    public void RoundTrip_Scatter3DFullyPopulated_FlipsColorAndMarkerSizeArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Scatter3D([1.0, 2], [3.0, 4], [5.0, 6],
                s => { s.Color = Colors.Cyan; s.MarkerSize = 12; }))
            .Build();
        var rt = RoundTrip(fig);
        var s3 = (Scatter3DSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(Colors.Cyan, s3.Color);
        Assert.Equal(12, s3.MarkerSize);
    }

    [Fact]
    public void RoundTrip_RugplotFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Rugplot([1.0, 2, 3, 4, 5],
                s => { s.Height = 0.1; s.Alpha = 0.8; s.LineWidth = 2.0; s.Color = Colors.Black; }))
            .Build();
        var rt = RoundTrip(fig);
        var rs = (RugplotSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(0.1, rs.Height);
        Assert.Equal(0.8, rs.Alpha);
        Assert.Equal(2.0, rs.LineWidth);
        Assert.Equal(Colors.Black, rs.Color);
    }

    [Fact]
    public void RoundTrip_Plot3DFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot3D([1.0, 2], [3.0, 4], [5.0, 6],
                s => { s.Color = Colors.Red; s.LineWidth = 3.0; s.LineStyle = LineStyle.Dashed; }))
            .Build();
        var rt = RoundTrip(fig);
        var ls = (Line3DSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(Colors.Red, ls.Color);
        Assert.Equal(3.0, ls.LineWidth);
        Assert.Equal(LineStyle.Dashed, ls.LineStyle);
    }

    [Fact]
    public void RoundTrip_Stem3DFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Stem3D([1.0, 2], [3.0, 4], [5.0, 6],
                s => { s.Color = Colors.Orange; s.MarkerSize = 8; }))
            .Build();
        var rt = RoundTrip(fig);
        var ss = (Stem3DSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(Colors.Orange, ss.Color);
        Assert.Equal(8, ss.MarkerSize);
    }

    [Fact]
    public void RoundTrip_TrisurfFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Trisurf([0.0, 1, 2], [0.0, 1, 2], [0.0, 1, 4],
                s =>
                {
                    s.Color = Colors.Magenta;
                    s.ShowWireframe = true;
                    s.Alpha = 0.7;
                }))
            .Build();
        var rt = RoundTrip(fig);
        var ts = (Trisurf3DSeries)rt.SubPlots[0].Series[0];
        Assert.True(ts.ShowWireframe);
        Assert.Equal(0.7, ts.Alpha);
    }

    [Fact]
    public void RoundTrip_Contour3DFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Contour3D(
                [0.0, 1.0],
                [0.0, 1.0],
                new double[,] { { 0, 1 }, { 1, 0 } },
                s =>
                {
                    s.Color = Colors.Red;
                    s.Levels = 5;
                    s.LineWidth = 1.5;
                }))
            .Build();
        var rt = RoundTrip(fig);
        var c3 = (Contour3DSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(5, c3.Levels);
        Assert.Equal(1.5, c3.LineWidth);
    }

    [Fact]
    public void RoundTrip_Quiver3DFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Quiver3D(
                [0.0], [0.0], [0.0],
                [1.0], [1.0], [1.0],
                s => { s.ArrowLength = 2.5; s.Color = Colors.Green; }))
            .Build();
        var rt = RoundTrip(fig);
        var q3 = (Quiver3DSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(2.5, q3.ArrowLength);
        Assert.Equal(Colors.Green, q3.Color);
    }

    [Fact]
    public void RoundTrip_PolarHeatmapWithColorMap_FlipsColorMapArm()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PolarHeatmap(new double[,] { { 1, 2 }, { 3, 4 } }, 4, 2,
                s => { s.ColorMap = global::MatPlotLibNet.Styling.ColorMaps.ColorMaps.Viridis; }))
            .Build();
        var rt = RoundTrip(fig);
        var ph = (PolarHeatmapSeries)rt.SubPlots[0].Series[0];
        Assert.NotNull(ph.ColorMap);
    }

    [Fact]
    public void RoundTrip_TripcolorFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Tripcolor([0.0, 1, 0.5], [0.0, 0, 1], [1.0, 2, 3],
                s =>
                {
                    s.Triangles = new int[] { 0, 1, 2 };
                    s.ColorMap = global::MatPlotLibNet.Styling.ColorMaps.ColorMaps.Plasma;
                }))
            .Build();
        var rt = RoundTrip(fig);
        var tp = (TripcolorSeries)rt.SubPlots[0].Series[0];
        Assert.NotNull(tp.Triangles);
        Assert.NotNull(tp.ColorMap);
    }

    [Fact]
    public void RoundTrip_TricontourWithColorMap_FlipsLevelsAndColorMapArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Tricontour([0.0, 1, 0.5], [0.0, 0, 1], [1.0, 2, 3],
                s => { s.Levels = 8; s.ColorMap = global::MatPlotLibNet.Styling.ColorMaps.ColorMaps.Viridis; }))
            .Build();
        var rt = RoundTrip(fig);
        var tc = (TricontourSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(8, tc.Levels);
        Assert.NotNull(tc.ColorMap);
    }

    [Fact]
    public void RoundTrip_StripplotFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Stripplot(new double[][] { [1, 2, 3], [4, 5, 6] },
                s => { s.Jitter = 0.2; s.MarkerSize = 6; s.Alpha = 0.7; s.Color = Colors.Red; }))
            .Build();
        var rt = RoundTrip(fig);
        var sp = (StripplotSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(0.2, sp.Jitter);
        Assert.Equal(6, sp.MarkerSize);
    }

    [Fact]
    public void RoundTrip_PointplotFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pointplot(new double[][] { [1, 2, 3], [4, 5, 6] },
                s =>
                {
                    s.MarkerSize = 8;
                    s.CapSize = 5;
                    s.ConfidenceLevel = 0.95;
                    s.Color = Colors.Blue;
                    s.Categories = ["A", "B"];
                }))
            .Build();
        var rt = RoundTrip(fig);
        var pp = (PointplotSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(8, pp.MarkerSize);
        Assert.Equal(5, pp.CapSize);
        Assert.NotNull(pp.Categories);
    }

    [Fact]
    public void RoundTrip_SwarmplotFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Swarmplot(new double[][] { [1, 2, 3], [4, 5, 6] },
                s => { s.MarkerSize = 5; s.Alpha = 0.6; s.Color = Colors.Magenta; }))
            .Build();
        var rt = RoundTrip(fig);
        var sw = (SwarmplotSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(5, sw.MarkerSize);
        Assert.Equal(0.6, sw.Alpha);
    }

    [Fact]
    public void RoundTrip_SpectrogramFullyPopulated_FlipsAllOptionalArms()
    {
        var signal = new double[100];
        for (int i = 0; i < signal.Length; i++) signal[i] = Math.Sin(i * 0.1);
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Spectrogram(signal, sampleRate: 1000, configure: s =>
            {
                s.WindowSize = 32;
                s.Overlap = 16;
                s.ColorMap = global::MatPlotLibNet.Styling.ColorMaps.ColorMaps.Viridis;
            }))
            .Build();
        var rt = RoundTrip(fig);
        var sp = (SpectrogramSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(32, sp.WindowSize);
        Assert.Equal(16, sp.Overlap);
    }

    [Fact]
    public void RoundTrip_EventplotWithLineLength_FlipsLineLengthArm()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Eventplot(new double[][] { [1, 2, 3] },
                s => { s.LineWidth = 2.0; s.LineLength = 0.8; }))
            .Build();
        var rt = RoundTrip(fig);
        var ep = (EventplotSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(2.0, ep.LineWidth);
        Assert.Equal(0.8, ep.LineLength);
    }

    [Fact]
    public void RoundTrip_BrokenBarHFullyPopulated_FlipsAllOptionalArms()
    {
        var ranges = new (double, double)[][] { [(1, 2), (4, 1)] };
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.BrokenBarH(ranges,
                s => { s.BarHeight = 0.5; s.Color = Colors.Red; s.Labels = ["row1"]; }))
            .Build();
        var rt = RoundTrip(fig);
        var bb = (BrokenBarSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(0.5, bb.BarHeight);
        Assert.NotNull(bb.Labels);
    }

    [Fact]
    public void RoundTrip_CountplotFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Countplot(["A", "A", "B", "C"],
                s => { s.Color = Colors.Blue; s.BarWidth = 0.6; s.Orientation = BarOrientation.Horizontal; }))
            .Build();
        var rt = RoundTrip(fig);
        var cs = (CountSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(0.6, cs.BarWidth);
        Assert.Equal(BarOrientation.Horizontal, cs.Orientation);
    }

    [Fact]
    public void RoundTrip_ResidplotFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Residplot([1.0, 2, 3, 4], [1.5, 2.1, 2.8, 4.2],
                s => { s.Degree = 2; s.MarkerSize = 8; s.Color = Colors.Red; }))
            .Build();
        var rt = RoundTrip(fig);
        var rp = (ResidualSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(2, rp.Degree);
        Assert.Equal(8, rp.MarkerSize);
    }

    [Fact]
    public void RoundTrip_PcolormeshWithColorMap_FlipsColorMapArm()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pcolormesh(
                [0.0, 1, 2],
                [0.0, 1, 2],
                new double[,] { { 1, 2 }, { 3, 4 } },
                s => { s.ColorMap = global::MatPlotLibNet.Styling.ColorMaps.ColorMaps.Plasma; }))
            .Build();
        var rt = RoundTrip(fig);
        var pm = (PcolormeshSeries)rt.SubPlots[0].Series[0];
        Assert.NotNull(pm.ColorMap);
    }

    [Fact]
    public void RoundTrip_BarbsFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Barbs([0.0], [0.0], [10.0], [45.0],
                s => { s.BarbLength = 8; s.Color = Colors.Black; }))
            .Build();
        var rt = RoundTrip(fig);
        var bs = (BarbsSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(8, bs.BarbLength);
        Assert.Equal(Colors.Black, bs.Color);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Phase Ω.2 — ChartSerializer.Create* static methods with full config.
    // Pre-Ω.2: ChartSerializer 99.4L / 80.5B (49 uncovered lines).
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_ScatterWithMarkerSize_FlipsMarkerSizeArm()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Scatter([1.0, 2], [3.0, 4], s =>
            {
                s.Color = Colors.Red;
                s.MarkerSize = 12;
            }))
            .Build();
        var rt = RoundTrip(fig);
        var ss = (ScatterSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(12, ss.MarkerSize);
    }

    [Fact]
    public void RoundTrip_BarWithWidthAndOrientation_FlipsBarWidthAndOrientationArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bar(["A", "B"], [1.0, 2.0], s =>
            {
                s.Color = Colors.Blue;
                s.BarWidth = 0.7;
                s.Orientation = BarOrientation.Horizontal;
            }))
            .Build();
        var rt = RoundTrip(fig);
        var bs = (BarSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(0.7, bs.BarWidth);
        Assert.Equal(BarOrientation.Horizontal, bs.Orientation);
    }

    [Fact]
    public void RoundTrip_RadarFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Radar(["A", "B", "C"], [1.0, 2.0, 3.0], s =>
            {
                s.Color = Colors.Red;
                s.FillColor = Colors.Blue;
                s.Alpha = 0.5;
                s.LineWidth = 3.0;
                s.MaxValue = 5.0;
            }))
            .Build();
        var rt = RoundTrip(fig);
        var rs = (RadarSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(0.5, rs.Alpha);
        Assert.Equal(3.0, rs.LineWidth);
        Assert.Equal(5.0, rs.MaxValue);
    }

    [Fact]
    public void RoundTrip_QuiverWithScaleAndArrowHead_FlipsBothArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Quiver([0.0], [0.0], [1.0], [1.0], s =>
            {
                s.Color = Colors.Red;
                s.Scale = 2.5;
                s.ArrowHeadSize = 12;
            }))
            .Build();
        var rt = RoundTrip(fig);
        var qs = (QuiverSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(2.5, qs.Scale);
        Assert.Equal(12, qs.ArrowHeadSize);
    }

    [Fact]
    public void RoundTrip_StreamplotWithColorAndLineWidth_FlipsBothArms()
    {
        var u = new double[2, 2] { { 1, 1 }, { 1, 1 } };
        var v = new double[2, 2] { { 1, 1 }, { 1, 1 } };
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Streamplot([0.0, 1.0], [0.0, 1.0], u, v, s =>
            {
                s.Color = Colors.Red;
                s.LineWidth = 2.0;
            }))
            .Build();
        var rt = RoundTrip(fig);
        var ss = (StreamplotSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(Colors.Red, ss.Color);
        Assert.Equal(2.0, ss.LineWidth);
    }

    [Fact]
    public void RoundTrip_CandlestickWithUpDownAndBodyWidth_FlipsAllArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Candlestick([10.0, 11], [12.0, 13], [9.0, 10], [11.0, 12],
                dateLabels: ["2024-01-01", "2024-01-02"],
                configure: s =>
                {
                    s.UpColor = Colors.Green;
                    s.DownColor = Colors.Red;
                    s.BodyWidth = 0.6;
                }))
            .Build();
        var rt = RoundTrip(fig);
        var cs = (CandlestickSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(Colors.Green, cs.UpColor);
        Assert.Equal(Colors.Red, cs.DownColor);
        Assert.Equal(0.6, cs.BodyWidth);
    }

    [Fact]
    public void RoundTrip_ErrorBarWithCapSizeAndXErrors_FlipsAllArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.ErrorBar([1.0, 2], [3.0, 4], [0.1, 0.2], [0.1, 0.2], s =>
            {
                s.Color = Colors.Red;
                s.LineWidth = 2.0;
                s.CapSize = 5;
                s.XErrorLow = [0.05, 0.05];
                s.XErrorHigh = [0.05, 0.05];
            }))
            .Build();
        var rt = RoundTrip(fig);
        var es = (ErrorBarSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(5, es.CapSize);
        Assert.NotNull(es.XErrorLow);
    }

    [Fact]
    public void RoundTrip_EcdfWithColorAndStyle_FlipsAllArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Ecdf([1.0, 2, 3], s =>
            {
                s.Color = Colors.Red;
                s.LineWidth = 2.0;
                s.LineStyle = LineStyle.Dashed;
            }))
            .Build();
        var rt = RoundTrip(fig);
        var es = (EcdfSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(Colors.Red, es.Color);
        Assert.Equal(2.0, es.LineWidth);
        Assert.Equal(LineStyle.Dashed, es.LineStyle);
    }

    [Fact]
    public void RoundTrip_ImageFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Image(new double[,] { { 1, 2 }, { 3, 4 } }, s =>
            {
                s.ColorMap = global::MatPlotLibNet.Styling.ColorMaps.ColorMaps.Plasma;
                s.VMin = 0;
                s.VMax = 5;
                s.Interpolation = "bilinear";
            }))
            .Build();
        var rt = RoundTrip(fig);
        var img = (ImageSeries)rt.SubPlots[0].Series[0];
        Assert.NotNull(img.ColorMap);
        Assert.Equal(0, img.VMin);
        Assert.Equal(5, img.VMax);
        Assert.Equal("bilinear", img.Interpolation);
    }

    [Fact]
    public void RoundTrip_Histogram2DFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Histogram2D([1.0, 2, 3], [1.0, 2, 3], bins: 10, configure: s =>
            {
                s.BinsY = 15;
                s.ColorMap = global::MatPlotLibNet.Styling.ColorMaps.ColorMaps.Viridis;
            }))
            .Build();
        var rt = RoundTrip(fig);
        var h2 = (Histogram2DSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(15, h2.BinsY);
        Assert.NotNull(h2.ColorMap);
    }

    [Fact]
    public void RoundTrip_StackedAreaWithLabelsAndAlpha_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StackPlot(
                [1.0, 2],
                new double[][] { [1.0, 2], [3.0, 4] },
                s => { s.Labels = ["a", "b"]; s.Alpha = 0.6; }))
            .Build();
        var rt = RoundTrip(fig);
        var sa = (StackedAreaSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(0.6, sa.Alpha);
        Assert.NotNull(sa.Labels);
    }

    [Fact]
    public void RoundTrip_StepWithStyleAndPosition_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Step([1.0, 2, 3], [1.0, 4, 9], s =>
            {
                s.Color = Colors.Blue;
                s.LineWidth = 2.5;
                s.LineStyle = LineStyle.Dashed;
                s.StepPosition = StepPosition.Mid;
            }))
            .Build();
        var rt = RoundTrip(fig);
        var ss = (StepSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(LineStyle.Dashed, ss.LineStyle);
        Assert.Equal(StepPosition.Mid, ss.StepPosition);
    }

    [Fact]
    public void RoundTrip_AreaFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.FillBetween([1.0, 2, 3], [1.0, 2, 1], y2: [0.5, 1.5, 0.5], configure: s =>
            {
                s.Color = Colors.Red;
                s.Alpha = 0.6;
                s.LineWidth = 2.0;
                s.LineStyle = LineStyle.Dashed;
                s.Smooth = true;
                s.SmoothResolution = 10;
            }))
            .Build();
        var rt = RoundTrip(fig);
        var ar = (AreaSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(0.6, ar.Alpha);
        Assert.True(ar.Smooth);
        Assert.Equal(10, ar.SmoothResolution);
    }

    [Fact]
    public void RoundTrip_DonutFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Donut([30.0, 70.0], labels: ["A", "B"], configure: s =>
            {
                s.InnerRadius = 0.6;
                s.CenterText = "Total";
                s.StartAngle = 90;
            }))
            .Build();
        var rt = RoundTrip(fig);
        var ds = (DonutSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(0.6, ds.InnerRadius);
        Assert.Equal("Total", ds.CenterText);
        Assert.Equal(90, ds.StartAngle);
    }

    [Fact]
    public void RoundTrip_BubbleWithAlpha_FlipsAlphaArm()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bubble([1.0, 2], [3.0, 4], [10.0, 20], s =>
            {
                s.Color = Colors.Red;
                s.Alpha = 0.5;
            }))
            .Build();
        var rt = RoundTrip(fig);
        var bs = (BubbleSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(0.5, bs.Alpha);
    }

    [Fact]
    public void RoundTrip_SecondaryYAxisWithBothLineAndScatter_DispatchesToBothFactories()
    {
        // Lines 277-281 in ChartSerializer.cs — switch on dto.Type for secondary
        // series: line vs scatter. Both arms must fire.
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .WithSecondaryYAxis(s => s
                    .SetYLabel("right")
                    .Plot([1.0, 2], [50.0, 60], cfg => { cfg.Color = Colors.Red; cfg.LineWidth = 2.0; cfg.Marker = MarkerStyle.Circle; cfg.MarkerSize = 8; })
                    .Scatter([1.5], [55.0], cfg => { cfg.Color = Colors.Blue; cfg.MarkerSize = 10; })))
            .Build();
        var rt = RoundTrip(fig);
        Assert.Equal(2, rt.SubPlots[0].SecondarySeries.Count);
    }

    [Fact]
    public void RoundTrip_AnnotationsWithOnlyText_FlipsMinimalArm()
    {
        // Existing Phase-Z RoundTrip_WithAnnotationAndArrow_PreservesAllFields tests
        // the all-options arm. This fact tests the all-defaults arm (line 247 false branches).
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 5], [1.0, 5])
                .Annotate("plain", 2, 3))
            .Build();
        var rt = RoundTrip(fig);
        var ann = rt.SubPlots[0].Annotations[0];
        Assert.Equal("plain", ann.Text);
        Assert.Equal(ConnectionStyle.Straight, ann.ConnectionStyle);
        Assert.Equal(BoxStyle.None, ann.BoxStyle);
    }

    [Fact]
    public void RoundTrip_FigureWithNoSubPlots_PreservesGridSpec()
    {
        // Line 68 — `figure.GridSpec is { } gs ? ... : null` — covers both arms
        // already, but the (4/50) miss might be from a specific GridSpec config.
        // This fact uses heightRatios+widthRatios (named-args path).
        var fig = Plt.Create()
            .WithGridSpec(2, 2, heightRatios: [1.0, 2.0], widthRatios: [1.0, 1.0])
            .AddSubPlot(new GridPosition(0, 1, 0, 1), ax => ax.Plot([1.0, 2], [3.0, 4]))
            .Build();
        var rt = RoundTrip(fig);
        Assert.NotNull(rt.GridSpec);
        Assert.Equal([1.0, 2.0], rt.GridSpec!.HeightRatios!);
        Assert.Equal([1.0, 1.0], rt.GridSpec.WidthRatios!);
    }

    // ── Wave J.2 — missing series types + TryParse false arms ────────────

    [Fact]
    public void RoundTrip_TableWithHeadersAndRows_FlipsColumnAndRowHeaderArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Table(
                new string[][] { ["a", "b"], ["c", "d"] },
                s => { s.ColumnHeaders = ["Col1", "Col2"]; s.RowHeaders = ["R1", "R2"]; }))
            .Build();
        var rt = RoundTrip(fig);
        var t = (TableSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(["Col1", "Col2"], t.ColumnHeaders!);
        Assert.Equal(["R1", "R2"], t.RowHeaders!);
    }

    [Fact]
    public void RoundTrip_Bar3DFullyPopulated_FlipsBarWidthAndColorArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bar3D(
                [0.0, 1.0], [0.0, 1.0], [1.0, 2.0],
                s => { s.BarWidth = 0.6; s.Color = Colors.Red; }))
            .Build();
        var rt = RoundTrip(fig);
        var b3 = (Bar3DSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(0.6, b3.BarWidth);
        Assert.Equal(Colors.Red, b3.Color);
    }

    [Fact]
    public void RoundTrip_VoxelsFullyPopulated_FlipsColorAndAlphaArms()
    {
        var filled = new bool[2, 2, 2];
        filled[0, 0, 0] = true; filled[1, 1, 1] = true;
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Voxels(filled,
                s => { s.Color = Colors.Blue; s.Alpha = 0.7; }))
            .Build();
        var rt = RoundTrip(fig);
        var vs = (VoxelSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(Colors.Blue, vs.Color);
        Assert.Equal(0.7, vs.Alpha);
    }

    /// <summary>VoxelDataToArray with empty inner list — L354 `yDim > 0` false arm → zDim=0.</summary>
    [Fact]
    public void FromJson_VoxelSeriesWithEmptyInnerList_DeserializesGracefully()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[{
                "series":[{
                    "type":"voxels",
                    "voxelData":[[]]
                }]
            }]
        }
        """;
        var fig = S.FromJson(json);
        Assert.Single(fig.SubPlots[0].Series);
    }

    [Fact]
    public void RoundTrip_Text3DFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Text3D(1, 2, 3, "hello",
                s => { s.FontSize = 14; s.Color = Colors.Green; }))
            .Build();
        var rt = RoundTrip(fig);
        var t3 = (Text3DSeries)rt.SubPlots[0].Series[0];
        Assert.Single(t3.Annotations);
        Assert.Equal("hello", t3.Annotations[0].Text);
    }

    /// <summary>Countplot with invalid Orientation string — L180 TryParse false arm,
    /// Orientation stays at default Vertical.</summary>
    [Fact]
    public void FromJson_CountplotWithInvalidOrientation_DefaultsVertical()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[{
                "series":[{
                    "type":"count",
                    "categories":["A","B"],
                    "orientation":"notADirection"
                }]
            }]
        }
        """;
        var fig = S.FromJson(json);
        var cs = (CountSeries)fig.SubPlots[0].Series[0];
        Assert.Equal(BarOrientation.Vertical, cs.Orientation);
    }

    /// <summary>Regression with invalid LineStyle string — L53 TryParse false arm.</summary>
    [Fact]
    public void FromJson_RegressionWithInvalidLineStyle_DefaultsSolid()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[{
                "series":[{
                    "type":"regression",
                    "xData":[1,2,3],"yData":[1,2,3],
                    "lineStyle":"notAStyle"
                }]
            }]
        }
        """;
        var fig = S.FromJson(json);
        var rs = (RegressionSeries)fig.SubPlots[0].Series[0];
        Assert.Equal(LineStyle.Solid, rs.LineStyle);
    }

    // ── Wave J.1 — remaining SeriesRegistry HasValue TRUE arms ───────────────

    /// <summary>SeriesRegistry.Create with unknown type → null return (FALSE arm of TryGetValue ternary).</summary>
    [Fact]
    public void SeriesRegistry_Create_UnknownType_ReturnsNull()
    {
        var axes = new Axes();
        var result = MatPlotLibNet.Serialization.SeriesRegistry.Create("__no_such_type__", axes, new MatPlotLibNet.Serialization.SeriesDto());
        Assert.Null(result);
    }

    [Fact]
    public void RoundTrip_EventplotFullyPopulated_FlipsLineWidthAndLineLengthArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Eventplot([[1.0, 2.0], [3.0, 4.0]], s =>
            {
                s.LineWidth = 3.0; s.LineLength = 0.5;
            }))
            .Build();
        var rt = RoundTrip(fig);
        var es = (MatPlotLibNet.Models.Series.EventplotSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(3.0, es.LineWidth);
        Assert.Equal(0.5, es.LineLength);
    }

    [Fact]
    public void RoundTrip_ResidualFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Residplot([1.0, 2, 3], [1.1, 2.1, 3.1], s =>
            {
                s.Degree = 2; s.MarkerSize = 8.0; s.Color = Colors.Green;
            }))
            .Build();
        var rt = RoundTrip(fig);
        var rs = (MatPlotLibNet.Models.Series.ResidualSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(2, rs.Degree);
        Assert.Equal(Colors.Green, rs.Color);
    }

    [Fact]
    public void RoundTrip_Line3DFullyPopulated_FlipsAllOptionalArms()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot3D([0.0, 1, 2], [0.0, 1, 2], [0.0, 1, 2], s =>
            {
                s.Color = Colors.Blue; s.LineWidth = 2.5; s.LineStyle = LineStyle.Dashed;
            }))
            .Build();
        var rt = RoundTrip(fig);
        var ls = (MatPlotLibNet.Models.Series.Line3DSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(Colors.Blue, ls.Color);
        Assert.Equal(LineStyle.Dashed, ls.LineStyle);
    }

    [Fact]
    public void RoundTrip_SpectrogramFullyPopulated_FlipsWindowSizeOverlapColorMapArms()
    {
        var signal = Enumerable.Range(0, 64).Select(i => Math.Sin(i * 0.5)).ToArray();
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Spectrogram(signal, 1000, s =>
            {
                s.WindowSize = 32; s.Overlap = 16;
                s.ColorMap = MatPlotLibNet.Styling.ColorMaps.ColorMaps.Plasma;
            }))
            .Build();
        var rt = RoundTrip(fig);
        var ss = (MatPlotLibNet.Models.Series.SpectrogramSeries)rt.SubPlots[0].Series[0];
        Assert.Equal(32, ss.WindowSize);
        Assert.Equal(16, ss.Overlap);
    }
}
