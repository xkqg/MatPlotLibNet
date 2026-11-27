// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.LegendRendering;

/// <summary>The legend drawn from a list of entries into a rectangle, without an axes renderer: the place it takes, the frame,
/// the title, each entry's swatch and text, and the hit-test bounds of each entry. The axes decides which series are entries
/// (<see cref="SeriesLegendEntry.RowsOf"/> and <see cref="PatchLegendEntry.Among"/>); this class only lays them out and draws them, so a figure-level legend can draw
/// with it too.</summary>
public class LegendRendererTests
{
    private static readonly Rect Area = new(80, 60, 400, 300);
    private static readonly double[] X = [0.0, 1.0, 2.0];
    private static readonly Legend Named = new() { Position = LegendPosition.UpperRight };

    private static LineSeries Line(Color? color = null) => new(X, X) { Label = "line", Color = color ?? Colors.Red };

    private static LegendEntry Entry(ISeries series, LegendSwatch swatch, string label = "entry", int index = 0) =>
        new SeriesLegendEntry(series, index, index, label, swatch);

    private static LegendEntry PatchEntry(string label, int index = 0) =>
        new PatchLegendEntry(new MatPlotLibNet.Models.Patches.Rectangle(new(0, 0), 1, 1) { Label = label }, label, index);

    private static LegendEntry LineEntry(string label = "entry", int index = 0) =>
        Entry(Line(), new LegendSwatch(LegendSwatchKind.Line), label, index);

    private static MarkObstacles NoMarks() => MarkObstacles.None;

    private static LegendLayout PlaceOrFail(LegendRenderer renderer, IReadOnlyList<LegendEntry> entries) =>
        renderer.Place(entries, Area, NoMarks) ?? throw new InvalidOperationException("the legend was not placed");

    /// <summary>Draws <paramref name="entries"/> on a recording context and returns what was drawn.</summary>
    private static RecordingRenderContext Drawn(Legend legend, IReadOnlyList<LegendEntry> entries, Theme? theme = null)
    {
        var ctx = new RecordingRenderContext();
        var renderer = new LegendRenderer(legend, theme ?? Theme.Default, ctx);
        renderer.Draw(entries, PlaceOrFail(renderer, entries), interactive: false);
        return ctx;
    }

    /// <summary>Everything drawn for the one entry of <paramref name="entry"/>: the calls past the frame, before the text.</summary>
    private static List<CallRecord> SwatchOf(LegendEntry entry, Legend? legend = null, Theme? theme = null)
    {
        var calls = Drawn((legend ?? Named) with { FrameOn = false }, [entry], theme).Calls;
        return [.. calls.Skip(1).TakeWhile(c => c is not (DrawTextCall or DrawRichTextCall))];
    }

    // ── Place ──────────────────────────────────────────────────────────────

    [Fact]
    public void Place_AHiddenLegend_IsNotPlaced_AndAsksForNoMarks()
    {
        int asked = 0;
        var renderer = new LegendRenderer(new Legend { Visible = false }, Theme.Default, new RecordingRenderContext());

        Assert.Null(renderer.Place([LineEntry()], Area, () => { asked++; return MarkObstacles.None; }));
        Assert.Equal(0, asked);
    }

    [Fact]
    public void Place_NoEntries_IsNotPlaced_AndAsksForNoMarks()
    {
        int asked = 0;
        var renderer = new LegendRenderer(new Legend(), Theme.Default, new RecordingRenderContext());

        Assert.Null(renderer.Place([], Area, () => { asked++; return MarkObstacles.None; }));
        Assert.Equal(0, asked);
    }

    [Fact]
    public void Place_ABestLegend_AsksForTheMarksOnce_AndKeepsClearOfThem()
    {
        int asked = 0;
        var legend = new Legend { Position = LegendPosition.Best };
        var renderer = new LegendRenderer(legend, Theme.Default, new SvgRenderContext());
        var free = PlaceOrFail(renderer, [LineEntry()]);

        var marks = new MarkObstacles.Builder();
        marks.AddBox(new Rect(free.BoxX, free.BoxY, free.BoxWidth, free.BoxHeight));
        var covered = renderer.Place([LineEntry()], Area, () => { asked++; return marks.Build(); })!;

        Assert.Equal(1, asked);
        Assert.NotEqual((free.BoxX, free.BoxY), (covered.BoxX, covered.BoxY));
    }

    [Fact]
    public void Place_ALegendThatNamesItsPlace_NeverAsksForTheMarks()
    {
        int asked = 0;
        var renderer = new LegendRenderer(Named, Theme.Default, new SvgRenderContext());

        Assert.NotNull(renderer.Place([LineEntry()], Area, () => { asked++; return MarkObstacles.None; }));
        Assert.Equal(0, asked);
    }

    /// <summary>A Best legend under a theme whose own place is named (the classic theme's upper right) does not look for room.</summary>
    [Fact]
    public void Place_ABestLegend_UnderATheme_ThatNamesThePlace_NeverAsksForTheMarks()
    {
        int asked = 0;
        var renderer = new LegendRenderer(new Legend(), Theme.MatplotlibClassic, new SvgRenderContext());

        Assert.NotNull(renderer.Place([LineEntry()], Area, () => { asked++; return MarkObstacles.None; }));
        Assert.Equal(0, asked);
    }

