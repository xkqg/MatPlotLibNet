// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia;

/// <summary>
/// Skia-backed <see cref="IGlyphPathProvider"/>: shapes the text with <see cref="TextShaper"/>, takes each glyph's
/// outline from <c>SKFont.GetGlyphPath</c> at the position the shaper gave it, and returns the composite through
/// <see cref="SKPath.ToSvgPathData"/>. This gives the SVG backend vector shapes that match exactly what Skia draws
/// into the PNG/PDF outputs — the same kerning, the same joined Arabic forms, the same right-to-left order — with
/// no browser-font dependency and no metric divergence.
/// </summary>
/// <remarks>
/// Registered on <see cref="global::MatPlotLibNet.ChartServices.GlyphPathProvider"/> by
/// <see cref="FigureSkiaExtensions.Initialize"/> at module load. Uses the same typeface resolution
/// (<see cref="SkiaFonts.Resolve(Font)"/>) as <see cref="SkiaRenderContext"/>, so measurements and outlines come
/// from the identical font.
/// </remarks>
public sealed class SkiaGlyphPathProvider : IGlyphPathProvider
{
    /// <inheritdoc />
    public string? GetPathData(string text, Font font)
    {
        if (string.IsNullOrEmpty(text)) return null;

        var resolved = SkiaFonts.Resolve(font);
        using var skFont = new SKFont(resolved.Typeface, (float)font.Size);
        var shaped = TextShaper.Shape(text, skFont, resolved.Shaper);

        using var composite = new SKPath();
        for (int i = 0; i < shaped.Glyphs.Length; i++)
        {
            using var glyphPath = skFont.GetGlyphPath(shaped.Glyphs[i]);
            if (glyphPath is { IsEmpty: false })
            {
                // Glyph outlines from Skia are positioned with their origin at (0, 0) on the baseline; ascents
                // are at negative y, matching SVG's y-down coordinate system when the caller translates to
                // (x, baselineY).
                composite.AddPath(glyphPath, shaped.Positions[i].X, shaped.Positions[i].Y, SKPathAddMode.Append);
            }
        }

        // ToSvgPathData emits "d" using M/L/C/Q/Z commands — directly usable as an SVG <path d="..."> attribute.
        return composite.ToSvgPathData();
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
