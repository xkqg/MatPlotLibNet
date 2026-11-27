// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TextMeasurement;

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>How a run of math is measured on a surface: by the ink of its glyphs where the surface measures like Agg (the matplotlib
/// looks with the Skia package's metrics), else by the box the surface measures (its letters rising the font size above the baseline).
/// The one place the layout reads a size of a glyph; made by <see cref="TextExtentExtensions"/>, the seam that decides how a surface
/// measures. Two measures are equal when they measure alike, so a layout made on one surface is the one another reads
/// (<see cref="LineBox"/>).</summary>
internal readonly record struct MathMeasure
{
    private readonly IGlyphInkMetrics? _ink;
    private readonly IFontMetrics? _metrics;
    private readonly IRenderContext? _surface;

    private MathMeasure(IGlyphInkMetrics? ink, IFontMetrics? metrics, IRenderContext? surface)
    {
        _ink = ink;
        _metrics = metrics;
        _surface = surface;
    }

    /// <summary>Measures by the ink of the glyphs, as matplotlib's mathtext reads a glyph's metrics.</summary>
    /// <param name="ink">The metrics that read the glyphs.</param>
    internal static MathMeasure ByInk(IGlyphInkMetrics ink) => new(ink, null, null);

    /// <summary>Measures by the box <paramref name="surface"/> measures a text as (<see cref="IRenderContext.MeasureText"/>): by the
    /// metrics it measures by when it names them (<see cref="IThemedRenderContext.BoxMetrics"/>, the installed metrics for every surface of
    /// the library), so the measure is one whichever of them asks and a layout outlives the surface it was made on; by the surface itself
    /// where it names none (a surface of your own).</summary>
    /// <param name="surface">The surface.</param>
    internal static MathMeasure ByBox(IRenderContext surface) =>
        surface.BoxMetricsTold is { } metrics ? new(null, metrics, null) : new(null, null, surface);

    /// <summary>The advance, height and depth of <paramref name="text"/> set in <paramref name="font"/>.</summary>
    /// <param name="text">The characters, as the run's face codes them.</param>
    /// <param name="font">The face and the size.</param>
    internal MathExtent Of(string text, SpanFont font)
    {
        if (_ink is { } ink)
        {
            var glyphs = ink.InkOf(text, font.Face, font.Size);
            return new MathExtent(glyphs.Advance, glyphs.Rise, glyphs.Drop);
        }

        var box = TextExtent.Of(_metrics is { } metrics ? metrics.Measure(text, font.Font) : _surface!.MeasureText(text, font.Font), font.Size);
        return new MathExtent(box.Width, box.Ascent, box.Descent);
    }
}
