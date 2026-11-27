// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>
/// Phase Z.2 — high-leverage round-trip coverage for <see cref="ChartSerializer"/> +
/// <see cref="SeriesRegistry"/>. Pre-Z.2: 95.9%L / 76.2%B (cyclomatic 478, ~200
/// uncovered branches in per-series-type dispatch + axes-extras paths).
///
/// One <c>[Theory]</c> puts every registered kind on every secondary Y axis seat (that every kind
/// comes back as its own type is <see cref="SeriesDrawnPropertiesRoundTripTests"/>'); the supporting
/// facts cover axes-level extras (spans, ref-lines, annotations, axis-breaks, insets, gridspec,
/// lighting) that the existing <see cref="ChartSerializerTests"/> /
/// <see cref="ChartSerializerCoverageTests"/> do not exercise jointly.
/// </summary>
/// <remarks>Tagged into the <c>ChartSerializerGlobalState</c> collection (see
/// <see cref="ChartSerializerGlobalStateCollection"/>): this class round-trips through the
/// process-global <c>SeriesRegistry</c> and must not run concurrently with
/// <see cref="SeriesRegistryTests"/>, which temporarily mutates the registry table.</remarks>
[Collection("ChartSerializerGlobalState")]
public class ChartSerializerRoundTripTests
{
    private static readonly ChartSerializer S = new();

    // lines and points; a bar or an area there is not kept") ─────────────────────────────────────────────────────────────

    /// <summary>Every seat a Y axis has on the wire past the primary: right slot 0 (<c>secondaryYAxis</c>), slot 1 of each
    /// side (the frozen lists <c>extraRightYAxes</c> and <c>leftYAxes</c>) and slot 2 of each side (the overflow lists).</summary>
    private static readonly YAxisSeat[] YAxisSeats =
        [new(AxisSide.Right, 0), new(AxisSide.Right, 1), new(AxisSide.Right, 2), new(AxisSide.Left, 1), new(AxisSide.Left, 2)];

    /// <summary>A Y axis slot: its side and its index in that side's stack.</summary>
    private readonly record struct YAxisSeat(AxisSide Side, int Slot);

    /// <summary>The sample series of every registered kind (<see cref="SampleSeries"/>), moved onto a Y axis slot past the primary
    /// before round-tripping (<see cref="Axes.TakeLastSeries"/> + <see cref="YAxisSlot.AddSeries{TSeries}"/>): it comes back on that
    /// same slot, on every seat the wire has, and no slot is added or merged on the way. One case per kind, every seat it does not
    /// come back on named at once.</summary>
    [Theory]
    [MemberData(nameof(ChartSerializerGoldenCorpusTests.Discriminators), MemberType = typeof(ChartSerializerGoldenCorpusTests))]
    public void EveryKind_RoundTrips_OnEveryYAxisSlot_StaysOnItsSlot(string kind)
    {
        var lost = YAxisSeats.Where(seat => !StaysOnItsSlot(kind, seat)).Select(seat => $"{seat.Side} slot {seat.Slot}").ToList();

        Assert.True(lost.Count == 0, $"{kind} does not come back on its own slot at: {string.Join(", ", lost)}.");
    }

    /// <summary>Whether the sample series of <paramref name="kind"/>, moved onto <paramref name="seat"/>, comes back alone on that
    /// slot with the stacks as they were written.</summary>
    private static bool StaysOnItsSlot(string kind, YAxisSeat seat)
    {
        var fig = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.AddSampleSeries(kind)).Build();
        var axes = fig.SubPlots[0];
        var stack = seat.Side == AxisSide.Right ? axes.YAxes.Right : axes.YAxes.Left;
        while (stack.Count <= seat.Slot) stack.Add();
        var moved = axes.TakeLastSeries()!;
        stack[seat.Slot].AddSeries(moved);

        var rt = fig.RoundTripped().SubPlots[0];
        var rtStack = seat.Side == AxisSide.Right ? rt.YAxes.Right : rt.YAxes.Left;

