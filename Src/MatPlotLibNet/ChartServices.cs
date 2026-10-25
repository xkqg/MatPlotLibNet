// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet;

/// <summary>The default <see cref="IChartSerializer"/> for non-DI scenarios (console apps, scripts). One
/// replaceable slot, separated from every other default — the condition a process-global static is held to:
/// it holds the ONE thing it exists to provide, and nothing else is hung off it.</summary>
public static class ChartSerializerDefault
{
    private static volatile IChartSerializer _current = new ChartSerializer();

    /// <summary>Gets or sets the default chart serializer.</summary>
    public static IChartSerializer Current
    {
        get => _current;
        set => _current = value ?? throw new ArgumentNullException(nameof(value));
    }
}

/// <summary>The default <see cref="IChartRenderer"/> for non-DI scenarios (console apps, scripts, and every
/// native UI control -- Avalonia/Maui/Uno/Wpf -- that has no DI container reachable from its own render
/// callback). One replaceable slot, separated from every other default.</summary>
public static class ChartRendererDefault
{
    private static volatile IChartRenderer _current = new ChartRenderer();

    /// <summary>Gets or sets the default chart renderer.</summary>
    public static IChartRenderer Current
    {
        get => _current;
        set => _current = value ?? throw new ArgumentNullException(nameof(value));
    }
}

/// <summary>The default <see cref="ISvgRenderer"/> for non-DI scenarios. One replaceable slot, separated from
/// every other default.</summary>
public static class SvgRendererDefault
{
    private static volatile ISvgRenderer _current = new SvgTransform(new ChartRenderer());

    /// <summary>Gets or sets the default SVG renderer.</summary>
    public static ISvgRenderer Current
    {
        get => _current;
        set => _current = value ?? throw new ArgumentNullException(nameof(value));
    }
}

/// <summary>The text measurement strategy used by every <see cref="IRenderContext"/>. One replaceable slot,
/// separated from every other default.</summary>
/// <remarks>Defaults to <see cref="DefaultFontMetrics"/> (per-character width table, pure
/// managed). <c>MatPlotLibNet.Skia</c>'s module initializer replaces this with a Skia-backed implementation so
/// SVG and PNG output share the same layout metrics.</remarks>
public static class FontMetricsDefault
{
    private static volatile IFontMetrics _current = new DefaultFontMetrics();

    /// <summary>Gets or sets the default text measurement strategy.</summary>
    public static IFontMetrics Current
    {
        get => _current;
        set => _current = value ?? throw new ArgumentNullException(nameof(value));
    }
}

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
