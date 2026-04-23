// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia;

/// <summary>
/// Skia-backed <see cref="IGlyphPathProvider"/>: walks the supplied text one glyph
/// at a time using <c>SKFont.GetGlyphs</c>, composes the per-glyph outlines
/// from <c>SKFont.GetGlyphPath</c> into a single <see cref="SKPath"/>, and
/// returns the result via <see cref="SKPath.ToSvgPathData"/>. This gives the SVG backend
/// vector shapes that match exactly what Skia draws into the PNG/PDF outputs — no
/// browser-font dependency, no metric divergence.
/// </summary>
/// <remarks>
/// Registered on <see cref="global::MatPlotLibNet.ChartServices.GlyphPathProvider"/> by
/// <see cref="FigureSkiaExtensions.Initialize"/> at module load.
/// Uses the same typeface resolution path (<see cref="FigureSkiaExtensions.ResolveTypeface"/>)
/// as <see cref="SkiaRenderContext"/> so glyph measurements and glyph paths come from the
/// identical font — guaranteeing pixel-for-pixel agreement with the PNG output.
/// Excluded from coverage: exercised only by the SkiaSharp text-rendering pipeline at
/// runtime; unit tests cannot enter this path without a full end-to-end Skia fixture.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class SkiaGlyphPathProvider : IGlyphPathProvider
{
    /// <inheritdoc />
    public string? GetPathData(string text, Font font)
    {
        if (string.IsNullOrEmpty(text)) return null;

        var typeface = FigureSkiaExtensions.ResolveTypeface(font.Family,
            font.Weight == FontWeight.Bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
            font.Slant == FontSlant.Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
        using var skFont = new SKFont(typeface, (float)font.Size);

        // Glyph IDs for the entire string (handles Unicode via Skia's encoding).
        ushort[] glyphs = skFont.GetGlyphs(text);
        if (glyphs.Length == 0) return string.Empty;

        // Glyph widths in the current font size — advances along the baseline between
        // glyphs. Using the ReadOnlySpan<ushort> overload keeps everything in glyph-id
        // space without a second text→glyph pass.
        float[] widths = skFont.GetGlyphWidths(glyphs, paint: null);

        using var composite = new SKPath();
        float cursor = 0f;
        for (int i = 0; i < glyphs.Length; i++)
        {
            using var glyphPath = skFont.GetGlyphPath(glyphs[i]);
            if (glyphPath is { IsEmpty: false })
            {
                // Glyph outlines from Skia are positioned with their origin at (0, 0)
                // on the baseline; ascents are at negative y, matching SVG's y-down
                // coordinate system when the caller translates to (x, baselineY).
                composite.AddPath(glyphPath, cursor, 0f, SKPathAddMode.Append);
            }
            cursor += widths[i];
        }

        // ToSvgPathData emits "d" using M/L/C/Q/Z commands — directly usable as an SVG
        // <path d="..."> attribute.
        return composite.ToSvgPathData();
    }

    /// <summary>Returns the total advance width of <paramref name="text"/> in
    /// <paramref name="font"/>. Convenience for layout/alignment code that wants the
    /// same width Skia used to place the glyphs.</summary>
    public static double MeasureAdvance(string text, Font font)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var typeface = FigureSkiaExtensions.ResolveTypeface(font.Family,
            font.Weight == FontWeight.Bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
            font.Slant == FontSlant.Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
        using var skFont = new SKFont(typeface, (float)font.Size);
        return skFont.MeasureText(text);
    }
}
