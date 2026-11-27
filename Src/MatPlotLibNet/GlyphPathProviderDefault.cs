// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TextMeasurement;

namespace MatPlotLibNet;

/// <summary>The optional glyph-to-path converter used by <see cref="Rendering.Svg.SvgRenderContext"/> to emit
/// text as <c>&lt;path&gt;</c> elements instead of <c>&lt;text&gt;</c>. One replaceable slot, separated from
/// every other default.</summary>
/// <remarks>When <see langword="null"/>, SVG falls back to <c>&lt;text&gt;</c> with a font-family stack.
/// <c>MatPlotLibNet.Skia</c>'s module initializer installs a Skia-backed implementation so SVG output becomes
/// self-contained (no browser-font dependency) and SVG/PNG produce byte-identical layout.</remarks>
public static class GlyphPathProviderDefault
{
    private static volatile IGlyphPathProvider? _current;

    /// <summary>Gets or sets the default glyph-path provider; <see langword="null"/> falls back to
    /// <c>&lt;text&gt;</c> elements.</summary>
    public static IGlyphPathProvider? Current
    {
        get => _current;
        set => _current = value;
    }
}
