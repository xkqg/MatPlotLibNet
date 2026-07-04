// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>Branch coverage for <see cref="FigureSkiaExtensions.BuildKey"/> and
/// <see cref="FigureSkiaExtensions.ResolveTypeface"/>.</summary>
public class FigureSkiaExtensionsTests
{
    // ── BuildKey switch arms ─────────────────────────────────────────────────

    [Fact]
    public void BuildKey_RegularWeight_ReturnsFamily()
    {
        var style = new SKFontStyle(SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
        var key = FigureSkiaExtensions.BuildKey("DejaVu Sans", style);
        Assert.Equal("DejaVu Sans", key);
    }

    [Fact]
    public void BuildKey_BoldWeight_ReturnsFamilyBold()
    {
        var style = new SKFontStyle(SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
        var key = FigureSkiaExtensions.BuildKey("Arial", style);
        Assert.Equal("Arial|Bold", key);
    }

    [Fact]
    public void BuildKey_ItalicSlant_ReturnsFamilyItalic()
    {
        var style = new SKFontStyle(SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic);
        var key = FigureSkiaExtensions.BuildKey("Arial", style);
        Assert.Equal("Arial|Italic", key);
    }

    [Fact]
    public void BuildKey_BoldItalic_ReturnsFamilyBoldItalic()
    {
        var style = new SKFontStyle(SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic);
        var key = FigureSkiaExtensions.BuildKey("Arial", style);
        Assert.Equal("Arial|BoldItalic", key);
    }

    // ── ResolveTypeface ──────────────────────────────────────────────────────

    [Fact]
    public void ResolveTypeface_NullFamily_FallsBackToSystemFont()
    {
        var tf = FigureSkiaExtensions.ResolveTypeface(null, SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        Assert.NotNull(tf);
    }

    [Fact]
    public void ResolveTypeface_EmptyFamily_FallsBackToSystemFont()
    {
        var tf = FigureSkiaExtensions.ResolveTypeface("", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        Assert.NotNull(tf);
    }

    [Fact]
    public void ResolveTypeface_BundledDejaVuSans_ReturnsBundledTypeface()
    {
        var tf = FigureSkiaExtensions.ResolveTypeface("DejaVu Sans", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        Assert.NotNull(tf);
        Assert.Contains("DejaVu", tf.FamilyName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveTypeface_CssFontStack_FindsFirstBundledFamily()
    {
        // CSS-style comma-separated stack — first candidate "DejaVu Sans" should match bundled
        var tf = FigureSkiaExtensions.ResolveTypeface("DejaVu Sans, sans-serif", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        Assert.NotNull(tf);
    }

    [Fact]
    public void ResolveTypeface_UnknownFamily_FallsBackToSystemFont()
    {
        var tf = FigureSkiaExtensions.ResolveTypeface("NoSuchFontXYZ123", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        Assert.NotNull(tf);
    }

    [Fact]
    public void ResolveTypeface_BundledDejaVuSansBold_ReturnsBoldTypeface()
    {
        var tf = FigureSkiaExtensions.ResolveTypeface("DejaVu Sans", SKFontStyleWeight.Bold, SKFontStyleSlant.Upright);
        Assert.NotNull(tf);
    }

    [Fact]
    public void ResolveTypeface_EmptyCandidateInStack_SkipsBlankEntry()
    {
        // Leading comma produces an empty candidate — trimmed.Length == 0 → continue arm
        var tf = FigureSkiaExtensions.ResolveTypeface(", DejaVu Sans", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        Assert.NotNull(tf);
    }

    // ── ResolveTypeface cache/ownership (council K5/F8) ─────────────────────
    // ResolveTypeface must ALWAYS return a cached, process-lifetime SKTypeface: callers
    // (SkiaRenderContext, SkiaFontMetrics, SkiaGlyphPathProvider) never dispose the result.
    // Before the fix, non-bundled ("host OS fallback") resolutions returned a FRESH
    // SKTypeface.FromFamilyName instance on every call — a per-call native leak. These
    // pins assert same-key calls are reference-identical (cache hit) and that the cache
    // key is not collapsed across weight/slant variations.

    [Fact]
    public void ResolveTypeface_SameKey_HostFallbackFamily_ReturnsSameInstance()
    {
        // "Arial" is not a bundled font (only DejaVu Sans ships in this assembly), so this
        // exercises the SKTypeface.FromFamilyName fallback path — the one that leaked pre-fix.
        var first = FigureSkiaExtensions.ResolveTypeface("Arial", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        var second = FigureSkiaExtensions.ResolveTypeface("Arial", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        Assert.Same(first, second);
    }

    [Fact]
    public void ResolveTypeface_SameKey_UnresolvableFamily_ReturnsSameInstance()
    {
        // Nonsense family also takes the fallback path (SKTypeface.FromFamilyName returns the
        // default system typeface) — still must be cached and reference-identical across calls.
        var first = FigureSkiaExtensions.ResolveTypeface("NoSuchFontXYZ123", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        var second = FigureSkiaExtensions.ResolveTypeface("NoSuchFontXYZ123", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        Assert.Same(first, second);
    }

    [Fact]
    public void ResolveTypeface_DistinctWeights_AreCachedSeparately()
    {
        var normal = FigureSkiaExtensions.ResolveTypeface("NoSuchFontABC456", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        var bold = FigureSkiaExtensions.ResolveTypeface("NoSuchFontABC456", SKFontStyleWeight.Bold, SKFontStyleSlant.Upright);
        Assert.NotSame(normal, bold);
        // Re-resolving the normal weight must still hit the same cache entry, not a fresh one.
        var normalAgain = FigureSkiaExtensions.ResolveTypeface("NoSuchFontABC456", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        Assert.Same(normal, normalAgain);
    }

    [Fact]
    public void ResolveTypeface_DistinctSlants_AreCachedSeparately()
    {
        var upright = FigureSkiaExtensions.ResolveTypeface("NoSuchFontDEF789", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        var italic = FigureSkiaExtensions.ResolveTypeface("NoSuchFontDEF789", SKFontStyleWeight.Normal, SKFontStyleSlant.Italic);
        Assert.NotSame(upright, italic);
        var uprightAgain = FigureSkiaExtensions.ResolveTypeface("NoSuchFontDEF789", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        Assert.Same(upright, uprightAgain);
    }

    [Fact]
    public void ResolveTypeface_SameKey_NullFamily_ReturnsSameInstance()
    {
        var first = FigureSkiaExtensions.ResolveTypeface(null, SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        var second = FigureSkiaExtensions.ResolveTypeface(null, SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        Assert.Same(first, second);
    }
}
