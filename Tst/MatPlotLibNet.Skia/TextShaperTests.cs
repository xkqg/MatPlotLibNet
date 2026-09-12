// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>One shaper for every text the Skia package measures or draws. The numbers here were measured on the
/// bundled DejaVu Sans at 13 px with HarfBuzz before the shaper existed, so they are the reference, not a print
/// of the implementation: Latin gains kerning, Arabic gains its joined forms, Hebrew reads right to left, and a
/// line that mixes scripts is cut into runs that each shape in their own script.</summary>
public sealed class TextShaperTests
{
    private const float Size = 13f;

    private static ResolvedTypeface DejaVu() =>
        SkiaFonts.Resolve("DejaVu Sans", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);

    private static ShapedText Shape(string text)
    {
        var resolved = DejaVu();
        using var font = new SKFont(resolved.Typeface, Size);
        return TextShaper.Shape(text, font, resolved.Shaper);
    }

    [Fact]
    public void Latin_IsKerned()
    {
        Assert.Equal(13.762f, Shape("Ta").Width, 0.01f);
        Assert.Equal(16.936f, Shape("AV").Width, 0.01f);
    }

    [Fact]
    public void Arabic_IsJoined_AndReadsRightToLeft()
    {
        var shaped = Shape("درجة");

        Assert.Equal(new ushort[] { 5259, 5270, 1374, 1372 }, shaped.Glyphs);
        Assert.Equal(27.092f, shaped.Width, 0.01f);
        Assert.Equal(0f, shaped.Positions[0].X, 0.1f);
        Assert.Equal(7.0f, shaped.Positions[1].X, 0.1f);
        Assert.Equal(15.0f, shaped.Positions[2].X, 0.1f);
        Assert.Equal(21.3f, shaped.Positions[3].X, 0.1f);
    }

    [Fact]
    public void Hebrew_ReadsRightToLeft()
    {
        var shaped = Shape("שלום");

        Assert.Equal(new ushort[] { 1331, 1323, 1330, 1343 }, shaped.Glyphs);
        Assert.Equal(28.793f, shaped.Width, 0.01f);
    }

    [Fact]
    public void LatinThenArabic_ShapesEachRunInItsOwnScript_LatinOnTheLeft()
    {
        var shaped = Shape("Temp درجة");

        // The Latin run, then the joined Arabic forms: the run boundary is where the script changes.
        Assert.Equal(new ushort[] { 55, 72, 80, 83, 3, 5259, 5270, 1374, 1372 }, shaped.Glyphs);
        Assert.Equal(38.797f + 27.092f, shaped.Width, 0.05f);
        Assert.True(shaped.Positions[4].X < shaped.Positions[5].X);
    }

    [Fact]
    public void ArabicThenLatin_PutsTheLatinRunOnTheLeft()
    {
        // A right-to-left paragraph: the Arabic run is visually on the right, so the Latin word that follows it
        // logically comes first in visual order.
        var shaped = Shape("درجة Temp");

        Assert.Equal((ushort)55, shaped.Glyphs[0]); // 'T' is the leftmost glyph
        Assert.Contains((ushort)5259, shaped.Glyphs);
        Assert.Equal(Shape("Temp").Width + Shape(" ").Width + 27.092f, shaped.Width, 0.05f);
    }

    [Fact]
    public void Empty_IsEmpty()
    {
        var shaped = Shape("");

        Assert.Empty(shaped.Glyphs);
        Assert.Empty(shaped.Positions);
        Assert.Equal(0f, shaped.Width);
    }

    [Fact]
    public void SpacesOnly_HaveWidth()
    {
        var shaped = Shape("   ");

        Assert.Equal(3, shaped.Glyphs.Length);
        Assert.True(shaped.Width > 0);
    }

    [Fact]
    public void ASurrogatePair_IsOneGlyph()
    {
        var shaped = Shape("\U0001D538");

        Assert.Single(shaped.Glyphs);
        Assert.Single(shaped.Positions);
    }

    [Fact]
    public void AnOverrideControl_TakesNoRoom_AndReversesWhatItCovers()
    {
        var overridden = Shape("‮ab‬");
        var plain = Shape("ab");

        Assert.Equal(plain.Width, overridden.Width, 0.01f);
        // Visually "b a": the glyph for 'b' (id 69) is left of the glyph for 'a' (68).
        int b = Array.IndexOf(overridden.Glyphs, (ushort)69);
        int a = Array.IndexOf(overridden.Glyphs, (ushort)68);
        Assert.True(overridden.Positions[b].X < overridden.Positions[a].X);
    }

    [Fact]
    public void AZeroWidthJoiner_StaysInsideItsRun()
    {
        var joined = Shape("ש‍ל");
        var plain = Shape("של");

        Assert.Equal(plain.Width, joined.Width, 0.01f);
    }

    [Fact]
    public void Digits_InsideArabic_KeepTheirOrder()
    {
        // "12" inside Arabic text is a left-to-right run at level 2: '1' left of '2', both inside the reversed
        // Arabic run.
        var shaped = Shape("درجة 12");
        int one = Array.IndexOf(shaped.Glyphs, (ushort)20);
        int two = Array.IndexOf(shaped.Glyphs, (ushort)21);

        Assert.True(one >= 0 && two >= 0);
        Assert.True(shaped.Positions[one].X < shaped.Positions[two].X);
        Assert.True(shaped.Positions[two].X < shaped.Positions[Array.IndexOf(shaped.Glyphs, (ushort)5259)].X);
    }

    [Fact]
    public void OneShaper_ServesTwoThreadsAtOnce()
    {
        var resolved = DejaVu();
        var texts = new[] { "Revenue", "درجة الحرارة", "שלום", "12.5", "Quarter 3, 2026" };
        var expected = texts.Select(t => { using var f = new SKFont(resolved.Typeface, Size); return TextShaper.Shape(t, f, resolved.Shaper); }).ToArray();
        int defects = 0;

        Parallel.For(0, 4, _ =>
        {
            using var font = new SKFont(resolved.Typeface, Size);
            for (int i = 0; i < 2000; i++)
            {
                var shaped = TextShaper.Shape(texts[i % texts.Length], font, resolved.Shaper);
                var reference = expected[i % texts.Length];
                if (Math.Abs(shaped.Width - reference.Width) > 0.001f || !shaped.Glyphs.SequenceEqual(reference.Glyphs))
                {
                    Interlocked.Increment(ref defects);
                }
            }
        });

        Assert.Equal(0, defects);
    }
}
