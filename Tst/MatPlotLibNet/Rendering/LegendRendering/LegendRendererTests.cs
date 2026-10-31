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
/// (<see cref="AxesLegendEntryExtensions"/>); this class only lays them out and draws them, so a figure-level legend can draw
/// with it too.</summary>
public class LegendRendererTests
{
    private static readonly Rect Area = new(80, 60, 400, 300);
    private static readonly double[] X = [0.0, 1.0, 2.0];
    private static readonly Legend Named = new() { Position = LegendPosition.UpperRight };

    private static LineSeries Line(Color? color = null) => new(X, X) { Label = "line", Color = color ?? Colors.Red };

    private static LegendEntry Entry(ISeries series, LegendSwatch swatch, string label = "entry", int index = 0) =>
        new(series, index, index, label, swatch);

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
        return [.. calls.Skip(1).TakeWhile(c => c.Kind is not ("DrawText" or "DrawRichText"))];
    }

    private static dynamic Args(CallRecord call) => call.Args!;

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
        Assert.EndsWith("</g>" + Environment.NewLine, svg);
    }

    [Fact]
    public void Draw_OnAnotherSurface_OpensAPlainLegendGroup_AndClosesIt()
    {
        var calls = Drawn(Named, [LineEntry()]).Calls;

        Assert.Equal("BeginGroup", calls[0].Kind);
        Assert.Equal("legend", (string)Args(calls[0]).cssClass);
        Assert.Equal("EndGroup", calls[^1].Kind);
        Assert.Equal(1, calls.Count(c => c.Kind == "BeginGroup"));
    }

    [Fact]
    public void Draw_TheFrame_FillsTheBoxInTheThemeBackgroundAtTheFrameAlpha_OutlinedLightGrey()
    {
        var ctx = new RecordingRenderContext();
        var renderer = new LegendRenderer(Named, Theme.Default, ctx);
        var layout = PlaceOrFail(renderer, [LineEntry()]);
        renderer.Draw([LineEntry()], layout, interactive: false);

        var frame = ctx.OfKind("DrawRectangle")[0];
        Assert.Equal(new Rect(layout.BoxX, layout.BoxY, layout.BoxWidth, layout.BoxHeight), (Rect)Args(frame).rect);
        Assert.Equal(Theme.Default.Background.WithAlpha(204), (Color?)Args(frame).fill);
        Assert.Equal(Color.FromHex("#CCCCCC"), (Color?)Args(frame).stroke);
        Assert.Equal(0.5, (double)Args(frame).strokeThickness);
    }

    [Fact]
    public void Draw_TheFrame_TakesTheFaceAndEdgeColoursTheLegendNames()
    {
        var rects = Drawn(Named with { FaceColor = Colors.Yellow, EdgeColor = Colors.Blue, FrameAlpha = 1.0 }, [LineEntry()])
            .OfKind("DrawRectangle");

        Assert.Equal(Colors.Yellow, (Color?)Args(rects[0]).fill);
        Assert.Equal(Colors.Blue, (Color?)Args(rects[0]).stroke);
    }

    [Fact]
    public void Draw_AShadow_IsADarkerBoxThreePixelsDownAndRight_DrawnBeforeTheFrame()
    {
        var ctx = new RecordingRenderContext();
        var renderer = new LegendRenderer(Named with { Shadow = true }, Theme.Default, ctx);
        var layout = PlaceOrFail(renderer, [LineEntry()]);
        renderer.Draw([LineEntry()], layout, interactive: false);

        var rects = ctx.OfKind("DrawRectangle");
        Assert.Equal(new Rect(layout.BoxX + 3, layout.BoxY + 3, layout.BoxWidth, layout.BoxHeight), (Rect)Args(rects[0]).rect);
        Assert.Equal(new Color(0, 0, 0, 80), (Color?)Args(rects[0]).fill);
        Assert.Equal(new Rect(layout.BoxX, layout.BoxY, layout.BoxWidth, layout.BoxHeight), (Rect)Args(rects[1]).rect);
    }

    [Fact]
    public void Draw_FrameOff_DrawsNoBox_AndNoShadow()
    {
        Assert.Empty(Drawn(Named with { FrameOn = false, Shadow = true }, [LineEntry()]).OfKind("DrawRectangle"));
    }

    [Fact]
    public void Draw_ATitle_IsCentredOverTheEntries_InTheTitleFont()
    {
        var ctx = new RecordingRenderContext();
        var renderer = new LegendRenderer(Named with { Title = "Legend" }, Theme.Default, ctx);
        var layout = PlaceOrFail(renderer, [LineEntry()]);
        renderer.Draw([LineEntry()], layout, interactive: false);

        var title = ctx.OfKind("DrawText")[0];
        Assert.Equal("Legend", (string)Args(title).text);
        Assert.Equal(new Point(layout.BoxX + layout.Padding + layout.TotalContentWidth / 2, layout.BoxY + layout.Padding + layout.TitleFont.Size),
            (Point)Args(title).position);
        Assert.Equal(layout.TitleFont, (Font)Args(title).font);
        Assert.Equal(TextAlignment.Center, (TextAlignment)Args(title).alignment);
    }

    [Fact]
    public void Draw_NoTitle_DrawsOnlyTheEntryTexts()
    {
        var texts = Drawn(Named, [LineEntry("one"), LineEntry("two", 1)]).OfKind("DrawText");

        Assert.Equal(["one", "two"], texts.Select(t => (string)Args(t).text));
    }

    // ── The entries ────────────────────────────────────────────────────────

    [Fact]
    public void Draw_AnEntryText_StandsPastItsSwatch_LeftAligned_InTheLegendFont()
    {
        var ctx = new RecordingRenderContext();
        var renderer = new LegendRenderer(Named, Theme.Default, ctx);
        var layout = PlaceOrFail(renderer, [LineEntry("one")]);
        renderer.Draw([LineEntry("one")], layout, interactive: false);

        var text = ctx.OfKind("DrawText")[0];
        Assert.Equal(new Point(layout.ColumnX(0) + layout.SwatchWidth + layout.SwatchGap, layout.EntryY(0) + layout.SwatchHeight - 1),
            (Point)Args(text).position);
        Assert.Equal(layout.Font, (Font)Args(text).font);
        Assert.Equal(TextAlignment.Left, (TextAlignment)Args(text).alignment);
    }

    [Fact]
    public void Draw_AMathLabel_IsDrawnAsRichText()
    {
        var ctx = Drawn(Named, [LineEntry(@"$\alpha$ decay")]);

        Assert.Single(ctx.OfKind("DrawRichText"));
        Assert.Empty(ctx.OfKind("DrawText"));
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
        var texts = ctx.OfKind("DrawText").Select(t => (Point)Args(t).position).ToArray();
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

    [Fact]
    public void ItemBounds_NoEntries_IsEmpty()
    {
        var layout = new LegendLayoutCalculator(Theme.Default, new SvgRenderContext()).Compute(Named, [], Area);

        Assert.Empty(LegendRenderer.ItemBounds([], layout));
    }

    [Fact]
    public void Draw_Interactive_OnSvg_WrapsEachEntryInAGroupCarryingItsSeriesIndex()
    {
        var entries = new[] { LineEntry("first", 2), LineEntry("second", 5) };
        var ctx = new SvgRenderContext();
        var renderer = new LegendRenderer(Named, Theme.Default, ctx);
        renderer.Draw(entries, PlaceOrFail(renderer, entries), interactive: true);

        string svg = ctx.GetOutput();
        Assert.Contains("<g data-legend-index=\"2\" style=\"cursor:pointer\" aria-label=\"first\">", svg);
        Assert.Contains("<g data-legend-index=\"5\" style=\"cursor:pointer\" aria-label=\"second\">", svg);
        Assert.Equal(3, svg.Split("</g>").Length - 1);
    }

    [Fact]
    public void Draw_NotInteractive_OnSvg_WritesNoEntryGroups()
    {
        var ctx = new SvgRenderContext();
        var renderer = new LegendRenderer(Named, Theme.Default, ctx);
        renderer.Draw([LineEntry()], PlaceOrFail(renderer, [LineEntry()]), interactive: false);

        Assert.DoesNotContain("data-legend-index", ctx.GetOutput());
    }

    [Fact]
    public void Draw_Interactive_OnAnotherSurface_OpensNoEntryGroups()
    {
        var ctx = new RecordingRenderContext();
        var renderer = new LegendRenderer(Named, Theme.Default, ctx);
        renderer.Draw([LineEntry(), LineEntry("b", 1)], PlaceOrFail(renderer, [LineEntry(), LineEntry("b", 1)]), interactive: true);

        Assert.Equal(1, ctx.CountOf("BeginGroup"));
        Assert.Equal(1, ctx.CountOf("EndGroup"));
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

        var texts = Drawn(Named, axes.LegendEntries(endLabelsDrawn)).OfKind("DrawText");

        Assert.Equal(expected, texts.Select(t => (string)Args(t).text));
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
        var entry = Entry(Line(), new LegendSwatch(LegendSwatchKind.Line, LineWidth: 3, LineStyle: LineStyle.Dashed));
        var (x, y, w, h) = SwatchBox(entry);

        var line = Assert.Single(SwatchOf(entry));
        Assert.Equal("DrawLine", line.Kind);
        Assert.Equal(new Point(x, y + h / 2), (Point)Args(line).p1);
        Assert.Equal(new Point(x + w, y + h / 2), (Point)Args(line).p2);
        Assert.Equal(Colors.Red, (Color)Args(line).color);
        Assert.Equal(3.0, (double)Args(line).thickness);
        Assert.Equal(LineStyle.Dashed, (LineStyle)Args(line).style);
    }

    [Fact]
    public void Swatch_Line_ThatIsLineStyleNone_DrawsNothing()
    {
        Assert.Empty(SwatchOf(Entry(Line(), new LegendSwatch(LegendSwatchKind.Line, LineStyle: LineStyle.None))));
    }

    [Fact]
    public void Swatch_Line_WearsTheColourOfItsColourSlot_WhenTheSeriesNamesNone()
    {
        var entry = new LegendEntry(new LineSeries(X, X) { Label = "line" }, 0, 2, "entry", new LegendSwatch(LegendSwatchKind.Line));

        Assert.Equal(Theme.Default.CycleColors[2], (Color)Args(Assert.Single(SwatchOf(entry))).color);
    }

    [Fact]
    public void Swatch_LineMarker_IsTheLineWithTheMarkerAtItsMiddle()
    {
        var entry = Entry(Line(), new LegendSwatch(LegendSwatchKind.LineMarker, Marker: MarkerStyle.Circle, MarkerSize: 36));
        var (x, y, w, h) = SwatchBox(entry);

        var calls = SwatchOf(entry);
        Assert.Equal(["DrawLine", "DrawCircle"], calls.Select(c => c.Kind));
        Assert.Equal(new Point(x + w / 2, y + h / 2), (Point)Args(calls[1]).center);
        Assert.Equal(Math.Sqrt(36 / Math.PI) * (100.0 / 72.0), (double)Args(calls[1]).radius, 9);
        Assert.Equal(Colors.Red, (Color?)Args(calls[1]).fill);
    }

    [Fact]
    public void Swatch_LineMarker_ATinyMarker_IsDrawnAtTheSizeOfOneSquarePoint()
    {
        var entry = Entry(Line(), new LegendSwatch(LegendSwatchKind.LineMarker, Marker: MarkerStyle.Circle, MarkerSize: 0));

        Assert.Equal(Math.Sqrt(1 / Math.PI) * (100.0 / 72.0), (double)Args(SwatchOf(entry)[1]).radius, 9);
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
        var entry = Entry(Line(), new LegendSwatch(LegendSwatchKind.SolidLine, LineWidth: 2, LineStyle: LineStyle.Dashed));

        var line = Assert.Single(SwatchOf(entry));
        Assert.Equal(LineStyle.Solid, (LineStyle)Args(line).style);
        Assert.Equal(2.0, (double)Args(line).thickness);
    }

    [Fact]
    public void Swatch_Marker_IsOneMarkerAtTheSwatchsLeft_NoBiggerThanTheSwatchIsHigh()
    {
        var entry = Entry(new ScatterSeries(X, X) { Label = "s", Color = Colors.Red },
            new LegendSwatch(LegendSwatchKind.Marker, MarkerSize: 10000));
        var (x, y, _, h) = SwatchBox(entry);

        var circle = Assert.Single(SwatchOf(entry));
        Assert.Equal("DrawCircle", circle.Kind);
        Assert.Equal(new Point(x + h / 2, y + h / 2), (Point)Args(circle).center);
        double clamped = h / 2 - 1;
        Assert.Equal(Math.Sqrt(clamped * clamped) * (100.0 / 72.0), (double)Args(circle).radius, 9);
    }

    [Fact]
    public void Swatch_Marker_NamingNone_IsTheRestingCircle_AndOneNamed_IsThatShape()
    {
        var resting = Entry(new ScatterSeries(X, X) { Color = Colors.Red }, new LegendSwatch(LegendSwatchKind.Marker, MarkerSize: 36));
        var square = Entry(new ScatterSeries(X, X) { Color = Colors.Red },
            new LegendSwatch(LegendSwatchKind.Marker, Marker: MarkerStyle.Square, MarkerSize: 36));

        Assert.Equal("DrawCircle", Assert.Single(SwatchOf(resting)).Kind);
        Assert.Equal("DrawRectangle", Assert.Single(SwatchOf(square)).Kind);
    }

    [Fact]
    public void Swatch_ErrorBar_IsALineWithACapAtEachEnd()
    {
        var entry = Entry(Line(), new LegendSwatch(LegendSwatchKind.ErrorBar, LineWidth: 2));
        var (x, y, w, h) = SwatchBox(entry);
        double mid = y + h / 2;

        var lines = SwatchOf(entry);
        Assert.Equal(3, lines.Count);
        Assert.All(lines, l => Assert.Equal("DrawLine", l.Kind));
        Assert.Equal((new Point(x + 2, mid), new Point(x + w - 2, mid)), ((Point)Args(lines[0]).p1, (Point)Args(lines[0]).p2));
        Assert.Equal((new Point(x + 2, mid - 3), new Point(x + 2, mid + 3)), ((Point)Args(lines[1]).p1, (Point)Args(lines[1]).p2));
        Assert.Equal((new Point(x + w - 2, mid - 3), new Point(x + w - 2, mid + 3)), ((Point)Args(lines[2]).p1, (Point)Args(lines[2]).p2));
        Assert.All(lines, l => Assert.Equal(2.0, (double)Args(l).thickness));
    }

    [Fact]
    public void Swatch_Stripes_AreOneStripePerDistinctState_SideBySide()
    {
        StateSegment[] states = [new(0, 1, "up", Colors.Green), new(1, 2, "down", Colors.Red), new(2, 3, "up", Colors.Green)];
        var entry = Entry(Line(), new LegendSwatch(LegendSwatchKind.Stripes, States: states));
        var (x, y, w, h) = SwatchBox(entry);

        var stripes = SwatchOf(entry);
        Assert.Equal(2, stripes.Count);
        Assert.Equal(new Rect(x, y, w / 2, h), (Rect)Args(stripes[0]).rect);
        Assert.Equal(new Rect(x + w / 2, y, w / 2, h), (Rect)Args(stripes[1]).rect);
        Assert.Equal([Colors.Green, Colors.Red], stripes.Select(s => (Color?)Args(s).fill));
    }

    [Fact]
    public void Swatch_Stripes_WithNoStateToShow_IsAFilledRectangle()
    {
        var entry = Entry(Line(), new LegendSwatch(LegendSwatchKind.Stripes));
        var (x, y, w, h) = SwatchBox(entry);

        var patch = Assert.Single(SwatchOf(entry));
        Assert.Equal(new Rect(x, y, w, h), (Rect)Args(patch).rect);
        Assert.Equal(Colors.Red, (Color?)Args(patch).fill);
    }

    [Fact]
    public void Swatch_Patch_IsAFilledRectangle_TheFullSwatchLongAndHigh()
    {
        var entry = Entry(Line(), LegendSwatch.Patch);
        var (x, y, w, h) = SwatchBox(entry);

        var patch = Assert.Single(SwatchOf(entry));
        Assert.Equal("DrawRectangle", patch.Kind);
        Assert.Equal(new Rect(x, y, w, h), (Rect)Args(patch).rect);
        Assert.Equal(Colors.Red, (Color?)Args(patch).fill);
        Assert.Null((Color?)Args(patch).stroke);
    }
}
