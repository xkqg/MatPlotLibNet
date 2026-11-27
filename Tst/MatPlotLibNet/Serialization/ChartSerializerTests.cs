// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>Verifies <see cref="ChartSerializer"/> serialization behavior.</summary>
public class ChartSerializerTests
{
    /// <summary>Verifies that ToJson produces a valid JSON document with the expected title.</summary>
    [Fact]
    public void ToJson_ProducesValidJson()
    {
        var figure = Plt.Create()
            .WithTitle("Test")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();

        string json = ChartSerializerDefault.Current.ToJson(figure);

        var doc = JsonDocument.Parse(json);
        Assert.Equal("Test", doc.RootElement.GetProperty("title").GetString());
    }

    /// <summary>Verifies that ToJson includes width and height dimensions.</summary>
    [Fact]
    public void ToJson_IncludesDimensions()
    {
        var figure = Plt.Create()
            .WithSize(1024, 768)
            .Build();

        string json = ChartSerializerDefault.Current.ToJson(figure);
        var doc = JsonDocument.Parse(json);
        Assert.Equal(1024, doc.RootElement.GetProperty("width").GetDouble());
        Assert.Equal(768, doc.RootElement.GetProperty("height").GetDouble());
    }

    /// <summary>A series' <see cref="ChartSeries.ColorGroup"/> travels through JSON: two series written with the
    /// same group read back sharing it, and a figure in which nothing is grouped writes exactly what it wrote
    /// before the field existed (absent, never <c>0</c>).</summary>
    [Fact]
    public void ColorGroup_SurvivesJson()
    {
        var curve = new LineSeries([0.0, 1], [0.0, 1]);
        var band = new LineSeries([0.0, 1], [0.0, 1]);
        curve.ColorGroup = band.ColorGroup = 3;
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => { ax.AddSeries(curve); ax.AddSeries(band); }).Build();

        var readBack = figure.RoundTripped();

