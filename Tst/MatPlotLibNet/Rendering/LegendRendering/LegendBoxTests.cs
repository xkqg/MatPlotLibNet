// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.LegendRendering;

/// <summary>The box a legend is laid out in. Under the matplotlib themes it is matplotlib's (<c>Legend._init_legend_box</c>, placed by
/// <c>_get_anchored_bbox</c>), printed with matplotlib 3.11.2 at 800 x 600 and 100 dpi: the frame pads its rows by <c>legend.borderpad</c>
/// (0.4 of the legend's font size) on every side and stands <c>legend.borderaxespad</c> (0.5 of it) inside the corner of the axes; a row is
/// as tall as its text or its handle (<c>handleheight</c> 0.7), whichever rises higher above the baseline, plus the text's descent, the
/// handle's foot on the baseline; rows are <c>labelspacing</c> (0.5) apart. Under the default style (font 13.889 px) one entry 'series' 42 px
/// wide makes a frame 92 x 25.448 at (106.944, 78.944) on axes at (100, 72): 92 = 42 + 2 x 5.556 + 27.778 + 11.111, 25.448 = 2 x 5.556 + the text
/// box 14.337; two entries 46.729 = 11.111 + 2 x 14.337 + 6.944; at the lower right the frame ends 6.944 short of the axes' right and bottom
/// (713.056, 527.056 on axes to 720, 534). Under the classic style (font 20 px) the frame is 131 x 36 at 10 px from the axes' top right corner.
/// The title is the font size (10 and 12 pt), not bold, its band its text and a label spacing; a title wider than the entries widens the box
/// and the entries stand centred under it (VPacker, align 'center'). The entries fill the columns in turn as <c>np.array_split</c> deals them,
/// an empty column dropped. The library's own themes keep the box they always had.</summary>
public class LegendBoxTests
{
    private static readonly Theme V2 = Theme.MatplotlibV2;
    private static readonly Theme Classic = Theme.MatplotlibClassic;

    /// <summary>The axes of an 800 x 600 figure under each style's subplot parameters (printed: left 100, top 72 / 60, right 720).</summary>
    private static readonly Rect V2Axes = new(100, 72, 620, 462);
    private static readonly Rect ClassicAxes = new(100, 60, 620, 480);

    private static Legend At(LegendPosition position = LegendPosition.UpperRight) => new() { Position = position };

    private static LegendLayout Layout(Theme theme, Rect axes, Legend legend, params string[] labels) =>
        new LegendLayoutCalculator(theme, new SvgRenderContext()).Compute(legend, labels, axes, MarkObstacles.None);

    private static TextExtent Extent(Theme theme, Legend legend, string text) =>
        new SvgRenderContext().ExtentOf(TextLabel.Of(text), theme.LegendFontOf(legend));

    private static double V2Em => 10.0.PointsInPixels;

    // ── the box of one entry ────────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void UnderV2_TheFramePadsItsRowByPointFourOfTheFontSize_AndIsTheTextAndThreePointSixEmsWide()
    {
        var legend = At();
        var layout = Layout(V2, V2Axes, legend, "series");
        double text = Extent(V2, legend, "series").Width;

        Assert.Equal(V2Em, layout.Font.Size, 6);
        Assert.Equal(0.4 * V2Em, layout.Padding, 6);
        Assert.Equal(text + (2 * 0.4 + 2.0 + 0.8) * V2Em, layout.BoxWidth, 6);
    }

    [Fact]
    public void UnderV2_ARowIsAsTallAsItsTextOrItsHandle_WhicheverRisesHigher_PlusTheTextsDescent()
    {
        var legend = At();
        var text = Extent(V2, legend, "series");
        double row = Math.Max(0.7 * V2Em, text.Ascent) + text.Descent;

        var one = Layout(V2, V2Axes, legend, "series");
        var two = Layout(V2, V2Axes, legend, "series", "series");

        Assert.Equal(2 * 0.4 * V2Em + row, one.BoxHeight, 6);
        Assert.Equal(2 * 0.4 * V2Em + 2 * row + 0.5 * V2Em, two.BoxHeight, 6);
        Assert.Equal(row + 0.5 * V2Em, two.LineHeight, 6);
    }

    [Fact]
    public void UnderV2_TheTextStandsOnTheFootOfItsHandle()
    {
        var legend = At();
        var text = Extent(V2, legend, "series");
        var layout = Layout(V2, V2Axes, legend, "series");

        Assert.Equal(Math.Max(0.7 * V2Em, text.Ascent), layout.Slots[0].Row.Baseline, 6);
        Assert.Equal(layout.Slots[0].Row.Baseline - layout.SwatchHeight, layout.Slots[0].Row.HandleTop, 6);
    }

