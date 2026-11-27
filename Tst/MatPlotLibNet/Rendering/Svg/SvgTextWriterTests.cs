// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>The texts of the SVG surface (<see cref="Rendering.Svg.SvgRenderContext.SvgTextWriter"/>): a <c>&lt;text&gt;</c> element in the viewer's fonts, or the
/// text's glyph outline when a glyph source is installed, each placed by its alignment (read the other way round for a right-to-left
/// text) and turned about its anchor.</summary>
[Collection("ChartServicesGlobalState")] // sets GlyphPathProviderDefault.Current, which every text reads
public class SvgTextWriterTests
{
    private static readonly Font Plain = new() { Family = "F", Size = 10 };

    /// <summary>Writes one text with <paramref name="provider"/> installed, then puts the installed one back.</summary>
    private static string Written(IGlyphPathProvider? provider, Action<SvgElementBuffer, SvgRenderContext.SvgTextWriter> write)
    {
        using var heldOutlines = GlyphPathProviderDefault.Holding(provider);
        var buffer = new SvgElementBuffer(default);
        write(buffer, new SvgRenderContext.SvgTextWriter(buffer));
        return buffer.Markup.ToString();
    }

    private static TestShapedGlyphPaths ShapedOutlines => new(_ => "M 0 0", (_, _) => new ShapedOutline("M 0 0", 8));

    [Fact]
    public void AText_WithNoGlyphSource_IsATextElementInTheViewersFonts()
    {
        string svg = Written(null, (_, text) => text.Write("a<b", new Point(10, 20), Plain, TextAlignment.Center, 0));

        Assert.Equal("<text x=\"10\" y=\"20\" font-family=\"F\" font-size=\"10\" text-anchor=\"middle\">a&lt;b</text>\n", svg);
    }

    [Theory]
    [InlineData("abc", TextAlignment.Left, " text-anchor=\"start\">")]
    [InlineData("abc", TextAlignment.Right, " text-anchor=\"end\">")]
    [InlineData("שלום", TextAlignment.Left, " text-anchor=\"end\" direction=\"rtl\">")]
    [InlineData("שלום", TextAlignment.Right, " text-anchor=\"start\" direction=\"rtl\">")]
    [InlineData("שלום", TextAlignment.Center, " text-anchor=\"middle\" direction=\"rtl\">")]
    public void TheAnchorOfAText_IsItsEdge_ReadTheOtherWayRoundForARightToLeftText(string label, TextAlignment alignment, string expected)
    {
        string svg = Written(null, (_, text) => text.Write(label, new Point(0, 0), Plain, alignment, 0));

        Assert.Contains(expected, svg);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("ATurnedText_TurnsAboutItsAnchor_TheWaySvgTurns", () => Written(null, (_, text) => text.Write("a", new Point(10, 20), Plain, TextAlignment.Left, 90)), " transform=\"rotate(-90,10,20)\""),
        new DrawnToken("AnItalicBoldColouredText_SaysSo", () =>
        {
            var font = Plain with { Slant = FontSlant.Italic, Weight = FontWeight.Bold, Color = Colors.Red };
            return Written(null, (_, text) => text.Write("a", new Point(0, 0), font, TextAlignment.Left, 0));
        }, " font-style=\"italic\" font-weight=\"bold\" fill=\"#FF0000\">"),
        // An outline has no width to fit, and leaves none for the text after it.
        new DrawnToken("AnOutline_HasNoWidthToFit_AndLeavesNoneForTheTextAfterIt", () =>
        {
            var buffer = new SvgElementBuffer(default);
            var text = new SvgRenderContext.SvgTextWriter(buffer);
            buffer.SetNextTextLength(40);
            using (GlyphPathProviderDefault.Holding(new TestGlyphPaths("M 1 1"))) text.Write("a", new Point(0, 0), Plain, TextAlignment.Left, 0);
            using (GlyphPathProviderDefault.Holding(null)) text.Write("b", new Point(0, 0), Plain, TextAlignment.Left, 0);
            return buffer.Markup.ToString();
        }) { Absent = ["textLength"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void TheWidthAskedOfATextElement_FitsItOnce()
    {
        string svg = Written(null, (buffer, text) =>
        {
            buffer.SetNextTextLength(40);
            text.Write("a", new Point(0, 0), Plain, TextAlignment.Left, 0);
            text.Write("b", new Point(0, 0), Plain, TextAlignment.Left, 0);
        });

        Assert.Equal(1, svg.Split(" textLength=\"40\" lengthAdjust=\"spacingAndGlyphs\"").Length - 1);
    }

    [Fact]
    public void AText_WithAGlyphSource_IsItsOutline_PlacedByTheOutlinesAdvance()
    {
        string svg = Written(ShapedOutlines, (_, text) => text.Write("a", new Point(10, 20), Plain, TextAlignment.Right, 90));

        Assert.Equal("<path d=\"M 0 0\" transform=\"translate(10,20) rotate(-90) translate(-8,0)\" />\n", svg);
    }

    [Fact]
    public void OnlyAShapingGlyphSource_OutlinesInThePackagesFaces()
    {
        using (GlyphPathProviderDefault.Holding(null)) Assert.False(SvgRenderContext.SvgTextWriter.OutlinesInPackageFaces);
        using (GlyphPathProviderDefault.Holding(new TestGlyphPaths("M 0 0"))) Assert.False(SvgRenderContext.SvgTextWriter.OutlinesInPackageFaces);
        using (GlyphPathProviderDefault.Holding(ShapedOutlines)) Assert.True(SvgRenderContext.SvgTextWriter.OutlinesInPackageFaces);
    }
}