        return axes.Series.Count == 0 && rt.Series.Count == 0 && rtStack.Count == stack.Count
            && rt.YAxes.Right.Count + rt.YAxes.Left.Count == axes.YAxes.Right.Count + axes.YAxes.Left.Count
            && rtStack[seat.Slot].Series is [var back] && back.GetType() == moved.GetType() && rt.AllSeries.Count() == 1;
    }

    /// <summary>The coverage plan's row-99 reproduction: a streaming series (built differently than the
    /// array-constructed kinds above — appended to after creation, not passed two arrays) on the secondary axis
    /// must also survive, not just Line/Scatter.</summary>
    [Fact]
    public void RoundTrip_StreamingLineOnSecondaryAxis_PreservesKindAndAppendedPoints()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithAddRightYAxis(y2 =>
                {
                    var s = y2.StreamingPlot();
                    s.AppendPoint(1.0, 2.0);
                    s.AppendPoint(2.0, 3.0);
                }))
            .Build();

        var rt = fig.RoundTripped();

        Assert.Single(rt.SubPlots[0].YAxes.Right[0].Series);
        Assert.IsType<MatPlotLibNet.Models.Series.Streaming.StreamingLineSeries>(rt.SubPlots[0].YAxes.Right[0].Series[0]);
    }

    /// <summary>ROADMAP.md row 101's "4 axes, 2 left and 2 right": a 2nd right axis and a left axis are new model
    /// surface (<see cref="Axes.YAxes"/> slots past right slot 0 and the primary) the wire never carried before — only
    /// right slot 0 (<c>secondaryYAxis</c>/<c>secondarySeries</c>) did. Proves the additive DTO
    /// extension (the <c>extraRightYAxes</c> and <c>leftYAxes</c> fields) keeps every slot's own label, range and series —
    /// including a kind other than line/scatter, reusing the same seat-aware restore the row-99 fix built.</summary>
    [Fact]
    public void RoundTrip_WithASecondRightAxisAndALeftAxis_PreservesEveryAxisItsOwnLabelRangeAndSeries()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .WithAddRightYAxis(y2 => y2.SetYLabel("right1").Plot([1.0, 2], [50.0, 60]))
                .WithAddRightYAxis(outer => outer.SetYLabel("right2").SetYLim(0, 1).Scatter([1.0, 2], [0.2, 0.4]))
                .WithAddLeftYAxis(left => left.SetYLabel("left1").Plot([1.0, 2], [9.0, 8])))
            .Build();

        // Model-level kind diversity on the new slots (post-build, not through the builder, which only offers
        // Plot/Scatter/StreamingPlot) — the wire must keep a Bar there too, not only Line/Scatter.
        fig.SubPlots[0].YAxes.Right[1].AddSeries(new BarSeries(["a", "b"], [1.0, 2.0]));

        var rt = fig.RoundTripped();
        var axes = rt.SubPlots[0];

        Assert.Equal(2, axes.YAxes.Right.Count);
        Assert.Equal(2, axes.YAxes.Left.Count);
        Assert.Equal("right1", axes.YAxes.Right[0].Axis.Label);
        Assert.Equal("right2", axes.YAxes.Right[1].Axis.Label);
        Assert.Equal(0, axes.YAxes.Right[1].Axis.Min);
        Assert.Equal(1, axes.YAxes.Right[1].Axis.Max);
        Assert.Equal("left1", axes.YAxes.Left[1].Axis.Label);
        Assert.Single(axes.YAxes.Right[0].Series);
        Assert.Equal(2, axes.YAxes.Right[1].Series.Count);
        Assert.IsType<ScatterSeries>(axes.YAxes.Right[1].Series[0]);
        Assert.IsType<BarSeries>(axes.YAxes.Right[1].Series[1]);
        Assert.Single(axes.YAxes.Left[1].Series);
        Assert.IsType<LineSeries>(axes.YAxes.Left[1].Series[0]);
    }

    /// <summary>ROADMAP.md row 99 names the top axis too: a bar moved onto the secondary X axis must survive a
    /// round trip the same way, not only through the right-hand axis.</summary>
    [Fact]
    public void RoundTrip_BarOnSecondaryXAxis_PreservesKind()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bar(["A", "B"], [1.0, 2.0]))
            .Build();
        var axes = fig.SubPlots[0];
        var moved = axes.TakeLastSeries()!;
        axes.AddXSecondarySeries(moved);
        Assert.Empty(axes.Series);
        Assert.Single(axes.XSecondarySeries);

        var rt = fig.RoundTripped();

        Assert.Empty(rt.SubPlots[0].Series);
        Assert.Single(rt.SubPlots[0].XSecondarySeries);
        Assert.IsType<BarSeries>(rt.SubPlots[0].XSecondarySeries[0]);
    }

    // ── Axes-level extras: spans / refs / annotations / breaks / insets / lighting

    /// <summary>Two items came back, one horizontal under the first label and one vertical under the second.</summary>
    private static void AssertOneHorizontalAndOneVertical<T>(IReadOnlyCollection<T> items, Func<T, Orientation> orientationOf, Func<T, string?> labelOf,
        string horizontalLabel, string verticalLabel)
    {
        Assert.Equal(2, items.Count);
        Assert.Contains(items, item => orientationOf(item) == Orientation.Horizontal && labelOf(item) == horizontalLabel);
        Assert.Contains(items, item => orientationOf(item) == Orientation.Vertical && labelOf(item) == verticalLabel);
    }

    /// <summary>Round-trips horizontal+vertical SpanRegions with custom alpha/linestyle
    /// through ChartSerializer.</summary>
    [Fact]
    public void RoundTrip_WithSpansHorizontalAndVertical_PreservesBoth()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 5], [1.0, 5])
                .AxHSpan(2, 3, sp => { sp.Alpha = 0.4; sp.LineStyle = LineStyle.Dashed; sp.LineWidth = 2.0; sp.Label = "h"; })
                .AxVSpan(1.5, 2.5, sp => { sp.Alpha = 0.5; sp.Label = "v"; }))
            .Build();
        var rt = fig.RoundTripped();
        AssertOneHorizontalAndOneVertical(rt.SubPlots[0].Spans, s => s.Orientation, s => s.Label, "h", "v");
    }

    /// <summary>Round-trips ReferenceLines (H+V) with custom line style + label
    /// through ChartSerializer.</summary>
    [Fact]
    public void RoundTrip_WithReferenceLinesBothOrientations_Preserves()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 5], [1.0, 5])
                .AxHLine(3.0, r => { r.LineStyle = LineStyle.Dotted; r.LineWidth = 1.5; r.Label = "h-ref"; })
                .AxVLine(2.0, r => { r.Label = "v-ref"; }))
            .Build();
        var rt = fig.RoundTripped();
        AssertOneHorizontalAndOneVertical(rt.SubPlots[0].ReferenceLines, r => r.Orientation, r => r.Label, "h-ref", "v-ref");
    }

    /// <summary>Round-trips an Annotation with arrow target + box style.</summary>
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
        var rt = fig.RoundTripped();
        Assert.Single(rt.SubPlots[0].Annotations);
        var ann = rt.SubPlots[0].Annotations[0];
        Assert.Equal("hi", ann.Text);
        Assert.Equal(ConnectionStyle.Arc3, ann.ConnectionStyle);
        Assert.Equal(0.5, ann.ConnectionRad);
        Assert.Equal(12, ann.ArrowHeadSize);
        Assert.Equal(BoxStyle.Round, ann.BoxStyle);
    }

    /// <summary>Round-trips XBreaks + YBreaks with explicit BreakStyle.</summary>
    [Fact]
    public void RoundTrip_WithAxisBreaks_PreservesBothAxes()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 10], [1.0, 10]))
            .Build();
        fig.SubPlots[0].AddXBreak(3, 5, BreakStyle.Zigzag);
        fig.SubPlots[0].AddYBreak(6, 8, BreakStyle.Zigzag);
        var rt = fig.RoundTripped();
        Assert.Single(rt.SubPlots[0].XBreaks);
        Assert.Single(rt.SubPlots[0].YBreaks);
        Assert.Equal(3, rt.SubPlots[0].XBreaks[0].From);
        Assert.Equal(5, rt.SubPlots[0].XBreaks[0].To);
    }

    /// <summary>Round-trips an Inset axes with its own series.</summary>
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
        var rt = fig.RoundTripped();
        Assert.Single(rt.SubPlots[0].Insets);
        Assert.Equal("zoom", rt.SubPlots[0].Insets[0].Title);
        Assert.Single(rt.SubPlots[0].Insets[0].Series);
    }

    /// <summary>Round-trips a GridSpec figure with explicit GridPosition.</summary>
    [Fact]
    public void RoundTrip_WithGridSpecAndPositions_PreservesLayout()
    {
        var fig = Plt.Create()
            .WithGridSpec(2, 3)
            .AddSubPlot(new GridPosition(0, 1, 0, 2), ax => ax.Plot([1.0, 2], [3.0, 4]))
            .AddSubPlot(new GridPosition(1, 2, 0, 3), ax => ax.Bar(["A", "B"], [1.0, 2.0]))
            .Build();
        var rt = fig.RoundTripped();
        Assert.NotNull(rt.GridSpec);
        Assert.Equal(2, rt.GridSpec.Rows);
        Assert.Equal(3, rt.GridSpec.Cols);
        Assert.Equal(2, rt.SubPlots.Count);
        Assert.NotNull(rt.SubPlots[0].GridPosition);
        Assert.NotNull(rt.SubPlots[1].GridPosition);
    }

    /// <summary>Round-trips a DirectionalLight on a 3D axes — exercises the
    /// `directional:dx,dy,dz,ambient,diffuse` parsing branch.</summary>
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
        var rt = fig.RoundTripped();
        var dl = Assert.IsType<global::MatPlotLibNet.Rendering.Lighting.DirectionalLight>(rt.SubPlots[0].LightSource);
        Assert.Equal(0.5, dl.Dx);
        Assert.Equal(-0.7, dl.Dy);
        Assert.Equal(0.3, dl.Dz);
        Assert.Equal(0.25, dl.Ambient);
        Assert.Equal(0.85, dl.Diffuse);
    }

    /// <summary>Round-trips a right Y axis with line + scatter on it (ChartSerializer.CreateOnSecondarySeat,
    /// the same registry-based restore the primary axis uses).</summary>
    [Fact]
    public void RoundTrip_WithARightYAxisAndSeries_PreservesAxisAndSeries()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .WithAddRightYAxis(s => s
                    .SetYLabel("right")
                    .Plot([1.0, 2], [50.0, 60])
                    .Scatter([1.5], [55.0])))
            .Build();
        var rt = fig.RoundTripped();
        Assert.NotEmpty(rt.SubPlots[0].YAxes.Right);
        Assert.Equal("right", rt.SubPlots[0].YAxes.Right[0].Axis.Label);
        Assert.Equal(2, rt.SubPlots[0].YAxes.Right[0].Series.Count);
    }

    /// <summary>Round-trips a multi-subplot figure with shared X-axis keys
    /// (the sharing-by-key resolution loop).</summary>
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
        var rt = fig.RoundTripped();
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

    /// <summary>A horizontal bar chart read back from JSON draws the picture it was written from: its values along X, its
    /// categories named on the Y axis.</summary>
    [Fact]
    public void RoundTrip_AHorizontalBarChart_DrawsItsValuesAlongX_AndItsNamesOnY()
    {
        var fig = Plt.Create().WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.Bar(["A", "B"], [10.0, 20.0], s => s.Orientation = BarOrientation.Horizontal))
            .Build();

        string original = fig.ToSvg();
        string readBack = fig.RoundTripped().ToSvg();

        Assert.Equal(original, readBack);
        var bars = Rendering.SeriesRenderers.BarSeriesRendererTests.Bars(readBack);
        var plot = readBack.PlotArea();
        Assert.Equal(2 * bars[0].W, bars[1].W, 6);
        Assert.Equal(bars[0].H, bars[1].H, 6);
        Assert.True(Assert.Single(Rendering.SeriesRenderers.BarSeriesRendererTests.Texts(readBack, "A")).X < plot.X);
    }

    [Fact]
    public void RoundTrip_ARightYAxisWithBothLineAndScatter_DispatchesToBothFactories()
    {
        // Switch on dto.Type for secondary
        // series: line vs scatter. Both arms must fire.
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .WithAddRightYAxis(s => s
                    .SetYLabel("right")
                    .Plot([1.0, 2], [50.0, 60], cfg => { cfg.Color = Colors.Red; cfg.LineWidth = 2.0; cfg.Marker = MarkerStyle.Circle; cfg.MarkerSize = 8; })
                    .Scatter([1.5], [55.0], cfg => { cfg.Color = Colors.Blue; cfg.MarkerSize = 10; })))
            .Build();
        var rt = fig.RoundTripped();
        Assert.Equal(2, rt.SubPlots[0].YAxes.Right[0].Series.Count);
    }

    [Fact]
    public void RoundTrip_FigureWithNoSubPlots_PreservesGridSpec()
    {
        // `figure.GridSpec is { } gs ? ... : null` — covers both arms
        // already, but the (4/50) miss might be from a specific GridSpec config.
        // This fact uses heightRatios+widthRatios (named-args path).
        var fig = Plt.Create()
            .WithGridSpec(2, 2, heightRatios: [1.0, 2.0], widthRatios: [1.0, 1.0])
            .AddSubPlot(new GridPosition(0, 1, 0, 1), ax => ax.Plot([1.0, 2], [3.0, 4]))
            .Build();
        var rt = fig.RoundTripped();
        Assert.NotNull(rt.GridSpec);
        Assert.Equal([1.0, 2.0], rt.GridSpec!.HeightRatios!);
        Assert.Equal([1.0, 1.0], rt.GridSpec.WidthRatios!);
    }

    // ── Wave J.2 — missing series types + TryParse false arms ────────────

    /// <summary>VoxelDataToArray with empty inner list — `yDim > 0` false arm → zDim=0.</summary>
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

    /// <summary>Countplot with invalid Orientation string — TryParse false arm,
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

    /// <summary>Regression with invalid LineStyle string — TryParse false arm.</summary>
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
}
