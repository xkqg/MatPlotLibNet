// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The faces of matplotlib's math font sets ship inside the package, as matplotlib ships them in mpl-data/fonts/ttf: a chart
/// whose theme sets math in Computer Modern draws it in those faces on every machine, none of which has them installed.</summary>
[Collection("SkiaFontsGlobalState")]
public sealed class MathFontSetFacesTests
{
    public MathFontSetFacesTests() => SkiaFonts.ResetForTests();

    [Theory]
    [InlineData("cmr10")]
    [InlineData("cmmi10")]
    [InlineData("cmti10")]
    [InlineData("cmb10")]
    [InlineData("cmsy10")]
    [InlineData("cmex10")]
    [InlineData("cmtt10")]
    [InlineData("DejaVu Sans Display")]
    [InlineData("STIXGeneral")]
    [InlineData("STIXNonUnicode")]
    [InlineData("STIXSizeOneSym")]
    [InlineData("STIXSizeTwoSym")]
    [InlineData("STIXSizeThreeSym")]
    [InlineData("STIXSizeFourSym")]
    public void AMathFace_ResolvesToTheBundledFace_WithNoDiagnostic(string family)
    {
        using var capture = new DiagnosticsCapture();
        var resolved = SkiaFonts.Resolve(new Font { Family = family });

        Assert.Equal(family, resolved.Typeface.FamilyName);
        Assert.Empty(capture.Seen);
    }

    [Theory]
    [InlineData("STIXGeneral", FontWeight.Normal, FontSlant.Normal)]
    [InlineData("STIXGeneral", FontWeight.Normal, FontSlant.Italic)]
    [InlineData("STIXGeneral", FontWeight.Bold, FontSlant.Normal)]
    [InlineData("STIXNonUnicode", FontWeight.Normal, FontSlant.Normal)]
    [InlineData("STIXNonUnicode", FontWeight.Normal, FontSlant.Italic)]
    public void ASTIXFace_ResolvesToTheBundledFaceOfItsWeightAndSlant(string family, FontWeight weight, FontSlant slant)
    {
        // The faces StixFallback names (family, weight, slant) are the five files the package embeds, each its own typeface.
        var resolved = SkiaFonts.Resolve(new Font { Family = family, Weight = weight, Slant = slant });

        Assert.Equal(family, resolved.Typeface.FamilyName);
        Assert.Equal(weight == FontWeight.Bold, resolved.Typeface.FontStyle.Weight >= (int)SKFontStyleWeight.SemiBold);
        Assert.Equal(slant == FontSlant.Italic, resolved.Typeface.FontStyle.Slant != SKFontStyleSlant.Upright);
    }

    private static byte[] Paint(Action<SkiaRenderContext> draw)
    {
        using var bitmap = new SKBitmap(160, 90);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            draw(new SkiaRenderContext(canvas));
        }
        return bitmap.Bytes;
    }

    [Fact]
    public void UnderTheClassicTheme_MathIsPaintedInComputerModern()
    {
        // matplotlib 3.11.2, plt.style.use('classic'): "$x$" is cmmi10 0x78.
        var font = new Font { Family = "DejaVu Sans", Size = 26 };

        var math = Paint(ctx =>
        {
            ctx.UseLooksOf(Theme.MatplotlibClassic);
            ctx.DrawRichText(MathTextParser.Parse("$x$"), new Point(20, 60), font, TextAlignment.Left);
        });
        var cm = Paint(ctx => ctx.DrawText("x", new Point(20, 60), font with { Family = "cmmi10" }, TextAlignment.Left));

        Assert.Equal(cm, math);
    }

    [Fact]
    public void UnderTheV2Theme_ALetterInMathIsPaintedInDejaVuSansOblique()
    {
        // matplotlib 3.11.2, default style: "$x$" is DejaVu Sans Oblique 0x78.
        var font = new Font { Family = "DejaVu Sans", Size = 26 };

        var math = Paint(ctx =>
        {
            ctx.UseLooksOf(Theme.MatplotlibV2);
            ctx.DrawRichText(MathTextParser.Parse("$x$"), new Point(20, 60), font, TextAlignment.Left);
        });
        var oblique = Paint(ctx => ctx.DrawText("x", new Point(20, 60), font with { Slant = FontSlant.Italic }, TextAlignment.Left));

        Assert.Equal(oblique, math);
    }

    [Fact]
    public void UnderTheClassicTheme_ACharacterComputerModernLacks_IsPaintedInSTIX()
    {
        // matplotlib 3.11.2, plt.style.use('classic'): "$\partial$" is STIXGeneral 0x2202 (BakomaFonts falls back to StixFonts).
        var font = new Font { Family = "DejaVu Sans", Size = 26 };

        var math = Paint(ctx =>
        {
            ctx.UseLooksOf(Theme.MatplotlibClassic);
            ctx.DrawRichText(MathTextParser.Parse(@"$\partial$"), new Point(20, 60), font, TextAlignment.Left);
        });
        var stix = Paint(ctx => ctx.DrawText("\u2202", new Point(20, 60), font with { Family = "STIXGeneral" }, TextAlignment.Left));
        var label = Paint(ctx => ctx.DrawText("\u2202", new Point(20, 60), font, TextAlignment.Left));

        Assert.Equal(stix, math);
        Assert.NotEqual(label, math); // not the label's face: the picture is the STIX glyph's own
    }

    [Fact]
    public void UnderTheV2Theme_ACalligraphicCapital_IsPaintedInSTIXNonUnicode()
    {
        // matplotlib 3.11.2, default style: "$\mathcal{A}$" is STIXNonUnicode Italic 0xe22d (DejaVuFonts falls back to StixSansFonts).
        var font = new Font { Family = "DejaVu Sans", Size = 26 };

        var math = Paint(ctx =>
        {
            ctx.UseLooksOf(Theme.MatplotlibV2);
            ctx.DrawRichText(MathTextParser.Parse(@"$\mathcal{A}$"), new Point(20, 60), font, TextAlignment.Left);
        });
        var stix = Paint(ctx => ctx.DrawText("\uE22D", new Point(20, 60), font with { Family = "STIXNonUnicode", Slant = FontSlant.Italic }, TextAlignment.Left));
        var label = Paint(ctx => ctx.DrawText("A", new Point(20, 60), font, TextAlignment.Left));

        Assert.Equal(stix, math);
        Assert.NotEqual(label, math); // not the label's capital A: the picture is the script capital's own
    }

    [Fact]
    public void UnderALibraryTheme_MathIsPaintedInTheLabelsFace()
    {
        var font = new Font { Family = "DejaVu Sans", Size = 26 };

        var math = Paint(ctx => ctx.DrawRichText(MathTextParser.Parse("$x$"), new Point(20, 60), font, TextAlignment.Left));
        var plain = Paint(ctx => ctx.DrawText("x", new Point(20, 60), font, TextAlignment.Left));

        Assert.Equal(plain, math);
    }
}
