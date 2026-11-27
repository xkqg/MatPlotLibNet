// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>A text is drawn and measured as a <see cref="TextLabel"/>: one value that decides once, when it is made, whether the
/// text is math (<c>$...$</c>) and holds the tree it parses to. A math label is laid out once and drawn as the runs of its layout
/// through the surface's public <c>DrawText</c> (and its rules through <c>DrawRectangle</c>), measured as the box of that layout; any
/// other label is plain text. The call the verb makes is the one the caller would have made, so a context that draws a turned text by
/// its own overload sees that overload, and no surface is asked for <c>DrawRichText</c> or <c>MeasureRichText</c>.</summary>
public sealed class TextLabelTests
{
    private static readonly Font Font = new() { Size = 12 };

    private static readonly Point At = new(1, 2);

    [Fact]
    public void APlainText_IsNotMath_AndParsesToNoTree()
    {
        var label = TextLabel.Of("price");

        Assert.False(label.IsMath);
        Assert.Null(label.Parsed(null).Tree);
        Assert.Equal("price", label.Text);
    }

    /// <summary>Making a label decides whether it is math and parses nothing: the tree is read when the label is drawn or measured.</summary>
    [Fact]
    public void ATextWithMathMarkup_IsMath_AndParsesToItsTreeWhenItIsRead()
    {
        var label = TextLabel.Of("$\\alpha$ rate");

        Assert.True(label.IsMath);
        Assert.Null(label.Tree);
        Assert.Equal("$\\alpha$ rate", label.Text);
        Assert.Equal(MathTextParser.Parse("$\\alpha$ rate").Spans, MathSpanWriter.Write(label.Parsed(null).Tree!).Spans);
    }

    [Fact]
    public void ADollarSignAlone_IsNotMath()
    {
        Assert.False(TextLabel.Of("$5").IsMath);
    }

    [Fact]
    public void APlainLabel_IsNeverMath_WhateverItHolds()
    {
        var label = TextLabel.Plain("$x$");

        Assert.False(label.IsMath);
        Assert.Null(label.Parsed(new KeptMathTexts()).Tree);
        Assert.Equal("$x$", label.Text);
    }

    /// <summary>A rich text a surface was handed is drawn by the tree it was written from; one made by hand is read into one. Parsing it
    /// again reads nothing.</summary>
    [Fact]
    public void ARichTextsLabel_HoldsTheTreeTheRichTextWasWrittenFrom()
    {
        var parsed = MathTextParser.Parse("$x^2$");
        var label = TextLabel.OfRich(parsed);

        Assert.Same(parsed.Box, label.Tree);
        Assert.Same(parsed.Box, label.Parsed(new KeptMathTexts()).Tree);
        Assert.Equal("x2", label.Text);
        Assert.NotNull(TextLabel.OfRich(new RichText([new TextSpan("x")])).Tree);
    }

    /// <summary>Without texts kept for it, a math label is parsed each time it is read; a label already parsed is not parsed again.</summary>
    [Fact]
    public void AMathLabelWithoutKeptTexts_IsParsedEachTimeItIsRead()
    {
        var label = TextLabel.Of("$x^2$");
        var parsed = label.Parsed(null);

        Assert.NotSame(parsed.Tree, label.Parsed(null).Tree);
        Assert.Equal(parsed, parsed.Parsed(null));
    }

    /// <summary>A plain text, and a label made plain on purpose whose text looks like math, are drawn as the text they hold.</summary>
    [Theory]
    [InlineData(true, "price")]
    [InlineData(false, "$x$")]
    public void APlainText_IsDrawnAsText(bool read, string text)
    {
        var ctx = new OverloadContext();

        ctx.Draw(read ? TextLabel.Of(text) : TextLabel.Plain(text), Font, new TextPlacement(At, TextAlignment.Center));

        Assert.Equal(["DrawText:" + text], ctx.Calls);
    }

