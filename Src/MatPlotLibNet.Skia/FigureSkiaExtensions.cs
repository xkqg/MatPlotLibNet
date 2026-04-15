// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using MatPlotLibNet.Models;
using MatPlotLibNet.Transforms;
using SkiaSharp;

namespace MatPlotLibNet.Skia;

/// <summary>Convenience extension methods for PNG and PDF export.</summary>
public static class FigureSkiaExtensions
{
    private static readonly PngTransform Png = new();
    private static readonly PdfTransform Pdf = new();

    /// <summary>
    /// Bundled-typeface cache keyed by family name (case-insensitive). Loaded once on assembly
    /// load from the embedded DejaVu Sans TTFs. Consumed by <see cref="SkiaRenderContext"/> so
    /// text rendering uses the IDENTICAL font matplotlib uses, regardless of host-OS fonts.
    /// </summary>
    internal static readonly ConcurrentDictionary<string, SKTypeface> BundledTypefaces =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Module initializer — runs once on assembly load. Registers `.png` and `.pdf` with the
    /// global `FigureExtensions.TransformRegistry` so that <c>figure.Save("chart.png")</c>
    /// routes through the Skia backend automatically when this assembly is referenced.
    /// Also loads the bundled DejaVu Sans TTFs into <see cref="BundledTypefaces"/>.
    /// This is the advertised entry point for the library package — CA2255 (the intended
    /// use of <c>ModuleInitializer</c> is app code only) is suppressed deliberately.
    /// </summary>
#pragma warning disable CA2255
    [ModuleInitializer]
    public static void Initialize()
    {
        global::MatPlotLibNet.FigureExtensions.RegisterTransform(".png", Png);
        global::MatPlotLibNet.FigureExtensions.RegisterTransform(".pdf", Pdf);
        LoadBundledFonts();

        // Install Skia-backed font metrics — same DejaVu Sans glyph widths as the PNG
        // pipeline, so layout (margins, tick positions, legend sizing) is identical
        // across SVG and PNG outputs.
        global::MatPlotLibNet.ChartServices.FontMetrics = new SkiaFontMetrics();

        // Install Skia-backed glyph path provider — when the SVG backend writes text,
        // it emits a <path> element with vector glyph outlines generated from the
        // SAME font Skia uses for PNG rendering. The SVG becomes self-contained: it
        // renders identically regardless of whether the viewer has DejaVu Sans
        // installed. Matches matplotlib's default svg.fonttype='path' behaviour.
        global::MatPlotLibNet.ChartServices.GlyphPathProvider = new SkiaGlyphPathProvider();
    }
#pragma warning restore CA2255

    private static void LoadBundledFonts()
    {
        var asm = typeof(FigureSkiaExtensions).Assembly;
        const string prefix = "MatPlotLibNet.Skia.Fonts.";
        foreach (var name in asm.GetManifestResourceNames())
        {
            if (!name.StartsWith(prefix, StringComparison.Ordinal) ||
                !name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
                continue;
            using var stream = asm.GetManifestResourceStream(name);
            if (stream is null) continue;
            // SKTypeface.FromStream reads the stream eagerly so we can dispose it.
            var typeface = SKTypeface.FromStream(stream);
            if (typeface is null) continue;
            // Key by FamilyName + style so "DejaVu Sans" / "DejaVu Sans|Bold" / etc
            // round-trip. The Resolve helper in SkiaRenderContext computes the same key.
            string key = BuildKey(typeface.FamilyName, typeface.FontStyle);
            BundledTypefaces[key] = typeface;
            // Also register the bare family name for the regular weight so callers
            // who pass just "DejaVu Sans" hit it.
            if (typeface.FontStyle.Weight == (int)SKFontStyleWeight.Normal &&
                typeface.FontStyle.Slant == SKFontStyleSlant.Upright)
            {
                BundledTypefaces.TryAdd(typeface.FamilyName, typeface);
            }
        }
    }

    internal static string BuildKey(string family, SKFontStyle style)
    {
        bool bold = style.Weight >= (int)SKFontStyleWeight.SemiBold;
        bool italic = style.Slant != SKFontStyleSlant.Upright;
        return (bold, italic) switch
        {
            (true,  true)  => $"{family}|BoldItalic",
            (true,  false) => $"{family}|Bold",
            (false, true)  => $"{family}|Italic",
            _              => family,
        };
    }

    /// <summary>Resolves a typeface by font-family name. Checks the bundled cache (DejaVu Sans
    /// shipped inside this assembly) before falling back to the host OS via
    /// <see cref="SKTypeface.FromFamilyName(string?, SKFontStyleWeight, SKFontStyleWidth, SKFontStyleSlant)"/>.</summary>
    internal static SKTypeface ResolveTypeface(string? family, SKFontStyleWeight weight, SKFontStyleSlant slant)
    {
        // Theme fonts often pass a CSS-style stack like "DejaVu Sans, sans-serif" — try each
        // family in order and return the first bundled match.
        if (!string.IsNullOrEmpty(family))
        {
            var fontStyle = new SKFontStyle(weight, SKFontStyleWidth.Normal, slant);
            foreach (var candidate in family.Split(','))
            {
                var trimmed = candidate.Trim().Trim('"', '\'');
                if (trimmed.Length == 0) continue;
                var key = BuildKey(trimmed, fontStyle);
                if (BundledTypefaces.TryGetValue(key, out var bundled)) return bundled;
                // For the bundled cache, also accept the bare family name as a "regular" match
                if (BundledTypefaces.TryGetValue(trimmed, out var bare) &&
                    weight == SKFontStyleWeight.Normal && slant == SKFontStyleSlant.Upright)
                    return bare;
            }
        }
        return SKTypeface.FromFamilyName(family, weight, SKFontStyleWidth.Normal, slant);
    }

    /// <summary>Exports the figure to a PNG byte array.</summary>
    public static byte[] ToPng(this Figure figure) => figure.Transform(Png).ToBytes();

    /// <summary>Exports the figure to a PDF byte array.</summary>
    public static byte[] ToPdf(this Figure figure) => figure.Transform(Pdf).ToBytes();
}
