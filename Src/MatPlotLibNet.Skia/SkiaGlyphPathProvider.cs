// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia;

/// <summary>
/// Skia-backed <see cref="IGlyphPathProvider"/>: shapes the text with the package's shaper, takes each glyph's
/// outline from <c>SKFont.GetGlyphPath</c> at the position the shaper gave it, and returns the outlines joined as SVG path
/// data with every number written to two decimals, as every other coordinate of the SVG is. This gives the SVG backend
/// vector shapes that match exactly what Skia draws into the PNG/PDF outputs — the same kerning, the same joined Arabic forms, the same right-to-left order — with
/// no browser-font dependency and no metric divergence.
/// </summary>
/// <remarks>
/// Registered on <see cref="global::MatPlotLibNet.GlyphPathProviderDefault.Current"/> by
/// <see cref="FigureSkiaExtensions.Initialize"/> at module load. Uses the same typeface resolution
/// as <see cref="SkiaRenderContext"/>, so measurements and outlines come
/// from the identical font.
/// </remarks>
public sealed class SkiaGlyphPathProvider : IGlyphPathProvider, IShapedGlyphPathProvider
{
    /// <summary>Outlines texts with a store of shapings of its own.</summary>
    public SkiaGlyphPathProvider() : this(new ShapedTextStore()) { }

    /// <summary>Outlines texts from the shapings kept in <paramref name="shaped"/>.</summary>
    internal SkiaGlyphPathProvider(ShapedTextStore shaped) => Shaped = shaped;

    /// <summary>The shapings the outlines are made from.</summary>
    internal ShapedTextStore Shaped { get; }

    /// <inheritdoc />
    /// <remarks>The same text in the same face at the same size is shaped and outlined once: every later ask hands back the
    /// string the first one made. Shaping a label and writing its outline again on every draw was 45 % of a frame while a
    /// 3-D surface was turned beside three panels.</remarks>
    public string? GetPathData(string text, Font font) =>
        string.IsNullOrEmpty(text) ? null : Shaped.Of(text, font).Outline;

    /// <summary>The outline and the advance of the one shaping kept for <paramref name="text"/>, so the vector writer places a
    /// centred or right-aligned text by the width its glyphs were laid out with, without measuring it again. The face is resolved
    /// apart from the size, as the PNG paints a piece of a rich text.</summary>
    ShapedOutline? IShapedGlyphPathProvider.GetOutline(string text, SpanFont font)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var run = Shaped.Of(text, SkiaFonts.Resolve(font.Face), (float)font.Size);
        return new ShapedOutline(run.Outline, run.Width);
    }

    /// <summary>Returns the total advance width of <paramref name="text"/> in <paramref name="font"/> — the same
    /// shaped width <see cref="SkiaFontMetrics"/> measures and the glyph outlines span.</summary>
    public static double MeasureAdvance(string text, Font font)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var resolved = SkiaFonts.Resolve(font);
        using var skFont = new SKFont(resolved.Typeface, (float)font.Size);
        return TextShaper.Shape(text, skFont, resolved.Shaper).Width;
    }
}
