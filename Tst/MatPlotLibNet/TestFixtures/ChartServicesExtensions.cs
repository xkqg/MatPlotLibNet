// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests;

/// <summary>A process-wide slot of the library held at a value for a scope: what the slot held is read, the value is written, and what
/// it held is written back when the scope ends (<c>using</c>), however the scope ends. A test class that holds a slot stays in a
/// <c>*GlobalState</c> collection, as every render test reads the slots.</summary>
/// <typeparam name="T">What the slot holds.</typeparam>
internal readonly struct HeldSlot<T> : IDisposable
{
    private readonly Action<T> _write;
    private readonly T _before;

    /// <summary>Holds the slot read by <paramref name="read"/> and written by <paramref name="write"/> at <paramref name="value"/>.</summary>
    internal HeldSlot(Func<T> read, Action<T> write, T value)
    {
        _before = read();
        _write = write;
        write(value);
    }

    /// <summary>Writes back what the slot held.</summary>
    public void Dispose() => _write(_before);
}

/// <summary>The one way a test sets a process-wide slot of the library: <c>using var held = FontMetricsDefault.Holding(metrics);</c>
/// holds it for the scope (<see cref="HeldSlot{T}"/>), the same for the serializer, the chart renderer, the SVG renderer and the glyph
/// outlines.</summary>
internal static class ChartServicesExtensions
{
    extension(ChartSerializerDefault)
    {
        /// <summary>Holds the serializer at <paramref name="serializer"/> for a scope.</summary>
        internal static HeldSlot<IChartSerializer> Holding(IChartSerializer serializer) => new(static () => ChartSerializerDefault.Current, static value => ChartSerializerDefault.Current = value, serializer);
    }

    extension(ChartRendererDefault)
    {
        /// <summary>Holds the chart renderer at <paramref name="renderer"/> for a scope.</summary>
        internal static HeldSlot<IChartRenderer> Holding(IChartRenderer renderer) => new(static () => ChartRendererDefault.Current, static value => ChartRendererDefault.Current = value, renderer);
    }

    extension(SvgRendererDefault)
    {
        /// <summary>Holds the SVG renderer at <paramref name="renderer"/> for a scope.</summary>
        internal static HeldSlot<ISvgRenderer> Holding(ISvgRenderer renderer) => new(static () => SvgRendererDefault.Current, static value => SvgRendererDefault.Current = value, renderer);
    }

    extension(FontMetricsDefault)
    {
        /// <summary>Holds the text measure at <paramref name="metrics"/> for a scope.</summary>
        internal static HeldSlot<IFontMetrics> Holding(IFontMetrics metrics) => new(static () => FontMetricsDefault.Current, static value => FontMetricsDefault.Current = value, metrics);
    }

    extension(GlyphPathProviderDefault)
    {
        /// <summary>Holds the glyph outlines at <paramref name="outlines"/> for a scope; <see langword="null"/> writes text elements.</summary>
        internal static HeldSlot<IGlyphPathProvider?> Holding(IGlyphPathProvider? outlines) => new(static () => GlyphPathProviderDefault.Current, static value => GlyphPathProviderDefault.Current = value, outlines);
    }
}
