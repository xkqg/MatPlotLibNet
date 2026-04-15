// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet;

/// <summary>Provides default service instances for non-DI scenarios (console apps, scripts).</summary>
public static class ChartServices
{
    private static volatile IChartSerializer _serializer = new ChartSerializer();
    private static volatile IChartRenderer _renderer = new ChartRenderer();
    private static volatile ISvgRenderer _svgRenderer = new SvgTransform(new ChartRenderer());
    private static volatile IFontMetrics _fontMetrics = new DefaultFontMetrics();
    private static volatile IGlyphPathProvider? _glyphPathProvider;

    /// <summary>Gets or sets the default chart serializer.</summary>
    public static IChartSerializer Serializer
    {
        get => _serializer;
        set => _serializer = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets or sets the default chart renderer.</summary>
    public static IChartRenderer Renderer
    {
        get => _renderer;
        set => _renderer = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets or sets the default SVG renderer.</summary>
    public static ISvgRenderer SvgRenderer
    {
        get => _svgRenderer;
        set => _svgRenderer = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the text measurement strategy used by every <see cref="IRenderContext"/>.
    /// Defaults to <see cref="DefaultFontMetrics"/> (per-character width table, pure managed).
    /// <c>MatPlotLibNet.Skia</c>'s module initializer replaces this with a Skia-backed
    /// implementation so SVG and PNG output share the same layout metrics.
    /// </summary>
    public static IFontMetrics FontMetrics
    {
        get => _fontMetrics;
        set => _fontMetrics = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Optional glyph-to-path converter used by <see cref="Rendering.Svg.SvgRenderContext"/>
    /// to emit text as <c>&lt;path&gt;</c> elements instead of <c>&lt;text&gt;</c>. When
    /// <see langword="null"/>, SVG falls back to <c>&lt;text&gt;</c> with a font-family stack.
    /// <c>MatPlotLibNet.Skia</c>'s module initializer installs a Skia-backed implementation
    /// so SVG output becomes self-contained (no browser-font dependency) and SVG/PNG produce
    /// byte-identical layout.
    /// </summary>
    public static IGlyphPathProvider? GlyphPathProvider
    {
        get => _glyphPathProvider;
        set => _glyphPathProvider = value;
    }
}
