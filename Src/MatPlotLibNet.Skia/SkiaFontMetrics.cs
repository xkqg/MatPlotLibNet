// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia;

/// <summary>
/// Skia-backed <see cref="IFontMetrics"/> implementation. Uses the exact same
/// <c>SKFont.MeasureText</c> call path as <see cref="SkiaRenderContext"/>'s
/// draw operations, so every text width/height returned during layout exactly matches
/// what gets painted in both the SVG and PNG outputs.
/// </summary>
/// <remarks>
/// Registered on <see cref="ChartServices.FontMetrics"/> by
/// <see cref="FigureSkiaExtensions.Initialize"/> at module load. Once active, both
/// <see cref="Rendering.Svg.SvgRenderContext"/> and <see cref="SkiaRenderContext"/> delegate
/// to this class, guaranteeing byte-identical layout between the two render backends.
/// That is the fix for the SVG/PNG tick-label divergence: previously, SvgRenderContext
/// used <see cref="DefaultFontMetrics"/> (per-character width table) while
/// SkiaRenderContext called Skia directly, producing different tick widths → different
/// plot margins → different axis tick positions → clipped edge labels in the PNG.
/// </remarks>
public sealed class SkiaFontMetrics : IFontMetrics
{
    /// <inheritdoc />
    public Size Measure(string text, Font font)
    {
        var typeface = FigureSkiaExtensions.ResolveTypeface(font.Family,
            font.Weight == FontWeight.Bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
            font.Slant == FontSlant.Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
        using var skFont = new SKFont(typeface, (float)font.Size);
        return new Size(skFont.MeasureText(text), font.Size * 1.2);
    }
}