    [Fact]
    public void UnderV2_TheBoxStandsHalfTheFontSizeInsideTheCornerOfTheAxes()
    {
        var upperRight = Layout(V2, V2Axes, At(LegendPosition.UpperRight), "series");
        var lowerLeft = Layout(V2, V2Axes, At(LegendPosition.LowerLeft), "series");

        Assert.Equal(V2Axes.X + V2Axes.Width - 0.5 * V2Em, upperRight.BoxX + upperRight.BoxWidth, 6);
        Assert.Equal(V2Axes.Y + 0.5 * V2Em, upperRight.BoxY, 6);
        Assert.Equal(V2Axes.X + 0.5 * V2Em, lowerLeft.BoxX, 6);
        Assert.Equal(V2Axes.Y + V2Axes.Height - 0.5 * V2Em, lowerLeft.BoxY + lowerLeft.BoxHeight, 6);
    }

    [Fact]
    public void UnderClassic_TheSameRule_AtTheClassicLegendSizeOfTwentyPixels()
    {
        var legend = At();
        var layout = Layout(Classic, ClassicAxes, legend, "series");
        var text = Extent(Classic, legend, "series");

        Assert.Equal(20.0, layout.Font.Size, 6);
        Assert.Equal(8.0, layout.Padding, 6);
        Assert.Equal(text.Width + 72.0, layout.BoxWidth, 6);
        Assert.Equal(16.0 + Math.Max(14.0, text.Ascent) + text.Descent, layout.BoxHeight, 6);
        Assert.Equal(710.0, layout.BoxX + layout.BoxWidth, 6);
        Assert.Equal(70.0, layout.BoxY, 6);
    }

    [Fact]
    public void UnderV2_TheTextIsDrawnOnTheRowsBaseline_AndTheHandleAboveIt()
    {
        var legend = At();
        var ctx = new RecordingRenderContext();
        var renderer = new LegendRenderer(legend with { FrameOn = false }, V2, ctx);
        LegendEntry entry = new SeriesLegendEntry(new LineSeries([0.0, 1.0], [0.0, 1.0]) { Label = "series" }, 0, 0, "series",
            new LegendSwatch(LegendSwatchKind.Line));
        var layout = renderer.Place([entry], V2Axes, () => MarkObstacles.None)!;

        renderer.Draw([entry], layout, interactive: false);

        double top = layout.EntryY(0);
        var line = ctx.Of<DrawLineCall>()[0];
        var text = ctx.Of<DrawTextCall>()[0];
        Assert.Equal(top + layout.Slots[0].Row.HandleTop + layout.SwatchHeight / 2, line.P1.Y, 6);
        Assert.Equal(top + layout.Slots[0].Row.Baseline, text.Position.Y, 6);
    }

    /// <summary>A label the text seam measures deeper than a plain line: a stacked fraction.</summary>
    private const string Tall = @"$\frac{a}{b}$";

    /// <summary>Each entry takes its own row, laid out by its own text through the text seam, as matplotlib stacks the entries of a column
    /// (a <c>VPacker</c> of their boxes, <c>labelspacing</c> apart; printed with 3.11.2 under the default style: '$\frac{a}{b}$' then 'x'
    /// make rows 17.0 and 14.337 tall, '$x^2$' then 'x' 16.337 and 14.337, the pixel figures the Skia suite's <c>LegendRowSkiaTests</c>
    /// holds, where the glyph metrics measure like Agg): each row is its own text's, and the box holds the rows and the gaps between them.</summary>
    [Fact]
    public void UnderV2_EveryEntryTakesARowAsTallAsItsOwnText_TheRowsLabelSpacingApart()
    {
        var legend = At();
        var layout = Layout(V2, V2Axes, legend, Tall, "x");
        double handle = 0.7 * V2Em, gap = 0.5 * V2Em;
        LegendRow RowOf(string text) => LegendBox.Matplotlib.RowOf(handle, Extent(V2, legend, text).AtLeast(Extent(V2, legend, "lp")));

        Assert.Equal([RowOf(Tall), RowOf("x")], layout.Slots.Select(s => s.Row));
        Assert.Equal(layout.Slots[0].Top + layout.Slots[0].Row.Height + gap, layout.Slots[1].Top, 9);
        Assert.Equal(2 * layout.Padding + RowOf(Tall).Height + gap + RowOf("x").Height, layout.BoxHeight, 9);
    }

