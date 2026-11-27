// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TextMeasurement;

namespace MatPlotLibNet;

/// <summary>The text measurement strategy used by every <see cref="IRenderContext"/>. One replaceable slot,
/// separated from every other default.</summary>
/// <remarks>Defaults to <see cref="DefaultFontMetrics"/> (per-character width table, pure
/// managed). <c>MatPlotLibNet.Skia</c>'s module initializer replaces this with a Skia-backed implementation so
/// SVG and PNG output share the same layout metrics.</remarks>
public static class FontMetricsDefault
{
    private static volatile Installed _installed = new(new DefaultFontMetrics());

    /// <summary>Gets or sets the default text measurement strategy.</summary>
    /// <exception cref="ArgumentNullException">The value set is <see langword="null"/>.</exception>
    public static IFontMetrics Current
    {
        get => _installed.Metrics;
        set => _installed = new Installed(value ?? throw new ArgumentNullException(nameof(value)));
    }

    /// <summary>The installed metrics when they read the glyphs of a font (<see cref="Rendering.TextMeasurement.IGlyphInkMetrics"/>, the
    /// Skia package's), worked out once when they are installed; <see langword="null"/> for metrics without glyphs. The one place the
    /// measuring seam (<see cref="Rendering.TextMeasurement.TextExtentExtensions"/>) asks, so a measure under any look tests no type
    /// of the metrics.</summary>
    internal static Rendering.TextMeasurement.IGlyphInkMetrics? GlyphInk => _installed.Glyphs;

    /// <summary>The metrics installed and what they can read, swapped as one.</summary>
    /// <param name="metrics">The metrics.</param>
    private sealed class Installed(IFontMetrics metrics)
    {
        internal IFontMetrics Metrics { get; } = metrics;

        internal Rendering.TextMeasurement.IGlyphInkMetrics? Glyphs { get; } = metrics as Rendering.TextMeasurement.IGlyphInkMetrics;
    }
}
