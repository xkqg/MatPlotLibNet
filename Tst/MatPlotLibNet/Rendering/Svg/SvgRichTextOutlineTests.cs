// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>With the package's glyph source installed every text of an SVG is its glyph outline, a label with math in it too: the label
/// is laid out once in core and each run of its layout is drawn as a text is, so it is outlined in its own face and size and stands
/// where the layout put it, the whole label placed by its alignment and turned about its anchor. Without a glyph source each run is a
/// text element for the viewer to set.</summary>
[Collection("ChartServicesGlobalState")] // mutates GlyphPathProviderDefault.Current
public sealed class SvgRichTextOutlineTests
{
    private static T WithStub<T>(Func<TestShapedGlyphPaths, T> draw)
    {
        var stub = new TestShapedGlyphPaths(text => "M" + text, (text, font) => text.Length == 0 ? null : new ShapedOutline("M" + text, text.Length * font.Size / 2));
        using var heldOutlines = GlyphPathProviderDefault.Holding(stub);
        return draw(stub);
    }

    /// <summary>The outline element a run of the layout is written as: its path, the label's fill, moved to where the run starts.</summary>
    private static string Outline(string text, Point at, string fill = "", string turn = "") => string.Create(CultureInfo.InvariantCulture,
        $"<path d=\"M{text}\"{fill} transform=\"translate({Number(at.X)},{Number(at.Y)}){turn}\" />");

    private static string Number(double value) => new System.Text.StringBuilder().AppendSvgNumber(value).ToString();

    [Fact]
    public void ALabelWithASuperscript_IsARunOfOutlinesEachWhereTheLayoutPutsIt()
    {
        var font = new Font { Size = 10 };
        var (svg, stub, ink) = WithStub(stub =>
        {
            var ctx = new SvgRenderContext();
            ctx.DrawRichText(MathTextParser.Parse("x$^{2}$"), new Point(10, 20), font, TextAlignment.Left);
            return (ctx.GetOutput(), stub, ctx.LayOut(TextLabel.Of("x$^{2}$").Parsed(null).Tree!, font));
        });

        var x = ink.Glyphs[0];
        var two = ink.Glyphs[1];
        Assert.Equal(Outline("x", new Point(10, 20)) + "\n" + Outline("2", new Point(10 + two.X, 20 + two.Y)) + "\n", svg);
        Assert.True(two.Y < 0 && two.X > x.X, "the superscript stands right of the x and raised");
        Assert.Equal([7.0, 10.0], stub.Fonts.Select(f => f.Size).Distinct().Order());
        Assert.DoesNotContain("<text", svg);
    }

    [Fact]
    public void ALabelTurnedAndCentred_TurnsEachRunAboutItsStart_AndCarriesItsFill()
    {
        var font = new Font { Size = 10, Color = Colors.Red };
        var (svg, ink) = WithStub(_ =>
        {
            var ctx = new SvgRenderContext();
            ctx.DrawRichText(MathTextParser.Parse("x$_{i}$"), new Point(10, 20), font, TextAlignment.Center, 90);
            return (ctx.GetOutput(), ctx.LayOut(TextLabel.Of("x$_{i}$").Parsed(null).Tree!, font));
        });

        // Centred, the layout starts half its width before the anchor, along the baseline turned 90 degrees: up the picture.
        double start = -ink.Extent.Width / 2;
        var i = ink.Glyphs[1];
        var first = new Point(10, 20 - start);
        var second = new Point(10 + i.Y, 20 - (start + i.X));
        Assert.Equal(Outline("x", first, " fill=\"#FF0000\"", " rotate(-90)") + "\n"
                     + Outline("i", second, " fill=\"#FF0000\"", " rotate(-90)") + "\n", svg);
    }

    /// <summary>A label whose layout is one run on its baseline is outlined whole, as a plain text is.</summary>
    [Fact]
    public void ALabelOfOneRun_IsOutlinedWhole_AsAPlainTextIs()
    {
        var svg = WithStub(_ =>
        {
            var ctx = new SvgRenderContext();
            ctx.DrawRichText(MathTextParser.Parse("$\\alpha\\beta$"), new Point(10, 20), new Font { Size = 10 }, TextAlignment.Center);
            return ctx.GetOutput();
        });

        Assert.Equal("<path d=\"Mαβ\" transform=\"translate(10,20) translate(-5,0)\" />\n", svg);
    }

    /// <summary>A binary operator stands between two spaces of 0.2 em of the font set's quad: three runs, the plus that far from the x.</summary>
    [Fact]
    public void ABinaryOperator_IsARunOfItsOwn_ASpaceOfAFifthOfAnEmFromItsNeighbours()
    {
        var font = new Font { Size = 10 };
        var ink = WithStub(_ => new SvgRenderContext().LayOut(TextLabel.Of("$x + y$").Parsed(null).Tree!, font));

        Assert.Equal(["x", "+", "y"], ink.Glyphs.Select(g => g.Text));
        double space = 0.2 * MathConstants.Library.Quad * 10;
        Assert.Equal(ink.Glyphs[0].X + new SvgRenderContext().MeasureText("x", font).Width + space, ink.Glyphs[1].X, 9);
    }

    [Fact]
    public void AnEmptySpan_IsNoRun_AndAddsNoAdvance()
    {
        var font = new Font { Size = 10 };
        var rich = new RichText([new TextSpan("x"), new TextSpan("", TextSpanKind.Superscript, 0.7), new TextSpan("y", TextSpanKind.Subscript, 0.7)]);

        var svg = WithStub(_ =>
        {
            var ctx = new SvgRenderContext();
            ctx.DrawRichText(rich, new Point(10, 20), font, TextAlignment.Left);
            return ctx.GetOutput();
        });

        Assert.Equal(2, svg.Split("<path", StringSplitOptions.None).Length - 1);
        Assert.Contains("d=\"Mx\"", svg);
        Assert.Contains("d=\"My\"", svg);
        Assert.DoesNotContain("<text", svg);
    }

    /// <summary>What waits for the next element goes on the first run the label is drawn as: the next element written.</summary>
    [Fact]
    public void TheDataWaitingForTheNextElement_GoesOnTheFirstRunOfTheLabel()
    {
        var svg = WithStub(_ =>
        {
            var ctx = new SvgRenderContext();
            ctx.SetNextElementData("k", "v");
            ctx.DrawRichText(MathTextParser.Parse("x$^{2}$"), new Point(10, 20), new Font { Size = 10 }, TextAlignment.Left);
            return ctx.GetOutput();
        });

        Assert.StartsWith("<path d=\"Mx\" transform=\"translate(10,20)\" data-k=\"v\" />", svg);
        Assert.Equal(1, svg.Split("data-k", StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void WithoutAGlyphSource_EachRunIsATextElement()
    {
        using var heldOutlines = GlyphPathProviderDefault.Holding(null);
        var ctx = new SvgRenderContext();
        ctx.DrawRichText(MathTextParser.Parse("x$^{2}$"), new Point(10, 20), new Font { Size = 10 }, TextAlignment.Left);

        string svg = ctx.GetOutput();
        Assert.StartsWith("<text x=\"10\" y=\"20\" font-family=\"sans-serif\" font-size=\"10\" text-anchor=\"start\">x</text>", svg);
        Assert.Contains("font-size=\"7\" text-anchor=\"start\">2</text>", svg);
    }
}
