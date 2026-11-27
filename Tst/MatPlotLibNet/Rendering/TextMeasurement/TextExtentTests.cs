// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.TextMeasurement;

/// <summary>The one seam a text is measured by: its extent (advance, height, and how far the letters rise above the baseline),
/// asked of a surface or of a font metrics. Both answer with their box; the letters rise the font size above the baseline.</summary>
public sealed class TextExtentTests
{
    private static readonly Font Font = new() { Size = 10 };

    /// <summary>A text boxed at least as tall as another (matplotlib's <c>TextArea</c> and 'lp', printed with 3.11.2 at 10 points: 'x'
    /// alone 13.889 tall, rising 10.552; 'lp' rises 11; 'x' in a legend 14.337) rises and reaches as far as the higher and the deeper of the
    /// two and keeps its own width; without a floor it is as measured.</summary>
    [Fact]
    public void AtLeast_TakesTheHigherRiseAndTheDeeperDrop_AndKeepsItsWidth()
    {
        var x = new TextExtent(7, 13.889, 10.552);
        var lp = new TextExtent(12, 14.337, 11);

        var boxed = x.AtLeast(lp);

        Assert.Equal(7, boxed.Width);
        Assert.Equal(11, boxed.Ascent, 9);
        Assert.Equal(14.337, boxed.Height, 9);
        Assert.Equal(new TextExtent(7, 20, 15), new TextExtent(7, 20, 15).AtLeast(lp));
        Assert.Equal(x, x.AtLeast(null));
    }

    [Fact]
    public void ALine_IsTheSizeTimesTheLineSpacingTall_AndRisesTheFontSizeAboveItsBaseline()
    {
        var extent = TextExtent.OfLine(40, Font);

        Assert.Equal(new TextExtent(40, 10 * 1.2, 10), extent);
        Assert.Equal(1.2, TextExtent.LineSpacing);
        Assert.Equal(10 * 1.2 - 10, extent.Descent);
        Assert.Equal(new Size(40, 10 * 1.2), extent.Box);
    }

    [Fact]
    public void ABox_RisesTheFontSizeAboveItsBaseline()
    {
        var extent = TextExtent.Of(new Size(40, 13), 10);

        Assert.Equal(new TextExtent(40, 13, 10), extent);
        Assert.Equal(3, extent.Descent);
    }

    [Fact]
    public void TheDefaultMetrics_MeasureALineOfTheSummedWidthsOfTheCharacters()
    {
        var box = new DefaultFontMetrics().Measure("ab", Font);

        double width = (DefaultFontMetrics.GetWidth('a') + DefaultFontMetrics.GetWidth('b')) * Font.Size;
        Assert.Equal(TextExtent.OfLine(width, Font).Box, box);
    }

    [Fact]
    public void AMetrics_IsAskedForItsBox_AndTheLettersRiseTheFontSize()
    {
        IFontMetrics metrics = new TestFontMetrics((_, _) => new(7, 9));

        Assert.Equal(new TextExtent(7, 9, 10), metrics.ExtentOf("Peak", Font));
    }

    [Fact]
    public void ASurface_IsAskedForItsBox_AndTheLettersRiseTheFontSize()
    {
        IRenderContext svg = new SvgRenderContext();
        IRenderContext recording = new RecordingRenderContext();

        Assert.Equal(TextExtent.Of(svg.MeasureText("Peak", Font), Font.Size), svg.ExtentOf(TextLabel.Plain("Peak"), Font));
        Assert.Equal(TextExtent.Of(recording.MeasureText("Peak", Font), Font.Size), recording.ExtentOf(TextLabel.Plain("Peak"), Font));
    }

    [Fact]
    public void ALibrarySurface_MeasuresByTheInstalledMetrics()
    {
        IRenderContext ctx = new SvgRenderContext();

        Assert.Equal(FontMetricsDefault.Current.ExtentOf("Peak", Font), ctx.ExtentOf(TextLabel.Plain("Peak"), Font));
    }

    [Fact]
    public void ACollectingSurface_MeasuresByTheSurfaceItCollectsFor()
    {
        var inner = new RecordingRenderContext();
        IRenderContext ctx = new MarkCollectingContext(inner, new MarkObstacles.Builder());

        Assert.Equal(inner.ExtentOf(TextLabel.Plain("Peak"), Font), ctx.ExtentOf(TextLabel.Plain("Peak"), Font));
    }

    /// <summary>A rich text is measured as the box of its layout: the x, then its superscript one size smaller (7 at 10) a kern of the
    /// set's delta away and raised by at least <c>sup1</c> x-heights, then the space after a script; each run's box rising its font size
    /// and reaching the rest of its line below the baseline (matplotlib's <c>Parser.subsuper</c> on the library's constants).</summary>
    [Fact]
    public void ARichText_IsMeasuredAsTheBoxOfItsLayout()
    {
        IRenderContext ctx = new SvgRenderContext();
        var rich = new RichText([new TextSpan("x"), new TextSpan("2", TextSpanKind.Superscript, 0.7)]);

        var measured = ctx.MeasureRichText(rich, Font);

        var c = MathConstants.Library;
        double xHeight = c.XHeight * Font.Size;
        var x = ctx.ExtentOf(TextLabel.Plain("x"), Font);
        var two = ctx.ExtentOf(TextLabel.Plain("2"), Font with { Size = 7 });
        double up = Math.Max(c.Sup1 * xHeight, two.Descent + xHeight / 4);
        double width = x.Width + c.Delta * xHeight * 0.7 + two.Width + c.ScriptSpace * xHeight;
        double height = Math.Max(x.Ascent, two.Ascent + up) + Math.Max(x.Descent, two.Descent - up);
        Assert.Equal(width, measured.Width, 9);
        Assert.Equal(height, measured.Height, 9);
    }
}