    /// <summary>The columns stand on the baseline of their first rows (the <c>HPacker</c> of the columns, align 'baseline'): a column whose
    /// first row rises less drops by the difference (printed with 3.11.2 under the default style, 'x', 'y', '$x^2$', 'q' in two columns: the
    /// rows of the first column at 86.5 and 107.781, of the second at 84.5 and 107.781), and the box reaches the deepest column.</summary>
    [Fact]
    public void UnderV2_TheColumnsStandOnTheBaselineOfTheirFirstRows()
    {
        var legend = At() with { NCols = 2 };
        var layout = Layout(V2, V2Axes, legend, "x", "y", "$x^2$", "q");
        double handle = 0.7 * V2Em;
        var plain = LegendBox.Matplotlib.RowOf(handle, Extent(V2, legend, "x").AtLeast(Extent(V2, legend, "lp")));
        var raised = LegendBox.Matplotlib.RowOf(handle, Extent(V2, legend, "$x^2$").AtLeast(Extent(V2, legend, "lp")));
        double entriesTop = layout.BoxY + layout.Padding;

        Assert.Equal(entriesTop + raised.Baseline - plain.Baseline, layout.Slots[0].Top, 9);
        Assert.Equal(entriesTop, layout.Slots[2].Top, 9);
        Assert.Equal(layout.Slots[0].Top, layout.EntryY(0), 9);
        Assert.Equal(entriesTop + 2 * layout.LineHeight, layout.EntryY(2), 9);
    }

    /// <summary>Every text of a matplotlib legend is boxed at least as tall as 'lp' (<c>TextArea</c>); the library's own box takes each text
    /// as it measures.</summary>
    [Fact]
    public void OnlyTheMatplotlibBox_BoxesItsTextsAtLeastAsTallAsLp()
    {
        Assert.Equal("lp", LegendBox.Matplotlib.LineFloor);
        Assert.Null(LegendBox.Library.LineFloor);
    }

    /// <summary>A layout made by hand holds no entries, so its rows stand <see cref="LegendLayout.LineHeight"/> apart below the title.</summary>
    [Fact]
    public void ALayoutMadeByHand_PutsItsRowsALineHeightApart()
    {
        var font = new Font { Size = 10 };
        var layout = new LegendLayout(font, font, 20, 7, 5, 4, 15, 1, 3, [30.0], 0, 55, 6, 63, 47, 100, 200);

        Assert.Equal(200 + 4 + 6 + 2 * 15, layout.EntryY(2), 9);
    }

    /// <summary>An entry is hit-tested by its row under the matplotlib themes, whose padding (0.4 of the font size) is narrower than the gap
    /// below a row (0.5 of it), so the last row and its gap would reach past the frame; the library's own box keeps the row and the gap
    /// below it.</summary>
    [Fact]
    public void AnEntryIsHitTestedByItsRow_InsideTheFrame_AndUnderTheLibrarysThemesByItsRowAndTheGapBelowIt()
    {
        var v2 = Layout(V2, V2Axes, At(), "a", "b");
        var library = Layout(Theme.Default, V2Axes, At(), "a", "b");

        Assert.Equal(v2.Slots[1].Row.Height, v2.Slots[1].HitHeight, 9);
        Assert.True(v2.Slots[1].Top + v2.Slots[1].HitHeight <= v2.BoxY + v2.BoxHeight);
        Assert.Equal(library.LineHeight, library.Slots[1].HitHeight);
    }

    // ── the title ───────────────────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void UnderTheMatplotlibThemes_TheTitleIsTheFontSize_NotBold_AndItsBandIsItsTextAndALabelSpacing()
    {
        var v2 = Layout(V2, V2Axes, At() with { Title = "Title" }, "series");
        var classic = Layout(Classic, ClassicAxes, At() with { Title = "Title" }, "series");
        var title = new SvgRenderContext().ExtentOf(TextLabel.Of("Title"), v2.TitleFont);

        Assert.Equal(10.0.PointsInPixels, v2.TitleFont.Size, 6);
        Assert.Equal(12.0.PointsInPixels, classic.TitleFont.Size, 6);
        Assert.Equal(FontWeight.Normal, v2.TitleFont.Weight);
        Assert.Equal(FontWeight.Normal, classic.TitleFont.Weight);
        Assert.Equal(title.Height + 0.5 * V2Em, v2.TitleHeight, 6);
        Assert.Equal(title.Ascent, v2.Title.Baseline, 6);
        Assert.Equal(v2.BoxY + v2.Padding + v2.TitleHeight, v2.EntryY(0), 6);
    }