    [Theory]
    [InlineData(LegendPosition.UpperRight)]
    [InlineData(LegendPosition.UpperLeft)]
    [InlineData(LegendPosition.LowerLeft)]
    [InlineData(LegendPosition.LowerRight)]
    [InlineData(LegendPosition.Right)]
    [InlineData(LegendPosition.CenterLeft)]
    [InlineData(LegendPosition.CenterRight)]
    [InlineData(LegendPosition.LowerCenter)]
    [InlineData(LegendPosition.UpperCenter)]
    [InlineData(LegendPosition.Center)]
    [InlineData(LegendPosition.OutsideRight)]
    [InlineData(LegendPosition.OutsideLeft)]
    [InlineData(LegendPosition.OutsideTop)]
    [InlineData(LegendPosition.OutsideBottom)]
    public void Place_EveryPosition_IsTheCalculatorsLayoutInTheGivenRectangle(LegendPosition position)
    {
        var ctx = new SvgRenderContext();
        var legend = new Legend { Position = position };
        var expected = new LegendLayoutCalculator(Theme.Default, ctx).Compute(legend, ["first", "second"], Area);

        var placed = new LegendRenderer(legend, Theme.Default, ctx).Place([LineEntry("first"), LineEntry("second", 1)], Area, NoMarks)!;

        Assert.Equal((expected.BoxX, expected.BoxY, expected.BoxWidth, expected.BoxHeight),
            (placed.BoxX, placed.BoxY, placed.BoxWidth, placed.BoxHeight));
    }

    [Fact]
    public void Place_ALongLabel_WidensTheBox()
    {
        var renderer = new LegendRenderer(Named, Theme.Default, new SvgRenderContext());

        var shortBox = PlaceOrFail(renderer, [LineEntry("a")]);
        var longBox = PlaceOrFail(renderer, [LineEntry("a label much longer than the first one")]);

        Assert.True(longBox.BoxWidth > shortBox.BoxWidth, $"{longBox.BoxWidth} is not wider than {shortBox.BoxWidth}");
        Assert.Equal(shortBox.BoxHeight, longBox.BoxHeight);
    }

    // ── The group, the frame and the title ─────────────────────────────────

    [Fact]
    public void Draw_OnSvg_OpensTheAccessibleLegendGroup_AndClosesIt()
    {
        var ctx = new SvgRenderContext();
        var renderer = new LegendRenderer(Named, Theme.Default, ctx);
        renderer.Draw([LineEntry()], PlaceOrFail(renderer, [LineEntry()]), interactive: false);

        string svg = ctx.GetOutput();
        Assert.StartsWith("<g class=\"legend\" aria-label=\"Chart legend\">", svg);
        Assert.EndsWith("</g>\n", svg);
    }

    [Fact]
    public void Draw_OnAnotherSurface_OpensAPlainLegendGroup_AndClosesIt()
    {
        var calls = Drawn(Named, [LineEntry()]).Calls;

        Assert.Equal("legend", Assert.IsType<BeginGroupCall>(calls[0]).CssClass);
        Assert.IsType<EndGroupCall>(calls[^1]);
        Assert.Equal(1, calls.Count(c => c is BeginGroupCall));
    }

    [Fact]
    public void Draw_TheFrame_FillsTheBoxInTheThemeBackgroundAtTheFrameAlpha_OutlinedLightGrey()
    {
        var ctx = new RecordingRenderContext();
        var renderer = new LegendRenderer(Named, Theme.Default, ctx);
        var layout = PlaceOrFail(renderer, [LineEntry()]);
        renderer.Draw([LineEntry()], layout, interactive: false);

        var frame = ctx.Of<DrawRectangleCall>()[0];
        Assert.Equal(new Rect(layout.BoxX, layout.BoxY, layout.BoxWidth, layout.BoxHeight), frame.Rect);
        Assert.Equal(Theme.Default.Background.WithAlpha(204), frame.Fill);
        Assert.Equal(Color.FromHex("#CCCCCC"), frame.Stroke);
        Assert.Equal(0.5, frame.StrokeThickness);
    }

    [Fact]
    public void Draw_TheFrame_TakesTheFaceAndEdgeColoursTheLegendNames()
    {
        var rects = Drawn(Named with { FaceColor = Colors.Yellow, EdgeColor = Colors.Blue, FrameAlpha = 1.0 }, [LineEntry()])
            .Of<DrawRectangleCall>();

        Assert.Equal(Colors.Yellow, rects[0].Fill);
        Assert.Equal(Colors.Blue, rects[0].Stroke);
    }