    /// <summary>The runs of the layout, each measured and then drawn left-aligned where the layout put it: the alpha (one character
    /// wide on this surface), then the text after it.</summary>
    [Fact]
    public void AMathText_IsDrawnAsTheRunsOfItsLayout()
    {
        var ctx = new OverloadContext();

        ctx.Draw(TextLabel.Of("$\\alpha$ rate"), Font, new TextPlacement(At, TextAlignment.Left));

        Assert.Equal(["MeasureText:α", "MeasureText: rate", "DrawText:α", "DrawText: rate"], ctx.Calls);
        Assert.Equal([new Point(1, 2), new Point(2, 2)], ctx.Positions);
    }

    /// <summary>A layout of one run on the baseline is drawn as a plain text is, by the alignment asked: the surface places it by its own
    /// measure of the run.</summary>
    [Fact]
    public void ATurnedText_IsDrawnThroughTheTurnedOverload()
    {
        var ctx = new OverloadContext();
        var turned = new TextPlacement(At, TextAlignment.Center) { Rotation = 90 };

        ctx.Draw(TextLabel.Of("price"), Font, turned);
        ctx.Draw(TextLabel.Of("$x$"), Font, turned);

        Assert.Equal(["DrawText@90:price", "MeasureText:x", "DrawText@90:x"], ctx.Calls);
        Assert.Equal([At, At], ctx.Positions);
    }

    /// <summary>A turned label of several runs: each run drawn turned about its own start, which stands where the turned layout puts
    /// it (turned 90 degrees counter-clockwise, the second run one character up the picture from the first).</summary>
    [Fact]
    public void ATurnedMathText_DrawsEachRunWhereTheTurnedLayoutPutsIt()
    {
        var ctx = new OverloadContext();

        ctx.Draw(TextLabel.Of("$\\alpha$ rate"), Font, new TextPlacement(At, TextAlignment.Left) { Rotation = 90 });

        Assert.Equal(["MeasureText:α", "MeasureText: rate", "DrawText@90:α", "DrawText@90: rate"], ctx.Calls);
        Assert.Equal(1, ctx.Positions[1].X, 9);
        Assert.Equal(1, ctx.Positions[1].Y, 9);
    }


    /// <summary>A math text is measured as the box of its layout, each run measured by the surface: one character wide and the font size
    /// tall here, at least one line of the font (1.2 font sizes, the letters rising the font size).</summary>
    [Fact]
    public void APlainText_IsMeasuredAsText_AndAMathTextAsTheBoxOfItsLayout()
    {
        var ctx = new OverloadContext();

        var plain = ctx.ExtentOf(TextLabel.Of("price"), Font);
        var math = ctx.ExtentOf(TextLabel.Of("$x$"), Font);

        Assert.Equal(new TextExtent(5, 12, 12), plain);
        Assert.Equal(new TextExtent(1, 12 * TextExtent.LineSpacing, 12), math);
        Assert.Equal(["MeasureText:price", "MeasureText:x"], ctx.Calls);
    }

    /// <summary>The font metrics a layout is handed measure a text as the surface draws it: a math text as the box of its layout, any
    /// other as written, each as the box of the extent the surface's own measure gives.</summary>
    [Fact]
    public void TheSurfaceFontMetrics_MeasureAPlainTextAsTextAndAMathTextAsItsLayout()
    {
        var ctx = new OverloadContext();
        IFontMetrics metrics = new SurfaceFontMetrics(ctx);

        var plain = metrics.Measure("price", Font);
        var math = metrics.Measure("$x$", Font);

        Assert.Equal(new Size(5, 12), plain);
        Assert.Equal(new Size(1, 12 * TextExtent.LineSpacing), math);
        Assert.Equal(["MeasureText:price", "MeasureText:x"], ctx.Calls);
    }

    /// <summary>A text that carries a level is drawn as any other on a surface that writes no markup: the level is the surface's to
    /// state, and one that has no markup has nothing to state it in.</summary>
    [Theory]
    [InlineData("row", "DrawText:row")]
    [InlineData("$x$", "MeasureText:x;DrawText:x")]
    public void ALeveledText_IsDrawnAsAnyOtherTextOnASurfaceWithoutMarkup(string text, string calls)
    {
        var ctx = new OverloadContext();

        ctx.Draw(TextLabel.Of(text), Font, new TextPlacement(At, TextAlignment.Left) { Level = 2 });

        Assert.Equal(calls.Split(';'), ctx.Calls);
    }