    /// <summary>A title wider than the entries widens the box under every theme, and the entries stand centred under it, as matplotlib's
    /// <c>VPacker</c> (align 'center') lays the title over the entries: the library's own box no longer lets a long title spill past its
    /// frame.</summary>
    [Theory]
    [MemberData(nameof(EveryBox))]
    public void ATitleWiderThanTheEntries_WidensTheBox_AndTheEntriesStandCentredUnderIt(Theme theme)
    {
        const string Long = "A much longer legend title";
        var legend = At() with { Title = Long };
        var layout = Layout(theme, V2Axes, legend, "a");
        double title = new SvgRenderContext().ExtentOf(TextLabel.Of(Long), layout.TitleFont).Width;

        Assert.Equal(title + 2 * layout.Padding, layout.BoxWidth, 6);
        Assert.Equal(layout.BoxX + layout.Padding + (title - layout.TotalContentWidth) / 2, layout.ColumnX(0), 6);

        var ctx = new RecordingRenderContext();
        var renderer = new LegendRenderer(legend, theme, ctx);
        renderer.Draw(Entries("a"), layout, interactive: false);
        double centre = (ctx.Of<DrawTextCall>().First(c => c.Text == Long).Position).X;
        Assert.InRange(centre - title / 2, layout.BoxX, layout.BoxX + layout.BoxWidth);
        Assert.InRange(centre + title / 2, layout.BoxX, layout.BoxX + layout.BoxWidth);
    }

    public static TheoryData<Theme> EveryBox => new() { Theme.Default, V2, Classic };

    private static LegendEntry[] Entries(params string[] labels) =>
        [.. labels.Select((label, i) => (LegendEntry)new SeriesLegendEntry(new LineSeries([0.0, 1.0], [0.0, 1.0]) { Label = label }, i, i, label,
            new LegendSwatch(LegendSwatchKind.Line)))];

    // ── the columns ─────────────────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void UnderTheMatplotlibThemes_TheEntriesFillTheColumnsInTurn_AsArraySplitDealsThem()
    {
        string[] labels = ["a", "bb", "ccc", "dddd", "e"];
        var layout = Layout(V2, V2Axes, At() with { NCols = 2 }, labels);
        double Width(string s) => Extent(V2, At(), s).Width;

        Assert.Equal(3, layout.NRows);
        Assert.Equal([new LegendCell(0, 0), new LegendCell(1, 0), new LegendCell(2, 0), new LegendCell(0, 1), new LegendCell(1, 1)],
            Enumerable.Range(0, labels.Length).Select(layout.CellOf));
        Assert.Equal(new[] { Width("ccc"), Width("dddd") }, layout.ColMaxWidths);
    }

    [Fact]
    public void UnderTheMatplotlibThemes_AskedForMoreColumnsThanEntries_TheEmptyColumnsAreDropped()
    {
        var layout = Layout(Classic, ClassicAxes, At() with { NCols = 3 }, "a", "bb");
        double Width(string s) => Extent(Classic, At(), s).Width;

        Assert.Equal(2, layout.NCols);
        Assert.Equal(16 + 2 * (40 + 16) + Width("a") + Width("bb") + 40, layout.BoxWidth, 6);
    }

    // ── the library's own box ───────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void UnderTheLibrarysThemes_TheBoxIsTheOneItAlwaysWas()
    {
        var legend = At() with { NCols = 2, Title = "T" };
        var layout = Layout(Theme.Default, V2Axes, legend, "a", "bb", "ccc");

        Assert.Equal(8.0, layout.Padding);
        Assert.Equal(V2Axes.Y + LegendPositionStrategy.Inset, layout.BoxY);
        Assert.Equal(0.0, layout.Slots[0].Row.HandleTop);
        Assert.Equal(layout.SwatchHeight - 1, layout.Slots[0].Row.Baseline);
        Assert.Equal(layout.TitleFont.Size, layout.Title.Baseline);
        Assert.Equal(FontWeight.Bold, layout.TitleFont.Weight);
        Assert.Equal([new LegendCell(0, 0), new LegendCell(0, 1), new LegendCell(1, 0)], Enumerable.Range(0, 3).Select(layout.CellOf));
        Assert.Equal(layout.BoxX + layout.Padding, layout.ColumnX(0));
    }
}