    [Fact]
    public void Draw_AShadow_IsADarkerBoxThreePixelsDownAndRight_DrawnBeforeTheFrame()
    {
        var ctx = new RecordingRenderContext();
        var renderer = new LegendRenderer(Named with { Shadow = true }, Theme.Default, ctx);
        var layout = PlaceOrFail(renderer, [LineEntry()]);
        renderer.Draw([LineEntry()], layout, interactive: false);

        var rects = ctx.Of<DrawRectangleCall>();
        Assert.Equal(new Rect(layout.BoxX + 3, layout.BoxY + 3, layout.BoxWidth, layout.BoxHeight), rects[0].Rect);
        Assert.Equal(new Color(0, 0, 0, 80), rects[0].Fill);
        Assert.Equal(new Rect(layout.BoxX, layout.BoxY, layout.BoxWidth, layout.BoxHeight), rects[1].Rect);
    }

    [Fact]
    public void Draw_FrameOff_DrawsNoBox_AndNoShadow()
    {
        Assert.Empty(Drawn(Named with { FrameOn = false, Shadow = true }, [LineEntry()]).Of<DrawRectangleCall>());
    }

    [Fact]
    public void Draw_ATitle_IsCentredOverTheEntries_InTheTitleFont()
    {
        var ctx = new RecordingRenderContext();
        var renderer = new LegendRenderer(Named with { Title = "Legend" }, Theme.Default, ctx);
        var layout = PlaceOrFail(renderer, [LineEntry()]);
        renderer.Draw([LineEntry()], layout, interactive: false);

        var title = ctx.Of<DrawTextCall>()[0];
        Assert.Equal("Legend", title.Text);
        Assert.Equal(new Point(layout.BoxX + layout.Padding + layout.TotalContentWidth / 2, layout.BoxY + layout.Padding + layout.TitleFont.Size),
            title.Position);
        Assert.Equal(layout.TitleFont, title.Font);
        Assert.Equal(TextAlignment.Center, title.Alignment);
    }

    [Fact]
    public void Draw_NoTitle_DrawsOnlyTheEntryTexts()
    {
        var texts = Drawn(Named, [LineEntry("one"), LineEntry("two", 1)]).Of<DrawTextCall>();

        Assert.Equal(["one", "two"], texts.Select(t => t.Text));
    }

    // ── The entries ────────────────────────────────────────────────────────

    [Fact]
    public void Draw_AnEntryText_StandsPastItsSwatch_LeftAligned_InTheLegendFont()
    {
        var ctx = new RecordingRenderContext();
        var renderer = new LegendRenderer(Named, Theme.Default, ctx);
        var layout = PlaceOrFail(renderer, [LineEntry("one")]);
        renderer.Draw([LineEntry("one")], layout, interactive: false);

        var text = ctx.Of<DrawTextCall>()[0];
        Assert.Equal(new Point(layout.ColumnX(0) + layout.SwatchWidth + layout.SwatchGap, layout.EntryY(0) + layout.SwatchHeight - 1),
            text.Position);
        Assert.Equal(layout.Font, text.Font);
        Assert.Equal(TextAlignment.Left, text.Alignment);
    }

    [Fact]
    public void Draw_AMathLabel_IsDrawnAsTheRunsOfItsLayout()
    {
        var ctx = Drawn(Named, [LineEntry(@"$\alpha$ decay")]);

        Assert.Empty(ctx.Of<DrawRichTextCall>());
        Assert.Equal(["α", " decay"], ctx.Of<DrawTextCall>().Select(c => c.Text));
    }

    [Fact]
    public void Draw_InColumns_FillsRowByRow_AndReturnsEachEntrysBounds()
    {
        var entries = new[] { LineEntry("a", 0), LineEntry("b", 1), LineEntry("c", 2) };
        var ctx = new RecordingRenderContext();
        var renderer = new LegendRenderer(Named with { NCols = 2 }, Theme.Default, ctx);
        var layout = PlaceOrFail(renderer, entries);

        var bounds = renderer.Draw(entries, layout, interactive: false);

        Assert.Equal(
        [
            new LegendItemBounds(0, new Rect(layout.ColumnX(0), layout.EntryY(0), layout.ItemWidth(0), layout.LineHeight)),
            new LegendItemBounds(1, new Rect(layout.ColumnX(1), layout.EntryY(0), layout.ItemWidth(1), layout.LineHeight)),
            new LegendItemBounds(2, new Rect(layout.ColumnX(0), layout.EntryY(1), layout.ItemWidth(0), layout.LineHeight)),
        ], bounds);
        var texts = ctx.Of<DrawTextCall>().Select(t => t.Position).ToArray();
        Assert.Equal(texts[0].Y, texts[1].Y);
        Assert.True(texts[1].X > texts[0].X);
        Assert.Equal(texts[0].X, texts[2].X);
        Assert.True(texts[2].Y > texts[0].Y);
    }

    [Fact]
    public void ItemBounds_AreTheBoundsTheDrawingReturns_WithTheSeriesIndexOfEachEntry()
    {
        var entries = new[] { LineEntry("a", 3), LineEntry("b", 7) };
        var renderer = new LegendRenderer(Named, Theme.Default, new RecordingRenderContext());
        var layout = PlaceOrFail(renderer, entries);

        var hitTest = LegendRenderer.ItemBounds(entries, layout);

        Assert.Equal(renderer.Draw(entries, layout, interactive: false), hitTest);
        Assert.Equal([3, 7], hitTest.Select(b => b.SeriesIndex));
    }

