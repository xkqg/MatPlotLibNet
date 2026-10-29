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

    /// <summary>
    /// Module initializer — runs once on assembly load. Registers `.png` and `.pdf` with the
    /// global `FigureExtensions.TransformRegistry` (over the refusals core seeds there) so that
    /// <c>figure.Save("chart.png")</c> routes through the Skia backend once this assembly is loaded. `.gif` is not
    /// registered: an animation is saved from its builder with <c>SaveGif</c>.
    /// Also loads the bundled DejaVu Sans faces into <see cref="SkiaFonts"/> and installs the Skia-backed
    /// text measurement and glyph outlines on <see cref="MatPlotLibNet.FontMetricsDefault"/> and
    /// <see cref="MatPlotLibNet.GlyphPathProviderDefault"/>.
    /// This is the advertised entry point for the library package — CA2255 (the intended
    /// use of <c>ModuleInitializer</c> is app code only) is suppressed deliberately.
    /// </summary>
#pragma warning disable CA2255
    [ModuleInitializer]
    public static void Initialize()
    {
        global::MatPlotLibNet.FigureExtensions.RegisterTransform(ExportExtension.Png, Png);
        global::MatPlotLibNet.FigureExtensions.RegisterTransform(ExportExtension.Pdf, Pdf);        SkiaFonts.LoadBundled();

        // Install Skia-backed font metrics — the same shaped glyph advances as the PNG pipeline, so layout
        // (margins, tick positions, legend sizing) is identical across SVG and PNG outputs.
        global::MatPlotLibNet.FontMetricsDefault.Current = new SkiaFontMetrics();

        // Install Skia-backed glyph path provider — when the SVG backend writes text, it emits a <path> element
        // with vector glyph outlines generated from the SAME font Skia uses for PNG rendering. The SVG becomes
        // self-contained: it renders identically regardless of whether the viewer has DejaVu Sans installed.
        // Matches matplotlib's default svg.fonttype='path' behaviour.
        global::MatPlotLibNet.GlyphPathProviderDefault.Current = new SkiaGlyphPathProvider();
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
