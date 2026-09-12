// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia;

/// <summary>
/// Skia-backed <see cref="IFontMetrics"/>: the width of a text is the advance of its shaped glyphs — the same
/// <see cref="TextShaper"/> pass <see cref="SkiaRenderContext"/> draws from and <see cref="SkiaGlyphPathProvider"/>
/// outlines from — so every width returned during layout exactly matches what gets painted in both the SVG and
/// PNG outputs, kerning and joined forms included.
/// </summary>
/// <remarks>
/// Registered on <see cref="ChartServices.FontMetrics"/> by <see cref="FigureSkiaExtensions.Initialize"/> at module
/// load. Once active, both <see cref="Rendering.Svg.SvgRenderContext"/> and <see cref="SkiaRenderContext"/> delegate
/// to this class, guaranteeing byte-identical layout between the two render backends.
/// </remarks>
public sealed class SkiaFontMetrics : IFontMetrics
{
    /// <inheritdoc />
    public Size Measure(string text, Font font)
    {
        var resolved = SkiaFonts.Resolve(font);
        using var skFont = new SKFont(resolved.Typeface, (float)font.Size);
        return new Size(TextShaper.Shape(text, skFont, resolved.Shaper).Width, font.Size * 1.2);
    }
}
