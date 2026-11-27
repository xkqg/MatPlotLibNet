// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A rich text (a label with math in it) is painted with the boxes of its layout in the order the bidi algorithm puts the
/// whole label in (one paragraph over all of them, as a viewer resolves one text). A Hebrew word with a superscript after it reads right to left, and the superscript, which comes
/// last, stands at the left end. A label with nothing right to left in it is painted span after span, as it is written.</summary>
public class RichTextBidiOrderTests
{
    private static readonly Font Font26 = new() { Family = "DejaVu Sans", Size = 26 };

    private const float Baseline = 60;

    private static SKBitmap Render(string label)
    {
        var bitmap = new SKBitmap(300, 90);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        new SkiaRenderContext(canvas).DrawRichText(MathTextParser.Parse(label), new Point(20, Baseline), Font26, TextAlignment.Left);
        return bitmap;
    }

    /// <summary>The leftmost and rightmost columns holding ink in the rows <paramref name="fromY"/> to <paramref name="toY"/>.</summary>
    private static (int Left, int Right) InkColumns(SKBitmap bitmap, int fromY, int toY)
    {
        int left = int.MaxValue, right = -1;
        for (int x = 0; x < bitmap.Width; x++)
            for (int y = fromY; y <= toY; y++)
                if (bitmap.GetPixel(x, y).Red < 128)
                {
                    left = Math.Min(left, x);
                    right = Math.Max(right, x);
                }
        return (left, right);
    }

    // At 26 px the letters without ascenders (shin, bet, tav; x) stand about 19 px above the baseline, and a superscript is raised
    // by at least sup1 x-heights of the library's constants (about 10.7 px), so the rows just above the baseline hold only the word
    // and the rows above its letters hold only the superscript.
    private static (int Left, int Right) Word(SKBitmap bitmap) => InkColumns(bitmap, (int)Baseline - 8, (int)Baseline - 1);

    private static (int Left, int Right) Superscript(SKBitmap bitmap) => InkColumns(bitmap, (int)Baseline - 26, (int)Baseline - 21);

    [Fact]
    public void AHebrewWordWithASuperscript_ReadsRightToLeft_SoTheSuperscriptStandsLeftOfTheWord()
    {
        using var picture = Render("שבת$^{2}$");

        var word = Word(picture);
        var superscript = Superscript(picture);

        Assert.True(superscript.Right >= 0 && word.Right >= 0, "nothing was painted");
        Assert.True(superscript.Right < word.Left, $"the superscript spans {superscript} and the word {word}");
    }

    [Fact]
    public void ALatinLetterWithASuperscript_IsPaintedAsWritten_TheSuperscriptRightOfTheLetter()
    {
        using var picture = Render("x$^{2}$");

        Assert.True(Superscript(picture).Left > Word(picture).Right);
    }

    [Fact]
    public void ALatinLabelWithAHebrewWordAndASuperscript_PutsTheSuperscriptBetweenTheLatinAndTheHebrew()
    {
        // "x שבת" then a superscript 2: a left-to-right paragraph (x decides), the Hebrew word at level 1 and the 2 after it at level 2
        // (rule I1), so rule L2 turns the two round: x, then the 2, then the word. The first span is cut where its level changes.
        using var picture = Render("x שבת$^{2}$");

        var word = Word(picture);
        var superscript = Superscript(picture);

        Assert.True(word.Left < superscript.Left, $"the x does not stand left of the superscript: {word} and {superscript}");
        Assert.True(word.Right > superscript.Right, $"the Hebrew word does not stand right of the superscript: {word} and {superscript}");
    }

    [Fact]
    public void AHebrewWordWithASuperscript_IsAsWideAsItsSpans()
    {
        // The order changes where each span stands, never the label's width: a right-aligned label ends on its point.
        using var bitmap = new SKBitmap(300, 90);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        new SkiaRenderContext(canvas).DrawRichText(MathTextParser.Parse("שבת$^{2}$"), new Point(280, Baseline), Font26, TextAlignment.Right);

        var all = InkColumns(bitmap, 0, bitmap.Height - 1);
        Assert.InRange(all.Right, 270, 280);
        Assert.True(Word(bitmap).Right > Superscript(bitmap).Right);
    }
}