    /// <summary>A text placed by its vertical alignment is drawn where its box puts the baseline (<see cref="TextBox"/>): the verb
    /// measures it as the surface does and no site works out a drop of its own. One on its baseline is drawn at its anchor, unmeasured.</summary>
    [Theory]
    [InlineData(TextVerticalAlignment.Baseline, 2.0)]
    [InlineData(TextVerticalAlignment.Center, 6.2)]    // 0.35 of the ascent (the font size) below the anchor
    [InlineData(TextVerticalAlignment.Top, 14.0)]      // the ascent below the anchor
    [InlineData(TextVerticalAlignment.Bottom, 2.0)]    // the descent above: the measure is one font size tall, all of it ascent
    public void AVerticallyAlignedText_IsDrawnWhereItsBoxPutsTheBaseline(TextVerticalAlignment vertical, double baselineY)
    {
        var ctx = new OverloadContext();

        ctx.Draw(TextLabel.Of("price"), Font, new TextPlacement(At, TextAlignment.Center) { Vertical = vertical });

        Assert.Equal(1, ctx.Positions.Single().X);
        Assert.Equal(baselineY, ctx.Positions.Single().Y, 1e-9);
        // Only a text standing on its box's foot reads the height of its measure; a centred or hung plain text rises the font size on a
        // surface that measures a box, so its advance is not measured.
        Assert.Equal(vertical == TextVerticalAlignment.Bottom ? ["MeasureText:price", "DrawText:price"] : ["DrawText:price"], ctx.Calls);
    }

    /// <summary>A hung math text is measured as its layout (to place the baseline) and drawn by the same layout, laid out once.</summary>
    [Fact]
    public void AVerticallyAlignedMathText_IsMeasuredAsItsLayout()
    {
        var ctx = new OverloadContext();

        ctx.Draw(TextLabel.Of("$x$"), Font, new TextPlacement(At, TextAlignment.Center) { Vertical = TextVerticalAlignment.Top });

        Assert.Equal(["MeasureText:x", "DrawText:x"], ctx.Calls);
        Assert.Equal(new Point(1, 14), ctx.Positions.Single());
    }

    /// <summary>A caller that measured the text already hands the extent in: the verb draws by it and measures nothing.</summary>
    [Fact]
    public void AnExtentHandedIn_IsWhatTheBaselineIsPutBy_AndNothingIsMeasured()
    {
        var ctx = new OverloadContext();

        ctx.Draw(TextLabel.Of("price"), Font, new TextPlacement(At, TextAlignment.Left) { Vertical = TextVerticalAlignment.Top, Rotation = 30 },
            new TextExtent(5, 12, 9));

        Assert.Equal(["DrawText@30:price"], ctx.Calls);
        Assert.Equal(new Point(1, 11), ctx.Positions.Single());
    }

    [Fact]
    public void ALineOfAFont_IsMeasuredByADigit_OnTheSurfaceTheLabelsAre()
    {
        var ctx = new OverloadContext();

        var line = ctx.LineExtentOf(Font);

        Assert.Equal(ctx.ExtentOf(TextLabel.Plain("0"), Font), line);
        Assert.Equal(["MeasureText:0", "MeasureText:0"], ctx.Calls);
    }

    /// <summary>Names every text call by the overload it reached. It implements no rich-text member: a surface that draws only text
    /// draws math.</summary>
    private sealed class OverloadContext : SilentRenderContext, IRenderContext
    {
        public List<string> Calls { get; } = [];

        public List<Point> Positions { get; } = [];

        public override void DrawText(string text, Point position, Font font, TextAlignment alignment)
        {
            Positions.Add(position);
            Calls.Add($"DrawText:{text}");
        }

        public void DrawText(string text, Point position, Font font, TextAlignment alignment, double rotation)
        {
            Positions.Add(position);
            Calls.Add($"DrawText@{rotation}:{text}");
        }

        public override Size MeasureText(string text, Font font)
        {
            Calls.Add($"MeasureText:{text}");
            return new Size(text.Length, font.Size);
        }
    }
}
