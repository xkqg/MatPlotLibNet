// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The PNG paints a font switch the caller wrote (<c>\mathbf</c>, <c>\mathit</c>) in that face of the label's family: a
/// value the caller gave wins under every theme, the library's own included.</summary>
public sealed class RichTextFontSwitchTests
{
    private static readonly Font Font26 = new() { Family = "DejaVu Sans", Size = 26 };

    private static byte[] Painted(RichText text)
    {
        using var bitmap = new SKBitmap(160, 90);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            new SkiaRenderContext(canvas).DrawRichText(text, new Point(20, 60), Font26, TextAlignment.Left);
        }
        return bitmap.Bytes;
    }

    private static byte[] PaintedPlain(Font font)
    {
        using var bitmap = new SKBitmap(160, 90);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            new SkiaRenderContext(canvas).DrawText("yz", new Point(20, 60), font, TextAlignment.Left);
        }
        return bitmap.Bytes;
    }

    [Fact]
    public void ABoldSwitch_IsPaintedInTheBoldFace()
    {
        Assert.Equal(PaintedPlain(Font26 with { Weight = FontWeight.Bold }), Painted(MathTextParser.Parse(@"$\mathbf{yz}$")));
    }

    [Fact]
    public void AnItalicSwitch_IsPaintedInTheItalicFace()
    {
        Assert.Equal(PaintedPlain(Font26 with { Slant = FontSlant.Italic }), Painted(MathTextParser.Parse(@"$\mathit{yz}$")));
    }

    [Fact]
    public void MathWithoutASwitch_IsPaintedInTheLabelsFace_UnderTheLibrarysOwnSet()
    {
        Assert.Equal(PaintedPlain(Font26), Painted(MathTextParser.Parse("$yz$")));
    }
}
