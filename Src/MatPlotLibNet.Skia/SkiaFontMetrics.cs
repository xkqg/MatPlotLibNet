// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Skia;

/// <summary>
/// Skia-backed <see cref="IFontMetrics"/>: the width of a text is the advance of its shaped glyphs — the same
/// shaping pass <see cref="SkiaRenderContext"/> draws from and <see cref="SkiaGlyphPathProvider"/>
/// outlines from — so every width returned during layout exactly matches what gets painted in both the SVG and
/// PNG outputs, kerning and joined forms included.
/// </summary>
/// <remarks>
/// Registered on <see cref="FontMetricsDefault.Current"/> by <see cref="FigureSkiaExtensions.Initialize"/> at module
/// load. Once active, both <see cref="Rendering.Svg.SvgRenderContext"/> and <see cref="SkiaRenderContext"/> delegate
/// to this class, guaranteeing byte-identical layout between the two render backends. A text is shaped once and its
/// width read again from the shaping kept for it, the one the glyph outlines of the registered
/// <see cref="SkiaGlyphPathProvider"/> are made from.
/// </remarks>
public sealed class SkiaFontMetrics : IFontMetrics, IGlyphInkMetrics
{
    /// <summary>Measures with a store of shapings of its own.</summary>
    public SkiaFontMetrics() : this(new ShapedTextStore()) { }

    /// <summary>Measures with the shapings kept in <paramref name="shaped"/>.</summary>
    internal SkiaFontMetrics(ShapedTextStore shaped) => Shaped = shaped;

    /// <summary>The shapings this measures from.</summary>
    internal ShapedTextStore Shaped { get; }

    /// <inheritdoc />
    /// <remarks>A line of the advance of the text's kept shaping; an empty text has no
    /// width.</remarks>
    public Size Measure(string text, Font font) =>
        TextExtent.OfLine(string.IsNullOrEmpty(text) ? 0 : Shaped.Of(text, font).Width, font).Box;

    /// <summary>How the glyphs of the text's kept shaping stand when matplotlib's Agg backend sets them (<see cref="ShapedRun.Ink"/>).</summary>
    GlyphInk IGlyphInkMetrics.InkOf(string text, Font face, double size) => Shaped.Of(text, SkiaFonts.Resolve(face), (float)size).Ink;

    /// <summary>The typographic band of the face the font resolves to (<see cref="FaceInk.BandAt"/>).</summary>
    /// <remarks>At the size Skia sets the text at, as the band a shaping carries (<see cref="ShapedRun.Ink"/>).</remarks>
    FontBand IGlyphInkMetrics.BandOf(Font font) => SkiaFonts.Resolve(font).Ink.BandAt((float)font.Size);
}
