// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.TextMeasurement;

/// <summary>
/// Converts a run of text into an SVG <c>d</c> path-data string composed of glyph outlines.
/// Used by <see cref="Svg.SvgRenderContext"/> to emit text as <c>&lt;path&gt;</c> elements
/// instead of <c>&lt;text&gt;</c>, eliminating dependence on the viewer's installed fonts
/// (the rendered vector shapes exactly match what the layout engine measured, regardless
/// of whether the viewing browser has DejaVu Sans installed).
/// </summary>
/// <remarks>
/// <para>This is the same approach matplotlib's SVG backend uses by default
/// (<c>rcParams['svg.fonttype'] = 'path'</c>). It trades text selectability for
/// deterministic rendering — the right trade-off for publication-quality charts where
/// pixel-perfect layout matters more than copy/paste support.</para>
///
/// <para>Registered via <see cref="ChartServices.GlyphPathProvider"/>. When
/// <c>MatPlotLibNet.Skia</c> is loaded its module initializer installs
/// <c>SkiaGlyphPathProvider</c> which uses <c>SKFont.GetGlyphPath</c> and
/// <c>SKPath.ToSvgPathData</c>. Pure-managed consumers (no Skia) leave the provider
/// null and <see cref="Svg.SvgRenderContext"/> falls back to emitting
/// <c>&lt;text&gt;</c> elements with a <c>font-family</c> stack.</para>
/// </remarks>
public interface IGlyphPathProvider
{
    /// <summary>
    /// Returns SVG path-data <c>d</c> string representing the outline of
    /// <paramref name="text"/> rendered in <paramref name="font"/>. The path is positioned
    /// so that the baseline origin is at <c>(0, 0)</c>; the caller applies a
    /// <c>translate</c>/<c>rotate</c> transform to place it at the target coordinates.
    /// </summary>
    /// <returns>
    /// SVG path data, or <see langword="null"/> when the provider cannot convert the text
    /// (e.g. an unresolvable font). Callers should fall back to <c>&lt;text&gt;</c> emission.
    /// </returns>
    string? GetPathData(string text, Font font);
}
