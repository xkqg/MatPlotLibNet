// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A glyph outline in an SVG is written as every other coordinate of the drawing is: to two decimals. Skia's own path
/// string carries six significant digits, about a third of the characters of a label's outline and of the document that holds
/// it, for a precision a hundredth of a pixel is far below.</summary>
public sealed partial class GlyphOutlineDigitsTests
{
    private static readonly Font Font13 = new() { Family = "DejaVu Sans", Size = 13 };

    [GeneratedRegex(@"-?\d+(\.\d+)?")]
    private static partial Regex Number();

    private static string Write(Action<SKPath> build, float dx = 0, float dy = 0)
    {
        using var path = new SKPath();
        build(path);
        return ShapedRun.AppendSvgPathData(new StringBuilder(), path, dx, dy).ToString();
    }

    [Fact]
    public void TheOutlineOfALabel_HasNoNumberPastTwoDecimals()
    {
        string outline = new SkiaGlyphPathProvider().GetPathData("Latency (ms) 0.25 -12.5", Font13)!;

        var numbers = Number().Matches(outline);
        Assert.True(numbers.Count > 100);
        Assert.All(numbers, m => Assert.True(m.Groups[1].Value.Length <= 3, $"'{m.Value}' has more than two decimals"));
    }

    /// <summary>The outline Skia's own path string gave before: the glyphs joined at their shaped positions, then written by Skia.</summary>
    private static string SkiasOwnOutline(string text, Font font)
    {
        var face = SkiaFonts.Resolve(font);
        using var skFont = new SKFont(face.Typeface, (float)font.Size);
        var shaped = TextShaper.Shape(text, skFont, face.Shaper);
        using var composite = new SKPath();
        for (int i = 0; i < shaped.Glyphs.Length; i++)
        {
            using var glyph = skFont.GetGlyphPath(shaped.Glyphs[i]);
            if (glyph is { IsEmpty: false }) composite.AddPath(glyph, shaped.Positions[i].X, shaped.Positions[i].Y, SKPathAddMode.Append);
        }
        return composite.ToSvgPathData();
    }

    [Fact]
    public void TheOutlineOfALabel_IsSkiasOwnPath_ToAHundredthOfAPixel()
    {
        const string text = "Wq (ms) 0.5";
        string written = new SkiaGlyphPathProvider().GetPathData(text, Font13)!;
        string skia = SkiasOwnOutline(text, Font13);

        // Skia writes a closing line before a Z when a contour does not end where it began; the Z closes it alone, so the
        // written outline leaves that line out. What stays equal is every command and every number of the rest.
        var ours = written.Split('M', StringSplitOptions.RemoveEmptyEntries);
        Assert.NotEmpty(ours);

        var skiaNumbers = Number().Matches(Regex.Replace(skia, @"[A-Za-z]", " ")).Select(m => double.Parse(m.Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        var writtenNumbers = Number().Matches(written).Select(m => double.Parse(m.Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        Assert.True(writtenNumbers.Length <= skiaNumbers.Length);
        Assert.True(writtenNumbers.Length > skiaNumbers.Length - 2 * ours.Length);
        // Every written number is within half a hundredth of a number Skia wrote at the same place.
        int at = 0;
        foreach (double w in writtenNumbers)
        {
            while (at < skiaNumbers.Length && Math.Abs(skiaNumbers[at] - w) > 0.0051) at++;
            Assert.True(at < skiaNumbers.Length, $"{w} has no counterpart in Skia's path");
            at++;
        }
    }

    [Fact]
    public void ALineAMoveAndAClose_AreWrittenAsSkiaWritesThem_WhenTheNumbersAreHundredths()
    {
        string data = Write(p => { p.MoveTo(1.5f, 2.25f); p.LineTo(3, 4.5f); p.LineTo(-1.75f, 0); p.Close(); });

        Assert.Equal("M1.5 2.25L3 4.5L-1.75 0Z", data);
    }

    [Fact]
    public void AQuadAndACubic_AreWrittenWithTheirControlPoints()
    {
        string data = Write(p => { p.MoveTo(0, 0); p.QuadTo(1, 2, 3, 0); p.CubicTo(4, 1, 5, 1, 6, 0); });

        Assert.Equal("M0 0Q1 2 3 0C4 1 5 1 6 0", data);
    }

    [Fact]
    public void TheOffset_MovesEveryPointOfTheSegments()
    {
        string data = Write(p => { p.MoveTo(0, 0); p.QuadTo(1, 2, 3, 0); p.CubicTo(4, 1, 5, 1, 6, 0); p.LineTo(7, 7); }, dx: 10, dy: -1);

        Assert.Equal("M10 -1Q11 1 13 -1C14 0 15 0 16 -1L17 6", data);
    }

    [Fact]
    public void ANumber_IsRoundedHalfAwayFromZero_ToAHundredth()
    {
        string data = Write(p => { p.MoveTo(1.234f, -5.678f); p.LineTo(0.004f, -0.004f); });

        Assert.Equal("M1.23 -5.68L0 0", data);
    }

    [Fact]
    public void AConic_IsWrittenAsQuads_AsSkiaWritesIt()
    {
        string data = Write(p => { p.MoveTo(0, 0); p.ConicTo(10, 0, 10, 10, 0.70710678f); });

        Assert.DoesNotContain("C", data);
        Assert.StartsWith("M0 0Q", data);
        Assert.EndsWith(" 10 10", data);
        Assert.True(data.Count(c => c == 'Q') >= 2);
    }

    [Fact]
    public void AnEmptyPath_WritesNothing()
    {
        Assert.Equal("", Write(_ => { }));
    }

    [Fact]
    public void TheWriter_AppendsToWhatTheBuilderHolds()
    {
        using var path = new SKPath();
        path.MoveTo(1, 1);

        string data = ShapedRun.AppendSvgPathData(new StringBuilder("x"), path, 0, 0).ToString();

        Assert.Equal("xM1 1", data);
    }

    /// <summary>The builder an outline is written into is kept for the next one, and let go when a very long text made it large: the
    /// outline after such a text is still the outline of its own text.</summary>
    [Fact]
    public void AnOutline_AfterATextThatGrewTheBuilderPastWhatIsKept_IsStillItsOwn()
    {
        var provider = new SkiaGlyphPathProvider();
        string before = provider.GetPathData("Ta", Font13)!;
        string huge = provider.GetPathData(new string('W', 1500), Font13)!;
        Assert.True(huge.Length > 64 * 1024);

        string after = new SkiaGlyphPathProvider().GetPathData("Ta", Font13)!;

        Assert.Equal(before, after);
    }
}
