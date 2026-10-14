// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>Branch coverage for <see cref="FigureSkiaExtensions.BuildKey"/> and
/// <see cref="SkiaFonts.Resolve"/>.</summary>
public class FigureSkiaExtensionsTests
{
    // ── BuildKey switch arms ─────────────────────────────────────────────────

    [Fact]
    public void BuildKey_RegularWeight_ReturnsFamily()
    {
        var style = new SKFontStyle(SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
        var key = SkiaFonts.BuildKey("DejaVu Sans", style);
        Assert.Equal("DejaVu Sans", key);
    }

    [Fact]
    public void BuildKey_BoldWeight_ReturnsFamilyBold()
    {
        var style = new SKFontStyle(SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
        var key = SkiaFonts.BuildKey("Arial", style);
        Assert.Equal("Arial|Bold", key);
    }

    [Fact]
    public void BuildKey_ItalicSlant_ReturnsFamilyItalic()
    {
        var style = new SKFontStyle(SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic);
        var key = SkiaFonts.BuildKey("Arial", style);
        Assert.Equal("Arial|Italic", key);
    }

    [Fact]
    public void BuildKey_BoldItalic_ReturnsFamilyBoldItalic()
    {
        var style = new SKFontStyle(SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic);
        var key = SkiaFonts.BuildKey("Arial", style);
        Assert.Equal("Arial|BoldItalic", key);
    }

    // ── ResolveTypeface ──────────────────────────────────────────────────────

    [Fact]
    public void ResolveTypeface_NullFamily_FallsBackToSystemFont()
    {
        var tf = SkiaFonts.Resolve(null, SKFontStyleWeight.Normal, SKFontStyleSlant.Upright).Typeface;
        Assert.NotNull(tf);
    }

    [Fact]
    public void ResolveTypeface_EmptyFamily_FallsBackToSystemFont()
    {
        var tf = SkiaFonts.Resolve("", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright).Typeface;
        Assert.NotNull(tf);
    }

    [Fact]
    public void ResolveTypeface_BundledDejaVuSans_ReturnsBundledTypeface()
    {
        var tf = SkiaFonts.Resolve("DejaVu Sans", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright).Typeface;
        Assert.NotNull(tf);
        Assert.Contains("DejaVu", tf.FamilyName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveTypeface_CssFontStack_FindsFirstBundledFamily()
    {
        // CSS-style comma-separated stack — first candidate "DejaVu Sans" should match bundled
        var tf = SkiaFonts.Resolve("DejaVu Sans, sans-serif", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright).Typeface;
        Assert.NotNull(tf);
    }

    [Fact]
    public void ResolveTypeface_UnknownFamily_FallsBackToSystemFont()
    {
        var tf = SkiaFonts.Resolve("NoSuchFontXYZ123", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright).Typeface;
        Assert.NotNull(tf);
    }

    [Fact]
    public void ResolveTypeface_BundledDejaVuSansBold_ReturnsBoldTypeface()
    {
        var tf = SkiaFonts.Resolve("DejaVu Sans", SKFontStyleWeight.Bold, SKFontStyleSlant.Upright).Typeface;
        Assert.NotNull(tf);
    }

    [Fact]
    public void ResolveTypeface_EmptyCandidateInStack_SkipsBlankEntry()
    {
        // Leading comma produces an empty candidate — trimmed.Length == 0 → continue arm
        var tf = SkiaFonts.Resolve(", DejaVu Sans", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright).Typeface;
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
        var first = SkiaFonts.Resolve("Arial", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright).Typeface;
        var second = SkiaFonts.Resolve("Arial", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright).Typeface;
        Assert.Same(first, second);
    }

    [Fact]
    public void ResolveTypeface_SameKey_UnresolvableFamily_ReturnsSameInstance()
    {
        // Nonsense family also takes the fallback path (SKTypeface.FromFamilyName returns the
        // default system typeface) — still must be cached and reference-identical across calls.
        var first = SkiaFonts.Resolve("NoSuchFontXYZ123", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright).Typeface;
        var second = SkiaFonts.Resolve("NoSuchFontXYZ123", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright).Typeface;
        Assert.Same(first, second);
    }

    [Fact]
    public void ResolveTypeface_DistinctWeights_AreCachedSeparately()
    {
        // The same correction as its slant twin below, and for the same reason: comparing the two TYPEFACES
        // tests whether this machine owns a bold face, not whether the cache keys on weight. It happens to hold
        // on both platforms today — a bare Linux container does have a bold face where it has no italic — which
        // is exactly why it is worth fixing before it is the one that goes red.
        var normal = SkiaFonts.Resolve("NoSuchFontABC456", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        var bold = SkiaFonts.Resolve("NoSuchFontABC456", SKFontStyleWeight.Bold, SKFontStyleSlant.Upright);
        Assert.NotSame(normal.Shaper, bold.Shaper);

        // Re-resolving the normal weight must still hit the same cache entry, not a fresh one.
        var normalAgain = SkiaFonts.Resolve("NoSuchFontABC456", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        Assert.Same(normal.Shaper, normalAgain.Shaper);
        Assert.Same(normal.Typeface, normalAgain.Typeface);
    }

    [Fact]
    public void ResolveTypeface_DistinctSlants_AreCachedSeparately()
    {
        // WHAT THIS PINS IS THE CACHE, NOT THE MACHINE'S FONTS — corrected after CI went red on ubuntu-latest
        // while Windows stayed green (run 38284548775, and reproduced in mcr.microsoft.com/dotnet/sdk:10.0).
        // The old assertion compared the two TYPEFACES and expected different instances. For an unknown family
        // that comes from SKTypeface.FromFamilyName's fallback, and whether the fallback hands back a separate
        // object per slant is a property of the installed fonts: a Windows box has a real italic face, a bare
        // Linux container has one family with no italic, so it returns the SAME instance for both slants. The
        // test was asserting the environment.
        //
        // The contract is that ResolutionKey carries the slant, so upright and italic are SEPARATE cache
        // entries. Each entry builds its own SKShaper, so the shaper identity shows the entries apart whatever
        // the platform's fallback does with the face itself.
        var upright = SkiaFonts.Resolve("NoSuchFontDEF789", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        var italic = SkiaFonts.Resolve("NoSuchFontDEF789", SKFontStyleWeight.Normal, SKFontStyleSlant.Italic);
        Assert.NotSame(upright.Shaper, italic.Shaper);

        // And each entry is STABLE: a re-resolve hits the same one rather than minting a second.
        var uprightAgain = SkiaFonts.Resolve("NoSuchFontDEF789", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        Assert.Same(upright.Shaper, uprightAgain.Shaper);
        Assert.Same(upright.Typeface, uprightAgain.Typeface);

        var italicAgain = SkiaFonts.Resolve("NoSuchFontDEF789", SKFontStyleWeight.Normal, SKFontStyleSlant.Italic);
        Assert.Same(italic.Shaper, italicAgain.Shaper);
    }

    [Fact]
    public void ResolveTypeface_SameKey_NullFamily_ReturnsSameInstance()
    {
        var first = SkiaFonts.Resolve(null, SKFontStyleWeight.Normal, SKFontStyleSlant.Upright).Typeface;
        var second = SkiaFonts.Resolve(null, SKFontStyleWeight.Normal, SKFontStyleSlant.Upright).Typeface;
        Assert.Same(first, second);
    }
}