    /// <summary>A patch's row has its hit-test box like a series' row, carrying the index the patch is toggled by.</summary>
    [Fact]
    public void ItemBounds_GiveAPatchRowItsCell_WithItsIndex()
    {
        var entries = new[] { LineEntry("a", 3), PatchEntry("patch", 9), LineEntry("b", 7) };
        var renderer = new LegendRenderer(Named, Theme.Default, new RecordingRenderContext());
        var layout = PlaceOrFail(renderer, entries);

        var hitTest = LegendRenderer.ItemBounds(entries, layout);

        Assert.Equal(
        [
            new LegendItemBounds(3, new Rect(layout.ColumnX(0), layout.EntryY(0), layout.ItemWidth(0), layout.LineHeight)),
            new LegendItemBounds(9, new Rect(layout.ColumnX(0), layout.EntryY(1), layout.ItemWidth(0), layout.LineHeight)),
            new LegendItemBounds(7, new Rect(layout.ColumnX(0), layout.EntryY(2), layout.ItemWidth(0), layout.LineHeight)),
        ], hitTest);
        Assert.Equal(renderer.Draw(entries, layout, interactive: false), hitTest);
    }

    [Theory]
    [InlineData(true, "patch", 4)]
    [InlineData(false, "second", 5)]
    public void Draw_Interactive_OnSvg_WrapsEachRowInAGroupCarryingItsSeriesIndex(bool secondIsAPatch, string label, int index)
    {
        var entries = new[] { LineEntry("first", 2), secondIsAPatch ? PatchEntry(label, index) : LineEntry(label, index) };
        var ctx = new SvgRenderContext();
        var renderer = new LegendRenderer(Named, Theme.Default, ctx);
        renderer.Draw(entries, PlaceOrFail(renderer, entries), interactive: true);

        string svg = ctx.GetOutput();
        Assert.Contains("<g data-legend-index=\"2\" style=\"cursor:pointer\" aria-label=\"first\">", svg);
        Assert.Contains($"<g data-legend-index=\"{index}\" style=\"cursor:pointer\" aria-label=\"{label}\">", svg);
        Assert.Equal(3, svg.Split("</g>").Length - 1);
    }