        var series = readBack.SubPlots[0].Series;
        Assert.Equal(3, ((ChartSeries)series[0]).ColorGroup);
        Assert.Equal(3, ((ChartSeries)series[1]).ColorGroup);
    }

    /// <summary>Verifies that a round-trip serialization preserves the figure title.</summary>
    [Fact]
    public void RoundTrip_PreservesTitle()
    {
        var original = Plt.Create()
            .WithTitle("Round Trip")
            .Build();

        var restored = original.RoundTripped();

        Assert.Equal("Round Trip", restored.Title);
    }

    /// <summary>Verifies that a round-trip serialization preserves width, height, and DPI.</summary>
    [Fact]
    public void RoundTrip_PreservesDimensions()
    {
        var original = Plt.Create()
            .WithSize(1024, 768)
            .WithDpi(150)
            .Build();

        var restored = original.RoundTripped();

        Assert.Equal(1024, restored.Width);
        Assert.Equal(768, restored.Height);
        Assert.Equal(150, restored.Dpi);
    }

    /// <summary>Verifies that a round-trip serialization preserves multiple subplots with distinct series types.</summary>
    [Fact]
    public void RoundTrip_PreservesMultipleSubPlots()
    {
        var original = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.WithTitle("Left").Plot([1.0], [2.0]))
            .AddSubPlot(1, 2, 2, ax => ax.WithTitle("Right").Scatter([3.0], [4.0]))
            .Build();

        var restored = original.RoundTripped();

        Assert.Equal(2, restored.SubPlots.Count);
        Assert.Equal("Left", restored.SubPlots[0].Title);
        Assert.Equal("Right", restored.SubPlots[1].Title);
        Assert.IsType<LineSeries>(restored.SubPlots[0].Series[0]);
        Assert.IsType<ScatterSeries>(restored.SubPlots[1].Series[0]);
    }

    /// <summary>Verifies that a round-trip serialization preserves axis labels and limits.</summary>
    [Fact]
    public void RoundTrip_PreservesAxisConfig()
    {
        var original = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.SetXLabel("Time")
                  .SetYLabel("Value")
                  .SetXLim(0, 100)
                  .SetYLim(-50, 50)
                  .Plot([1.0], [2.0]);
            })
            .Build();

        var restored = original.RoundTripped();

        Assert.Equal("Time", restored.SubPlots[0].XAxis.Label);
        Assert.Equal("Value", restored.SubPlots[0].YAxis.Label);
        Assert.Equal(0, restored.SubPlots[0].XAxis.Min);
        Assert.Equal(100, restored.SubPlots[0].XAxis.Max);
    }

    /// <summary>Verifies that a round-trip serialization preserves the right Y axis and its series.</summary>
    [Fact]
    public void RoundTrip_PreservesTheRightYAxis()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Plot([1.0, 2.0], [3.0, 4.0]);
        var right = axes.YAxes.Right.FirstOrAdd();
        right.Axis.Label = "Right";
        right.AddSeries(new LineSeries([1.0, 2.0], [100.0, 200.0]));

        var restored = figure.RoundTripped();
        Assert.NotEmpty(restored.SubPlots[0].YAxes.Right);
        Assert.Equal("Right", restored.SubPlots[0].YAxes.Right[0].Axis.Label);
        Assert.Single(restored.SubPlots[0].YAxes.Right[0].Series);
    }

    /// <summary>Verifies that a round-trip serialization preserves annotation text and arrow targets.</summary>
    [Fact]
    public void RoundTrip_PreservesAnnotations()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Plot([1.0, 2.0], [3.0, 4.0]);
        var ann = axes.Annotate("peak", 2.0, 4.0);
        ann.ArrowTargetX = 1.5;
        ann.ArrowTargetY = 3.5;

        var restored = figure.RoundTripped();
        Assert.Single(restored.SubPlots[0].Annotations);
        Assert.Equal("peak", restored.SubPlots[0].Annotations[0].Text);
        Assert.Equal(1.5, restored.SubPlots[0].Annotations[0].ArrowTargetX);
    }

    /// <summary>Verifies that a round-trip serialization preserves horizontal and vertical reference lines.</summary>
    [Fact]
    public void RoundTrip_PreservesReferenceLines()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Plot([1.0, 2.0], [3.0, 4.0]);
        axes.AxHLine(3.5);
        axes.AxVLine(1.5);

        var restored = figure.RoundTripped();
        Assert.Equal(2, restored.SubPlots[0].ReferenceLines.Count);
    }

    /// <summary>Verifies that a round-trip serialization preserves horizontal and vertical span regions.</summary>
    [Fact]
    public void RoundTrip_PreservesSpanRegions()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Plot([1.0, 2.0], [3.0, 4.0]);
        axes.AxHSpan(3.0, 3.5);
        axes.AxVSpan(1.2, 1.8);

        var restored = figure.RoundTripped();
        Assert.Equal(2, restored.SubPlots[0].Spans.Count);
    }

    /// <summary>Verifies that a round-trip serialization preserves the subplot bar mode.</summary>
    [Fact]
    public void RoundTrip_PreservesBarMode()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .SetBarMode(BarMode.Stacked)
                .Bar(["A", "B"], [10.0, 20.0]))
            .Build();

        var restored = figure.RoundTripped();
        Assert.Equal(BarMode.Stacked, restored.SubPlots[0].BarMode);
    }

    // --- GridSpec serialization ---

    /// <summary>Verifies that a GridSpec with ratios survives JSON round-trip.</summary>
    [Fact]
    public void RoundTrip_PreservesGridSpec()
    {
        var figure = new Figure { GridSpec = new GridSpec { Rows = 3, Cols = 2, HeightRatios = [1, 2, 1], WidthRatios = [1, 3] } };
        var gs = figure.GridSpec;
        figure.AddSubPlot(gs, GridPosition.Single(0, 0)).Plot([1.0], [2.0]);

        var restored = figure.RoundTripped();

        Assert.NotNull(restored.GridSpec);
        Assert.Equal(3, restored.GridSpec.Rows);
        Assert.Equal(2, restored.GridSpec.Cols);
        Assert.Equal([1.0, 2.0, 1.0], restored.GridSpec.HeightRatios!);
        Assert.Equal([1.0, 3.0], restored.GridSpec.WidthRatios!);
    }

    /// <summary>Verifies that a GridPosition survives JSON round-trip.</summary>
    [Fact]
    public void RoundTrip_PreservesGridPosition()
    {
        var figure = new Figure { GridSpec = new GridSpec { Rows = 2, Cols = 2 } };
        var gs = figure.GridSpec;
        figure.AddSubPlot(gs, GridPosition.Single(0, 1)).Plot([1.0], [2.0]);

        var restored = figure.RoundTripped();

        Assert.NotNull(restored.SubPlots[0].GridPosition);
        var pos = restored.SubPlots[0].GridPosition!.Value;
        Assert.Equal(0, pos.RowStart);
        Assert.Equal(1, pos.RowEnd);
        Assert.Equal(1, pos.ColStart);
        Assert.Equal(2, pos.ColEnd);
    }

    /// <summary>Verifies that a spanning GridPosition survives JSON round-trip.</summary>
    [Fact]
    public void RoundTrip_PreservesGridPosition_Spanning()
    {
        var figure = new Figure { GridSpec = new GridSpec { Rows = 3, Cols = 3 } };
        var gs = figure.GridSpec;
        figure.AddSubPlot(gs, new GridPosition(0, 2, 0, 3)).Plot([1.0], [2.0]);

        var restored = figure.RoundTripped();

        var pos = restored.SubPlots[0].GridPosition!.Value;
        Assert.Equal(0, pos.RowStart);
        Assert.Equal(2, pos.RowEnd);
        Assert.Equal(0, pos.ColStart);
        Assert.Equal(3, pos.ColEnd);
    }

    // --- Spines serialization ---

    /// <summary>Verifies that hidden spines survive JSON round-trip.</summary>
    [Fact]
    public void RoundTrip_PreservesSpinesConfig()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Spines = ax.Spines with
        {
            Top = new SpineConfig() with { Visible = false },
            Right = new SpineConfig() with { Visible = false }
        };
        ax.Plot([1.0], [2.0]);

        var restored = figure.RoundTripped();
        Assert.False(restored.SubPlots[0].Spines.Top.Visible);
        Assert.False(restored.SubPlots[0].Spines.Right.Visible);
        Assert.True(restored.SubPlots[0].Spines.Bottom.Visible);
        Assert.True(restored.SubPlots[0].Spines.Left.Visible);
    }

    /// <summary>Verifies that spine data position survives JSON round-trip.</summary>
    [Fact]
    public void RoundTrip_PreservesSpinePosition()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Spines = ax.Spines with
        {
            Bottom = new SpineConfig() with { Position = SpinePosition.Data, PositionValue = 0.0 }
        };
        ax.Plot([1.0], [2.0]);

        var restored = figure.RoundTripped();
        Assert.Equal(SpinePosition.Data, restored.SubPlots[0].Spines.Bottom.Position);
        Assert.Equal(0.0, restored.SubPlots[0].Spines.Bottom.PositionValue);
    }

    // --- Shared axes serialization ---

    /// <summary>Verifies that a shared X or Y axis survives JSON round-trip via keys.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RoundTrip_PreservesASharedAxis(bool shareX)
    {
        var figure = new Figure();
        var ax1 = shareX ? figure.AddSubPlot(2, 1, 1) : figure.AddSubPlot(1, 2, 1);
        ax1.Key = shareX ? "price" : "main";
        ax1.Plot([1.0, 2.0], [3.0, 4.0]);

        var ax2 = shareX ? figure.AddSubPlot(2, 1, 2, sharex: ax1) : figure.AddSubPlot(1, 2, 2, sharey: ax1);
        ax2.Key = shareX ? "volume" : "side";
        ax2.Plot([1.0, 2.0], [5.0, 6.0]);

        var restored = figure.RoundTripped();

        var shared = shareX ? restored.SubPlots[1].ShareXWith : restored.SubPlots[1].ShareYWith;
        Assert.NotNull(shared);
        Assert.Same(restored.SubPlots[0], shared);
    }

    // --- Inset axes serialization ---

    /// <summary>Verifies that an inset with series survives JSON round-trip.</summary>
    [Fact]
    public void RoundTrip_PreservesInsetAxes()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([1.0, 2.0], [3.0, 4.0]);
        var inset = ax.AddInset(0.6, 0.1, 0.35, 0.35);
        inset.Plot([1.5, 2.0], [3.5, 4.0]);

        var restored = figure.RoundTripped();

        Assert.Single(restored.SubPlots[0].Insets);
        Assert.Single(restored.SubPlots[0].Insets[0].Series);
    }

    /// <summary>Verifies that InsetBounds values are preserved.</summary>
    [Fact]
    public void RoundTrip_PreservesInsetBounds()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([1.0], [2.0]);
        ax.AddInset(0.6, 0.1, 0.35, 0.35).Plot([1.0], [2.0]);

        var restored = figure.RoundTripped();

        var bounds = restored.SubPlots[0].Insets[0].InsetBounds!.Value;
        Assert.Equal(0.6, bounds.X);
        Assert.Equal(0.1, bounds.Y);
        Assert.Equal(0.35, bounds.Width);
        Assert.Equal(0.35, bounds.Height);
    }

    /// <summary>Verifies that colormap name survives JSON round-trip.</summary>
    [Fact]
    public void RoundTrip_PreservesColorMapName()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        var hs = ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } });
        hs.ColorMap = MatPlotLibNet.Styling.ColorMaps.ColorMaps.Plasma;

        var restored = figure.RoundTripped();
        var restoredHs = (HeatmapSeries)restored.SubPlots[0].Series[0];
        Assert.NotNull(restoredHs.ColorMap);
        Assert.Equal("plasma", restoredHs.ColorMap!.Name);
    }

    /// <summary>Verifies that annotation ConnectionStyle round-trips through serialization.</summary>
    [Fact]
    public void RoundTrip_PreservesAnnotation_ConnectionStyle()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);
        var ann = ax.Annotate("label", 1.0, 1.0);
        ann.ArrowTargetX = 2.0;
        ann.ArrowTargetY = 2.0;
        ann.ConnectionStyle = MatPlotLibNet.Models.ConnectionStyle.Arc3;
        ann.ConnectionRad = 0.5;

        var restored = figure.RoundTripped();

        var restoredAnn = restored.SubPlots[0].Annotations[0];
        Assert.Equal(MatPlotLibNet.Models.ConnectionStyle.Arc3, restoredAnn.ConnectionStyle);
        Assert.Equal(0.5, restoredAnn.ConnectionRad);
    }

    /// <summary>Verifies that annotation BoxStyle round-trips through serialization.</summary>
    [Fact]
    public void RoundTrip_PreservesAnnotation_BoxStyle()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);
        var ann = ax.Annotate("label", 1.5, 1.5);
        ann.BoxStyle = MatPlotLibNet.Models.BoxStyle.Round;
        ann.BoxPadding = 6;
        ann.BoxCornerRadius = 8;

        var restored = figure.RoundTripped();

        var restoredAnn = restored.SubPlots[0].Annotations[0];
        Assert.Equal(MatPlotLibNet.Models.BoxStyle.Round, restoredAnn.BoxStyle);
        Assert.Equal(6.0, restoredAnn.BoxPadding);
        Assert.Equal(8.0, restoredAnn.BoxCornerRadius);
    }

    /// <summary>Verifies that SpanRegion border and label round-trip through serialization.</summary>
    [Fact]
    public void RoundTrip_PreservesSpanRegion_BorderAndLabel()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 3.0], [0.0, 3.0]);
        var span = ax.AxHSpan(1.0, 2.0);
        span.LineStyle = LineStyle.Dashed;
        span.LineWidth = 2.0;
        span.Label = "support";

        var restored = figure.RoundTripped();

        var restoredSpan = restored.SubPlots[0].Spans[0];
        Assert.Equal(LineStyle.Dashed, restoredSpan.LineStyle);
        Assert.Equal(2.0, restoredSpan.LineWidth);
        Assert.Equal("support", restoredSpan.Label);
    }

    /// <summary>FromJson — `?? throw` true arm: empty/whitespace input.</summary>
    [Fact]
    public void FromJson_NullJson_ThrowsJsonException()
    {
        var s = new ChartSerializer();
        Assert.Throws<JsonException>(() => s.FromJson("null"));
    }

    [Fact]
    public void FromJson_MalformedJson_Throws()
    {
        var s = new ChartSerializer();
        Assert.ThrowsAny<JsonException>(() => s.FromJson("{not valid"));
    }

    /// <summary>FromJson minimal-valid JSON with no SubPlots — `?? []` empty arm.
    /// SubPlots may be null in the wire DTO; deserialiser must handle gracefully.</summary>
    [Fact]
    public void FromJson_MinimalValidNoSubPlots_RoundTripsToEmptyFigure()
    {
        var s = new ChartSerializer();
        var fig = s.FromJson("""{"width":800,"height":600}""");
        Assert.Equal(800, fig.Width);
        Assert.Equal(600, fig.Height);
        Assert.Empty(fig.SubPlots);
    }

    /// <summary>Round-trip with Enable3DRotation=true — true arm of
    /// `dto.Enable3DRotation is true`.</summary>
    [Fact]
    public void RoundTrip_With3DRotation_PreservesFlag()
    {
        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        fig.Enable3DRotation = true;
        var rt = fig.RoundTripped();
        Assert.True(rt.Enable3DRotation);
    }

    /// <summary>Round-trip without Enable3DRotation — false arm (null in DTO).</summary>
    [Fact]
    public void RoundTrip_Without3DRotation_DefaultsFalse()
    {
        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        var rt = fig.RoundTripped();
        Assert.False(rt.Enable3DRotation);
    }

    /// <summary>Round-trip with custom Spines — exercises DtoToSpine non-null arm
    /// (false arm) + serialisation of a non-default spine config.</summary>
    [Fact]
    public void RoundTrip_WithCustomSpines_PreservesConfig()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([1.0, 2], [3.0, 4]);
                ax.HideTopSpine();
                ax.HideRightSpine();
            })
            .Build();
        var rt = fig.RoundTripped();
        Assert.False(rt.SubPlots[0].Spines.Top.Visible);
        Assert.False(rt.SubPlots[0].Spines.Right.Visible);
    }

    /// <summary>Round-trip with 3D camera config (Elevation, Azimuth, CameraDistance) —
    /// true arms.</summary>
    [Fact]
    public void RoundTrip_WithCameraConfig_PreservesValues()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Scatter3D(new[] { 1.0 }, new[] { 2.0 }, new[] { 3.0 }))
            .Build();
        fig.SubPlots[0].Elevation = 25;
        fig.SubPlots[0].Azimuth = -45;
        fig.SubPlots[0].CameraDistance = 12.5;

        var rt = fig.RoundTripped();
        Assert.Equal(25.0, rt.SubPlots[0].Elevation);
        Assert.Equal(-45.0, rt.SubPlots[0].Azimuth);
        Assert.Equal(12.5, rt.SubPlots[0].CameraDistance);
    }

    /// <summary>Round-trip with stacked BarMode — true arms.</summary>
    [Fact]
    public void RoundTrip_WithStackedBarMode_PreservesMode()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Bar(["A", "B"], [1.0, 2.0]);
                ax.SetBarMode(BarMode.Stacked);
            })
            .Build();
        var rt = fig.RoundTripped();
        Assert.Equal(BarMode.Stacked, rt.SubPlots[0].BarMode);
    }

    /// <summary>Spine round-trip with explicit position — covers the SpinePosition switch's
    /// reachable arms (data, axes) without needing the unknown-arm test which requires
    /// JSON-injection that proved to be fragile against the multi-subplot case.</summary>
    [Theory]
    [InlineData(SpinePosition.Data)]
    [InlineData(SpinePosition.Axes)]
    [InlineData(SpinePosition.Edge)]
    public void RoundTrip_SpinePosition_PreservesValue(SpinePosition position)
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .WithSpines(s => s with
                {
                    Top = new SpineConfig { Position = position, PositionValue = 0.5, Visible = false }
                }))
            .Build();
        var rt = fig.RoundTripped();
        Assert.Equal(position, rt.SubPlots[0].Spines.Top.Position);
    }

    private static readonly ChartSerializer S = new();

    // ── ToJson indented arm ──────────────────────────────────────────────────

    // ── Annotation TryParse failure arms ─────────────────────────────────────

    /// <summary>A style the reader cannot parse (a name that is none, or null) leaves the member at the default of its kind: an annotation's
    /// connection and box style, a reference line's and a span's line style, a break's style (the break itself is still read).</summary>
    public static TheoryData<string, Func<Figure, object>, object> StylesThatAreNone { get; } = new()
    {
        { """" "annotations":[{"text":"hi","x":1,"y":1,"connectionStyle":"notAStyle"}] """", fig => fig.SubPlots[0].Annotations[0].ConnectionStyle, ConnectionStyle.Straight },
        { """" "annotations":[{"text":"hi","x":1,"y":1,"boxStyle":"notABoxStyle"}] """", fig => fig.SubPlots[0].Annotations[0].BoxStyle, BoxStyle.None },
        { """" "referenceLines":[{"value":5.0,"orientation":"horizontal","lineStyle":"notAStyle","lineWidth":1.0}] """", fig => fig.SubPlots[0].ReferenceLines[0].LineStyle, LineStyle.Dashed },
        { """" "spans":[{"min":1,"max":3,"orientation":"horizontal","alpha":0.3,"lineStyle":"notAStyle"}] """", fig => fig.SubPlots[0].Spans[0].LineStyle, LineStyle.None },
        { """" "xBreaks":[{"from":10,"to":20,"style":null}] """", fig => fig.SubPlots[0].XBreaks.Single().Style, BreakStyle.Zigzag },
        { """" "yBreaks":[{"from":5,"to":15,"style":"notAStyle"}] """", fig => fig.SubPlots[0].YBreaks.Single().Style, BreakStyle.Zigzag },
    };

    [Theory]
    [MemberData(nameof(StylesThatAreNone))]
    public void FromJson_AStyleThatIsNone_LeavesTheDefaultOfItsKind(string subPlotMembers, Func<Figure, object> read, object expected)
    {
        var fig = S.FromJson("{\"width\":800,\"height\":600,\"subPlots\":[{" + subPlotMembers + "}]}");

        Assert.Equal(expected, read(fig));
    }

    // ── ReferenceLines TryParse failure arm ──────────────────────────────────

    // ── right slot 0: unknown series type arm ─────────────────────────────────

    /// <summary>The right slot 0 axis is present but its series has an unknown type —
    /// switch default arm (`_ => null`) + `if (sec is not null)` false arm.</summary>
    [Fact]
    public void FromJson_ARightSlotZeroSeriesOfAnUnknownType_SkipsGracefully()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[{
                "secondaryYAxis":{"label":"right"},
                "secondarySeries":[{"type":"futureType","label":"skip"}]
            }]
        }
        """;
        var fig = S.FromJson(json);
        Assert.NotEmpty(fig.SubPlots[0].YAxes.Right);
        Assert.Empty(fig.SubPlots[0].YAxes.Right[0].Series);
    }

    // ── Spans TryParse failure arm ────────────────────────────────────────────

    // ── BreakStyle null / invalid arms ───────────────────────────────────────

    // ── ShareX/Y key not found arm ────────────────────────────────────────────

    /// <summary>ShareXKey pointing to a non-existent key → TryGetValue false,
    /// ShareXWith stays null.</summary>
    [Fact]
    public void FromJson_ShareXKeyNotFound_ShareXWithStaysNull()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[
                {"key":"ax1"},
                {"shareXKey":"doesNotExist"}
            ]
        }
        """;
        var fig = S.FromJson(json);
        Assert.Null(fig.SubPlots[1].ShareXWith);
    }

    /// <summary>A place no subplot holds joins nothing.</summary>
    [Theory]
    [InlineData("shareXSubplot", 7)]
    [InlineData("shareYSubplot", -1)]
    public void FromJson_ASharePlaceNoSubplotHolds_JoinsNothing(string field, int place)
    {
        string json = $$"""{"width":800,"height":600,"subPlots":[{},{"{{field}}":{{place}}}]}""";

        var fig = S.FromJson(json);

        Assert.Null(fig.SubPlots[1].ShareXWith);
        Assert.Null(fig.SubPlots[1].ShareYWith);
    }

    // ── ApplyAxis invalid scale string ────────────────────────────────────────

    /// <summary>XAxis with an unrecognised scale string → TryParse false,
    /// Scale stays at default Linear.</summary>
    [Fact]
    public void FromJson_AxisWithInvalidScale_DefaultsLinear()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[{
                "xAxis":{"scale":"notAScale"}
            }]
        }
        """;
        var fig = S.FromJson(json);
        Assert.Equal(AxisScale.Linear, fig.SubPlots[0].XAxis.Scale);
    }

    // ── Inset without InsetBounds — defensive continue arm ───────────────────

    /// <summary>Inset DTO missing InsetBounds → `continue` arm fires,
    /// no inset is added to the figure.</summary>
    [Fact]
    public void FromJson_InsetWithoutInsetBounds_SkipsInset()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[{
                "insets":[{"series":[]}]
            }]
        }
        """;
        var fig = S.FromJson(json);
        Assert.Empty(fig.SubPlots[0].Insets);
    }

    // ── ShareY key found — TRUE arm ──────────────────────────────────────────

    /// <summary>ShareYKey pointing to an existing key → TryGetValue TRUE arm,
    /// ShareYWith is resolved.</summary>
    [Fact]
    public void FromJson_ShareYKeyFound_ShareYWithResolved()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[
                {"key":"ax1"},
                {"shareYKey":"ax1"}
            ]
        }
        """;
        var fig = S.FromJson(json);
        Assert.NotNull(fig.SubPlots[1].ShareYWith);
        Assert.Same(fig.SubPlots[0], fig.SubPlots[1].ShareYWith);
    }

    // ── right slot 0 "scatter" type arm ──────────────────────────────────────

    /// <summary>The right slot 0 with a "scatter" series → restored through the registry, like the primary axis.</summary>
    [Fact]
    public void FromJson_ARightSlotZeroScatterSeries_CreatesScatterSeries()
    {
        const string json = """
        {
            "width":800,"height":600,
            "subPlots":[{
                "secondaryYAxis":{"label":"right"},
                "secondarySeries":[{
                    "type":"scatter","xData":[1,2],"yData":[10,20],"label":"sec-scatter"
                }]
            }]
        }
        """;
        var fig = S.FromJson(json);
        Assert.NotEmpty(fig.SubPlots[0].YAxes.Right);
        Assert.Single(fig.SubPlots[0].YAxes.Right[0].Series);
        Assert.IsType<MatPlotLibNet.Models.Series.ScatterSeries>(fig.SubPlots[0].YAxes.Right[0].Series[0]);
    }

    /// <summary>A line or scatter on either secondary axis with no xData/yData loads as an empty series of that kind,
    /// on that axis (LineSeries.FromSeriesDto / ScatterSeries.FromSeriesDto, reached through
    /// ChartSerializer.CreateOnSecondarySeat on both axes now, both <c>?? []</c> arms each), the same leniency the
    /// primary line and scatter have.</summary>
    [Theory]
    [InlineData("secondaryYAxis", "secondarySeries", "line")]
    [InlineData("secondaryYAxis", "secondarySeries", "scatter")]
    [InlineData("secondaryXAxis", "xSecondarySeries", "line")]
    [InlineData("secondaryXAxis", "xSecondarySeries", "scatter")]
    public void FromJson_SecondaryAxisSeriesWithNoXYData_LoadsAnEmptySeriesOnThatAxis(
        string axisField, string seriesField, string type)
    {
        string json = $$"""
            {"width":800,"height":600,"subPlots":[{
              "{{axisField}}":{"label":"other"},
              "{{seriesField}}":[{"type":"{{type}}","label":"empty"}]}]}
            """;

        var axes = S.FromJson(json).SubPlots[0];

        var list = axisField == "secondaryYAxis" ? axes.YAxes.Right[0].Series : axes.XSecondarySeries;
        var series = Assert.Single(list);
        Assert.Equal("empty", series.Label);
        var xy = Assert.IsAssignableFrom<MatPlotLibNet.Models.Series.XYSeries>(series);
        Assert.Equal(type == "line", xy is MatPlotLibNet.Models.Series.LineSeries);
        Assert.Empty(xy.XData);
        Assert.Empty(xy.YData);
    }

    public static TheoryData<string, Func<ISeries, object?>> SeriesWithAColorMapName => new()
    {
        {
            """
            {
              "width":800,"height":600,
              "subPlots":[{"series":[{
                "type":"contour3d",
                "xData":[0,1],"yData":[0,1],"zGridData":[[0,1],[1,0]],
                "colorMapName":"viridis"
              }]}]
            }
            """,
            series => Assert.IsType<MatPlotLibNet.Models.Series.Contour3DSeries>(series).ColorMap
        },
        {
            """
            {
              "width":800,"height":600,
              "subPlots":[{"series":[{
                "type":"trisurf",
                "xData":[0,1,2],"yData":[0,1,2],"zData":[0,1,4],
                "colorMapName":"plasma"
              }]}]
            }
            """,
            series => Assert.IsType<MatPlotLibNet.Models.Series.Trisurf3DSeries>(series).ColorMap
        },
    };

    [Theory]
    [MemberData(nameof(SeriesWithAColorMapName))]
    public void FromJson_ASeriesWithAColorMapName_SetsColorMap(string json, Func<ISeries, object?> colorMapOf)
    {
        var fig = S.FromJson(json);
        Assert.NotNull(colorMapOf(fig.SubPlots[0].Series[0]));
    }

    /// <summary>QuiverKey with no quiverKeyLabel field → QuiverKeySeries.FromSeriesDto
    /// <c>dto.QuiverKeyLabel ?? ""</c> TRUE (null) arm fires; the round-trip idiom always
    /// carries an explicit label, so only a raw/minimal JSON omitting it reaches this arm.</summary>
    [Fact]
    public void FromJson_QuiverKeyWithNoLabel_UsesDefaultEmptyLabel()
    {
        const string json = """{"width":800,"height":600,"subPlots":[{"series":[{"type":"quiverkey"}]}]}""";
        var fig = S.FromJson(json);
        var qk = Assert.IsType<MatPlotLibNet.Models.Series.QuiverKeySeries>(fig.SubPlots[0].Series[0]);
        Assert.Equal("", qk.Label);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        //  A series with no ColorGroup writes no field for it at all — the golden-corpus guarantee: a figure built before this field
        // existed writes the same bytes after.
        new DrawnToken("AnUngroupedSeries_WritesNoColorGroupField", () =>
        {
            var figure = Plt.Create().Plot([0.0, 1], [0.0, 1]).Build();
            return ChartSerializerDefault.Current.ToJson(figure);
        }) { Absent = ["colorGroup"], Comparison = StringComparison.OrdinalIgnoreCase },
        //  Verifies that ToJson includes a type discriminator for series.
        new DrawnToken("ToJson_IncludesSeriesTypeDiscriminator", () =>
        {
            var figure = Plt.Create()
                .Scatter([1.0], [2.0])
                .Build();
            return ChartSerializerDefault.Current.ToJson(figure);
        }, "\"type\"", "scatter"),
        //  Verifies that ToJson includes line series data and label.
        new DrawnToken("ToJson_IncludesLineSeries", () =>
        {
            var figure = Plt.Create()
                .Plot([1.0, 2.0], [3.0, 4.0], line =>
                {
                    line.Color = Colors.Red;
                    line.LineWidth = 2.5;
                    line.Label = "My Line";
                })
                .Build();
            return ChartSerializerDefault.Current.ToJson(figure);
        }, "line", "My Line"),
        //  Verifies that ToJson with indentation produces multi-line output.
        new DrawnToken("ToJson_Indented_IsReadable", () =>
        {
            var figure = Plt.Create().WithTitle("Test").Build();
            return ChartSerializerDefault.Current.ToJson(figure, indented: true);
        }, "\n"),
        //  Verifies that a figure without GridSpec omits the gridSpec field from JSON.
        new DrawnToken("RoundTrip_NoGridSpec_OmitsFromJson", () =>
        {
            var figure = Plt.Create().Plot([1.0], [2.0]).Build();
            return ChartSerializerDefault.Current.ToJson(figure);
        }) { Absent = ["gridSpec"] },
        //  Verifies that default spines are omitted from JSON.
        new DrawnToken("RoundTrip_DefaultSpines_OmitsFromJson", () =>
        {
            var figure = new Figure();
            figure.AddSubPlot().Plot([1.0], [2.0]);
            return ChartSerializerDefault.Current.ToJson(figure);
        }) { Absent = ["spines"] },
        //  Verifies that no sharing keys are emitted when not configured.
        new DrawnToken("RoundTrip_NoSharing_OmitsKeys", () =>
        {
            var figure = new Figure();
            figure.AddSubPlot().Plot([1.0], [2.0]);
            return ChartSerializerDefault.Current.ToJson(figure);
        }) { Absent = ["shareXKey", "shareYKey", "Subplot\""] },
        //  A partner with a key is named by it, not by its place; a partner that is not a subplot of the figure has no name the JSON can
        // write, so neither is written.
        new DrawnToken("AShare_IsWrittenByKeyWhenThePartnerHasOne_AndNotAtAllWhenItIsNotASubplot", () =>
        {
            var figure = new Figure();
            var keyed = figure.AddSubPlot(1, 3, 1);
            keyed.Key = "left";
            figure.AddSubPlot(1, 3, 2, sharex: keyed);
            figure.AddSubPlot(1, 3, 3, sharey: new Figure().AddSubPlot());
            return ChartSerializerDefault.Current.ToJson(figure);
        }, "shareXKey") { Absent = ["shareXSubplot", "shareYSubplot", "shareYKey"] },
        //  Verifies that no insets field is emitted when not configured.
        new DrawnToken("RoundTrip_NoInsets_OmitsFromJson", () =>
        {
            var figure = new Figure();
            figure.AddSubPlot().Plot([1.0], [2.0]);
            return ChartSerializerDefault.Current.ToJson(figure);
        }) { Absent = ["insets", "insetBounds"] },
        //  ToJson(indented: true) — true arm.
        new DrawnToken("ToJson_Indented_ContainsNewlines", () =>
        {
            var fig = Plt.Create().Plot([1.0], [1.0]).Build();
            return S.ToJson(fig, indented: true);
        }, "\n"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}

// ─── ChartSerializerCoverageTests.cs ─────────────────────────────────────────

