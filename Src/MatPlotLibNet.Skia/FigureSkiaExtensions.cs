// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using MatPlotLibNet.Models;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Skia;

/// <summary>Convenience extension methods for PNG and PDF export.</summary>
public static class FigureSkiaExtensions
{
    private static readonly PngTransform Png = new();
    private static readonly PdfTransform Pdf = new();

    /// <summary>The text measurement the package installs: the shaped width of the kept shapings, so the layout of an SVG and
    /// of a PNG is the same.</summary>
    private static readonly SkiaFontMetrics Metrics = new(ShapedTextStore.Shared);

    /// <summary>The glyph outlines the package installs, made from the same kept shapings: the SVG writer draws text as
    /// <c>&lt;path&gt;</c> outlines of the face Skia paints, as matplotlib's default <c>svg.fonttype='path'</c> does, so the
    /// picture does not depend on the fonts the viewer has.</summary>
    private static readonly SkiaGlyphPathProvider Outlines = new(ShapedTextStore.Shared);

    /// <summary>
    /// Module initializer — runs once on assembly load. Registers `.png` and `.pdf` with the
    /// global `FigureExtensions.TransformRegistry` (over the refusals core seeds there) so that
    /// <c>figure.Save("chart.png")</c> routes through the Skia backend once this assembly is loaded. `.gif` is not
    /// registered: an animation is saved from its builder with <c>SaveGif</c>.
    /// Also installs the Skia-backed text measurement and glyph outlines on <see cref="MatPlotLibNet.FontMetricsDefault"/> and
    /// <see cref="MatPlotLibNet.GlyphPathProviderDefault"/>; both read one store of shapings, so a text is shaped once.
    /// Calling it again installs the same exporters, metrics and outlines over the same shapings, and keeps every font
    /// registered with <see cref="SkiaFonts"/>.
    /// This is the advertised entry point for the library package — CA2255 (the intended
    /// use of <c>ModuleInitializer</c> is app code only) is suppressed deliberately.
    /// </summary>
#pragma warning disable CA2255
    [ModuleInitializer]
    public static void Initialize()
    {
        global::MatPlotLibNet.FigureExtensions.RegisterTransform(ExportExtension.Png, Png);
        global::MatPlotLibNet.FigureExtensions.RegisterTransform(ExportExtension.Pdf, Pdf);
        global::MatPlotLibNet.FontMetricsDefault.Current = Metrics;
        global::MatPlotLibNet.GlyphPathProviderDefault.Current = Outlines;
    }
#pragma warning restore CA2255

    extension(Figure figure)
    {
        /// <summary>Exports the figure to a PNG byte array.</summary>
        public byte[] ToPng() => figure.Transform(Png).ToBytes();

        /// <summary>Exports the figure to a PDF byte array.</summary>
        public byte[] ToPdf() => figure.Transform(Pdf).ToBytes();
    }
}