    [Fact]
    public void ItemBounds_NoEntries_IsEmpty()
    {
        var layout = new LegendLayoutCalculator(Theme.Default, new SvgRenderContext()).Compute(Named, [], Area);

        Assert.Empty(LegendRenderer.ItemBounds([], layout));
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Draw_NotInteractive_OnSvg_WritesNoEntryGroups", () =>
        {
            var ctx = new SvgRenderContext();
            var renderer = new LegendRenderer(Named, Theme.Default, ctx);
            renderer.Draw([LineEntry()], PlaceOrFail(renderer, [LineEntry()]), interactive: false);
            return ctx.GetOutput();
        }) { Absent = ["data-legend-index"] },
        // With Marker=None the scatter draws nothing at the data points.
        // Tick marks / legend bullets / axis marks don't emit <circle> in this config,
        // so the absence of <circle> confirms the None branch was honoured.
        new DrawnToken("Scatter_MarkerNone_EmitsNoCircles", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Scatter([1.0, 2.0, 3.0], [1.0, 2.0, 3.0], s =>
            {
                s.Marker = MarkerStyle.None;
                s.MarkerSize = 36;
            }))
            .ToSvg()) { Absent = ["<circle"] },
        // Line itself renders as <polyline>; no <circle> / <rect> / <polygon> for markers.
        new DrawnToken("LineChart_MarkerNone_DrawsLineOnly_NoMarkerPrimitives", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0, 3.0], [1.0, 2.0, 3.0]))
            .ToSvg(), "<polyline"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void Draw_Interactive_OnAnotherSurface_OpensNoEntryGroups()
    {
        var ctx = new RecordingRenderContext();
        var renderer = new LegendRenderer(Named, Theme.Default, ctx);
        renderer.Draw([LineEntry(), LineEntry("b", 1)], PlaceOrFail(renderer, [LineEntry(), LineEntry("b", 1)]), interactive: true);

        Assert.Equal(1, ctx.Count<BeginGroupCall>());
        Assert.Equal(1, ctx.Count<EndGroupCall>());
    }

    /// <summary>The renderer draws exactly the entries it is handed. Which series are entries is the axes' rule: a series
    /// whose name is drawn at the end of its line is left out where end labels are drawn, kept where none are.</summary>
    [Theory]
    [InlineData(true, new[] { "kept" })]
    [InlineData(false, new[] { "end", "kept" })]
    public void Draw_TheEntriesTheAxesHandsOver_ADirectlyLabelledSeriesLeftOutWhereEndLabelsAreDrawn(bool endLabelsDrawn, string[] expected)
    {
        var axes = new Axes();
        axes.Plot(X, X).Label = "end";
        ((LineSeries)axes.Series[0]).LabelPlacement = SeriesLabelPlacement.LineEnd;
        axes.Plot(X, X).Label = "kept";

        var texts = Drawn(Named, axes.LegendEntries(Theme.Default, endLabelsDrawn)).Of<DrawTextCall>();

        Assert.Equal(expected, texts.Select(t => t.Text));
    }

    // ── The swatches ───────────────────────────────────────────────────────

    private static (double X, double Y, double W, double H) SwatchBox(LegendEntry entry)
    {
        var renderer = new LegendRenderer(Named, Theme.Default, new RecordingRenderContext());
        var layout = PlaceOrFail(renderer, [entry]);
        return (layout.ColumnX(0), layout.EntryY(0), layout.SwatchWidth, layout.SwatchHeight);
    }

    [Fact]
    public void Swatch_Line_IsAHorizontalLineAcrossTheSwatch_InTheSeriesStroke()
    {
        var entry = Entry(Line(), new LegendSwatch(LegendSwatchKind.Line, new GivenStroke(3, LineStyle.Dashed)));
        var (x, y, w, h) = SwatchBox(entry);

        var line = Assert.IsType<DrawLineCall>(Assert.Single(SwatchOf(entry)));
        Assert.Equal(new Point(x, y + h / 2), line.P1);
        Assert.Equal(new Point(x + w, y + h / 2), line.P2);
        Assert.Equal(Colors.Red, line.Color);
        Assert.Equal(3.0, line.Thickness);
        Assert.Equal(LineStyle.Dashed, line.Style);
    }

    [Fact]
    public void Swatch_Line_ThatIsLineStyleNone_DrawsNothing()
    {
        Assert.Empty(SwatchOf(Entry(Line(), new LegendSwatch(LegendSwatchKind.Line, new GivenStroke(null, LineStyle.None)))));
    }

    [Fact]
    public void Swatch_Line_WearsTheColourOfItsColourSlot_WhenTheSeriesNamesNone()
    {
        var entry = new SeriesLegendEntry(new LineSeries(X, X) { Label = "line" }, 0, 2, "entry", new LegendSwatch(LegendSwatchKind.Line));

        Assert.Equal(Theme.Default.CycleColors[2], Assert.IsType<DrawLineCall>(Assert.Single(SwatchOf(entry))).Color);
    }

    [Fact]
    public void Swatch_LineMarker_IsTheLineWithTheMarkerAtItsMiddle()
    {
        var entry = Entry(Line(), new LegendSwatch(LegendSwatchKind.LineMarker, Marker: MarkerStyle.Circle, MarkerSize: 6));
        var (x, y, w, h) = SwatchBox(entry);

        var calls = SwatchOf(entry);
        Assert.Equal(["DrawLine", "DrawCircle"], calls.Select(c => c.Kind));
        var marker = Assert.IsType<DrawCircleCall>(calls[1]);
        Assert.Equal(new Point(x + w / 2, y + h / 2), marker.Center);
        Assert.Equal(6 * (100.0 / 72.0) / 2, marker.Radius, 9);   // 6 points across
        Assert.Equal(Colors.Red, marker.Fill);
    }

    [Fact]
    public void Swatch_LineMarker_ATinyMarker_IsDrawnOnePointAcross()
    {
        var entry = Entry(Line(), new LegendSwatch(LegendSwatchKind.LineMarker, Marker: MarkerStyle.Circle, MarkerSize: 0));

        Assert.Equal(1 * (100.0 / 72.0) / 2, Assert.IsType<DrawCircleCall>(SwatchOf(entry)[1]).Radius, 9);
    }

    /// <summary>A marker in a handle is the size its series draws it at, at least a point and no more than the handle is high, as a
    /// scatter's and a 3-D scatter's marker is: the marker of a line follows the same rule (a line whose marker is larger than the
    /// handle once drew a marker that stood out of its box).</summary>
    [Fact]
    public void Swatch_LineMarker_ALargeMarker_IsNoBiggerThanTheSwatchIsHigh()
    {
        var entry = Entry(Line(), new LegendSwatch(LegendSwatchKind.LineMarker, Marker: MarkerStyle.Circle, MarkerSize: 10000));
        var (x, y, w, h) = SwatchBox(entry);

        var marker = Assert.IsType<DrawCircleCall>(SwatchOf(entry)[1]);
        Assert.Equal(new Point(x + w / 2, y + h / 2), marker.Center);
        Assert.Equal(h / 2, marker.Radius, 9);
    }

    [Theory]
    [InlineData("Marker")]
    [InlineData("Scatter")]
    [InlineData("Scatter3D")]
    public void Swatch_OfAScatter_ATinyMarker_IsOnePointAcross_AndAHugeOne_NoBiggerThanTheSwatchIsHigh(string kindName)
    {
        var kind = Enum.Parse<LegendSwatchKind>(kindName);
        var scatter = new ScatterSeries(X, X) { Label = "s", Color = Colors.Red };
        var tiny = SwatchOf(Entry(scatter, new LegendSwatch(kind, Marker: MarkerStyle.Circle, MarkerSize: 0.001)));
        var entry = Entry(scatter, new LegendSwatch(kind, Marker: MarkerStyle.Circle, MarkerSize: 10000));
        var (_, _, _, h) = SwatchBox(entry);

        Assert.Equal(1 * (100.0 / 72.0) / 2, Assert.IsType<DrawCircleCall>(Assert.Single(tiny)).Radius, 9);
        Assert.Equal(h / 2, Assert.IsType<DrawCircleCall>(Assert.Single(SwatchOf(entry))).Radius, 9);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(MarkerStyle.None)]
    public void Swatch_LineMarker_WithoutAMarker_IsOnlyTheLine(MarkerStyle? marker)
    {
        var entry = Entry(Line(), new LegendSwatch(LegendSwatchKind.LineMarker, Marker: marker));

        Assert.Equal(["DrawLine"], SwatchOf(entry).Select(c => c.Kind));
    }

    [Fact]
    public void Swatch_LineMarker_WithoutAMarker_TakesTheCyclersMarker()
    {
        var theme = Theme.CreateFrom(Theme.Default).WithPropCycler(new PropCyclerBuilder()
            .WithColors(Colors.Red).WithMarkerStyles(MarkerStyle.Square).Build()).Build();
        var entry = Entry(Line(), new LegendSwatch(LegendSwatchKind.LineMarker));

        Assert.Equal(["DrawLine", "DrawRectangle"], SwatchOf(entry, theme: theme).Select(c => c.Kind));
    }

    [Fact]
    public void Swatch_SolidLine_IsSolidWhateverStyleTheSeriesHolds()
    {
        var entry = Entry(Line(), new LegendSwatch(LegendSwatchKind.SolidLine, new GivenStroke(2, LineStyle.Dashed)));

        var line = Assert.IsType<DrawLineCall>(Assert.Single(SwatchOf(entry)));
        Assert.Equal(LineStyle.Solid, line.Style);
        Assert.Equal(2.0, line.Thickness);
    }

    [Fact]
    public void Swatch_Marker_IsOneMarkerAtTheSwatchsLeft_NoBiggerThanTheSwatchIsHigh()
    {
        var entry = Entry(new ScatterSeries(X, X) { Label = "s", Color = Colors.Red },
            new LegendSwatch(LegendSwatchKind.Marker, MarkerSize: 10000));
        var (x, y, _, h) = SwatchBox(entry);

        var circle = Assert.IsType<DrawCircleCall>(Assert.Single(SwatchOf(entry)));
        Assert.Equal(new Point(x + h / 2, y + h / 2), circle.Center);
        Assert.Equal(h / 2, circle.Radius, 9);
    }

    [Fact]
    public void Swatch_Marker_NamingNone_IsTheRestingCircle_AndOneNamed_IsThatShape()
    {
        var resting = Entry(new ScatterSeries(X, X) { Color = Colors.Red }, new LegendSwatch(LegendSwatchKind.Marker, MarkerSize: 6));
        var square = Entry(new ScatterSeries(X, X) { Color = Colors.Red },
            new LegendSwatch(LegendSwatchKind.Marker, Marker: MarkerStyle.Square, MarkerSize: 6));

        Assert.IsType<DrawCircleCall>(Assert.Single(SwatchOf(resting)));
        Assert.IsType<DrawRectangleCall>(Assert.Single(SwatchOf(square)));
    }

    [Fact]
    public void Swatch_ErrorBar_IsALineWithACapAtEachEnd()
    {
        var entry = Entry(Line(), new LegendSwatch(LegendSwatchKind.ErrorBar, new GivenStroke(2, null)));
        var (x, y, w, h) = SwatchBox(entry);
        double mid = y + h / 2;

        var lines = SwatchOf(entry).Select(c => Assert.IsType<DrawLineCall>(c)).ToList();
        Assert.Equal(3, lines.Count);
        Assert.Equal((new Point(x + 2, mid), new Point(x + w - 2, mid)), (lines[0].P1, lines[0].P2));
        Assert.Equal((new Point(x + 2, mid - 3), new Point(x + 2, mid + 3)), (lines[1].P1, lines[1].P2));
        Assert.Equal((new Point(x + w - 2, mid - 3), new Point(x + w - 2, mid + 3)), (lines[2].P1, lines[2].P2));
        Assert.All(lines, l => Assert.Equal(2.0, l.Thickness));
    }

    [Fact]
    public void Swatch_Stripes_AreOneStripePerDistinctState_SideBySide()
    {
        StateSegment[] states = [new(0, 1, "up", Colors.Green), new(1, 2, "down", Colors.Red), new(2, 3, "up", Colors.Green)];
        var entry = Entry(Line(), new LegendSwatch(LegendSwatchKind.Stripes, States: states));
        var (x, y, w, h) = SwatchBox(entry);

        var stripes = SwatchOf(entry).Select(c => Assert.IsType<DrawRectangleCall>(c)).ToList();
        Assert.Equal(2, stripes.Count);
        Assert.Equal(new Rect(x, y, w / 2, h), stripes[0].Rect);
        Assert.Equal(new Rect(x + w / 2, y, w / 2, h), stripes[1].Rect);
        Assert.Equal([Colors.Green, Colors.Red], stripes.Select(s => s.Fill));
    }

    [Fact]
    public void Swatch_Stripes_WithNoStateToShow_IsAFilledRectangle()
    {
        var entry = Entry(Line(), new LegendSwatch(LegendSwatchKind.Stripes));
        var (x, y, w, h) = SwatchBox(entry);

        var patch = Assert.IsType<DrawRectangleCall>(Assert.Single(SwatchOf(entry)));
        Assert.Equal(new Rect(x, y, w, h), patch.Rect);
        Assert.Equal(Colors.Red, patch.Fill);
    }

    /// <summary>An edged handle reads its edge from the patch its row stands for (here a bar given a red edge 2 wide, under classic,
    /// which edges every patch); a row whose series is no such patch has no edge to read and is drawn as a plain rectangle.</summary>
    [Fact]
    public void AnEdgedHandle_ReadsItsEdgeFromThePatchItStandsFor()
    {
        var bar = new BarSeries(["a"], [1.0]) { Label = "bars", Color = Colors.Blue, EdgeColor = Colors.Red, LineWidth = 2 };

        var edged = Assert.IsType<DrawRectangleCall>(Assert.Single(SwatchOf(Entry(bar, LegendSwatch.EdgedPatch), theme: Theme.MatplotlibClassic)));
        var plain = Assert.IsType<DrawRectangleCall>(Assert.Single(SwatchOf(Entry(Line(), LegendSwatch.EdgedPatch), theme: Theme.MatplotlibClassic)));

        Assert.Equal(Colors.Red, edged.Stroke);
        Assert.Equal(2, edged.StrokeThickness, 9);
        Assert.Null(plain.Stroke);
    }

    [Fact]
    public void Swatch_Patch_IsAFilledRectangle_TheFullSwatchLongAndHigh()
    {
        var entry = Entry(Line(), LegendSwatch.Patch);
        var (x, y, w, h) = SwatchBox(entry);

        var patch = Assert.IsType<DrawRectangleCall>(Assert.Single(SwatchOf(entry)));
        Assert.Equal(new Rect(x, y, w, h), patch.Rect);
        Assert.Equal(Colors.Red, patch.Fill);
        Assert.Null(patch.Stroke);
    }

    // ---- the marker a legend entry draws (DrawMarker) ----
    // Phase M.2 — the <c>MarkerStyle</c> enum has 13 members (Circle, Square, Triangle, Diamond, Cross, Plus, Star, Pentagon,
    // Hexagon, TriangleDown/Left/Right, plus None). Pre-Phase-M, <c>LineSeriesRenderer</c> drew every marker as a circle and
    // <c>ScatterSeriesRenderer</c> honoured only Square, silently collapsing every other shape to a circle. These tests pin the
    // correct SVG primitive for each shape end-to-end via <c>Plt.Create()</c>.

    // Scatter — each marker shape maps to a specific SVG primitive.
    [Theory]
    [InlineData(MarkerStyle.Circle,        "<circle")]
    [InlineData(MarkerStyle.Square,        "<rect")]
    [InlineData(MarkerStyle.Triangle,      "<polygon")]
    [InlineData(MarkerStyle.TriangleDown,  "<polygon")]
    [InlineData(MarkerStyle.TriangleLeft,  "<polygon")]
    [InlineData(MarkerStyle.TriangleRight, "<polygon")]
    [InlineData(MarkerStyle.Diamond,       "<polygon")]
    [InlineData(MarkerStyle.Pentagon,      "<polygon")]
    [InlineData(MarkerStyle.Hexagon,       "<polygon")]
    [InlineData(MarkerStyle.Star,          "<polygon")]
    public void Scatter_Marker_EmitsCorrectSvgPrimitive(MarkerStyle style, string expected)
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Scatter([1.0, 2.0, 3.0], [1.0, 2.0, 3.0], s =>
            {
                s.Marker = style;
                s.MarkerSize = 36;
            }))
            .ToSvg();
        Assert.Contains(expected, svg);
    }

    // Cross and Plus are outline-only "X" / "+" markers — matplotlib parity.
    [Theory]
    [InlineData(MarkerStyle.Cross)]
    [InlineData(MarkerStyle.Plus)]
    public void Scatter_CrossOrPlus_EmitsLineElements(MarkerStyle style)
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Scatter([1.0, 2.0, 3.0], [1.0, 2.0, 3.0], s =>
            {
                s.Marker = style;
                s.MarkerSize = 36;
            }))
            .ToSvg();
        // Cross / Plus each render as 2 line segments per point → expect <line at minimum.
        Assert.Contains("<line", svg);
    }

    /// <summary>The markup of one marker drawn straight onto an SVG surface at (50, 50), filled and stroked in the hex colours given.</summary>
    private static string Marker(MarkerStyle style, double size, string? fill = "#FF0000", string? stroke = null, double width = 0)
    {
        var svg = new SvgRenderContext();
        LegendRenderer.DrawMarker(svg, style, new Point(50, 50), size,
            new ShapeStyle(fill is null ? null : Color.FromHex(fill), stroke is null ? null : Color.FromHex(stroke), width), Theme.Default);
        return svg.GetOutput();
    }

    /// <summary>A cross and a plus are drawn as lines, in their stroke colour, their fill or black, at the width given or one that grows
    /// with the size; every other filled shape is a polygon.</summary>
    [Theory]
    [InlineData(MarkerStyle.Cross, 10, null, null, 0, "<line")]
    [InlineData(MarkerStyle.Plus, 10, null, null, 0, "<line")]
    [InlineData(MarkerStyle.Cross, 16, "#FF0000", null, 0, "<line")]
    [InlineData(MarkerStyle.Cross, 12, null, "#FF0000", 1.5, "<line")]
    [InlineData(MarkerStyle.Plus, 12, null, "#0000FF", 1.5, "<line")]
    [InlineData(MarkerStyle.Pentagon, 12, "#FF0000", null, 0, "<polygon")]
    [InlineData(MarkerStyle.Hexagon, 12, "#0000FF", null, 0, "<polygon")]
    [InlineData(MarkerStyle.Star, 12, "#008000", null, 0, "<polygon")]
    [InlineData(MarkerStyle.TriangleLeft, 12, "#00FFFF", null, 0, "<polygon")]
    [InlineData(MarkerStyle.TriangleRight, 12, "#FF00FF", null, 0, "<polygon")]
    [InlineData(MarkerStyle.TriangleDown, 12, "#FFFF00", null, 0, "<polygon")]
    [InlineData(MarkerStyle.Diamond, 12, "#000000", null, 0, "<polygon")]
    public void AMarker_IsDrawnAsItsShape(MarkerStyle style, double size, string? fill, string? stroke, double width, string element) =>
        Assert.Contains(element, Marker(style, size, fill, stroke, width));

    /// <summary>A marker of no style, or of no size, draws nothing: the surface is as it was.</summary>
    [Theory]
    [InlineData(MarkerStyle.None, 12)]
    [InlineData(MarkerStyle.Circle, 0)]
    [InlineData(MarkerStyle.Circle, -5)]
    public void AMarkerOfNoStyleOrNoSize_DrawsNothing(MarkerStyle style, double size) =>
        Assert.Equal(new SvgRenderContext().GetOutput(), Marker(style, size));

    // Line charts — markers apply per-vertex. Pre-fix every choice collapsed to circle.
    [Theory]
    [InlineData(MarkerStyle.Square,   "<rect")]
    [InlineData(MarkerStyle.Triangle, "<polygon")]
    [InlineData(MarkerStyle.Diamond,  "<polygon")]
    [InlineData(MarkerStyle.Star,     "<polygon")]
    public void LineChart_Marker_EmitsCorrectSvgPrimitive(MarkerStyle style, string expected)
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0, 3.0], [1.0, 2.0, 3.0], s =>
            {
                s.Marker = style;
                s.MarkerSize = 10;
            }))
            .ToSvg();
        Assert.Contains(expected, svg);
    }

    [Fact]
    public void LineChart_CrossMarker_EmitsLineElements()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0, 3.0], [1.0, 2.0, 3.0], s =>
            {
                s.Marker = MarkerStyle.Cross;
                s.MarkerSize = 10;
            }))
            .ToSvg();
        // The line polyline itself may not emit <line> (it's <polyline>), but the
        // Cross markers at each vertex must. Lines aren't already in the axis ticks
        // for every chart, so a plain count check suffices — a Cross per 3 vertices
        // is 6 line segments minimum.
        int lineCount = svg.Elements("line").Count;
        Assert.True(lineCount >= 6, $"expected ≥6 <line> for 3 Cross markers (2 each); got {lineCount}");
    }

    /// <summary>Exhaustiveness guard: every <see cref="MarkerStyle"/> value the enum currently declares must
    /// draw without throwing. <c>DrawMarker</c>'s polygon switch ends in a default arm that throws for an
    /// unhandled style, on purpose — a future shape added to the enum and forgotten here fails THIS test
    /// (loudly, at the call site) instead of silently drawing nothing. <see cref="MarkerStyle.None"/> is
    /// itself a no-op by design (size/visibility gate), so it is covered too, trivially.</summary>
    [Fact]
    public void DrawMarker_HandlesEveryDeclaredMarkerStyle()
    {
        var ctx = new SvgRenderContext();
        var shape = new ShapeStyle(Colors.Blue, Colors.Black, 1);

        foreach (MarkerStyle style in Enum.GetValues<MarkerStyle>())
        {
            var ex = Record.Exception(() => LegendRenderer.DrawMarker(ctx, style, new Point(10, 10), 8, shape, Theme.Default));
            Assert.True(ex is null, $"MarkerStyle.{style} threw: {ex}");
        }
    }

    /// <summary>The other half of the guard above: an undeclared value DOES throw — the default arm this
    /// exhaustiveness test exists to exercise, which no valid enum member ever reaches.</summary>
    [Fact]
    public void DrawMarker_UndeclaredStyle_ThrowsNamingTheStyle()
    {
        var ctx = new SvgRenderContext();
        var shape = new ShapeStyle(Colors.Blue, Colors.Black, 1);
        var undeclared = (MarkerStyle)999;

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => LegendRenderer.DrawMarker(ctx, undeclared, new Point(10, 10), 8, shape, Theme.Default));

        Assert.Contains("999", ex.Message);
    }
}
