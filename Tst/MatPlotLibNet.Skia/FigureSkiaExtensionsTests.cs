// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>Branch coverage for <see cref="SkiaFonts.Resolve"/>.</summary>
public class FigureSkiaExtensionsTests
{
    // ── ResolveTypeface ──────────────────────────────────────────────────────

    /// <summary>A family nobody gave, an empty one, an unknown one, a CSS stack, a bold request and a stack that opens with a blank
    /// candidate (a leading comma, trimmed away) each resolve to a typeface.</summary>
    [Theory]
    [InlineData(null, SKFontStyleWeight.Normal)]
    [InlineData("", SKFontStyleWeight.Normal)]
    [InlineData("NoSuchFontXYZ123", SKFontStyleWeight.Normal)]
    [InlineData("DejaVu Sans, sans-serif", SKFontStyleWeight.Normal)]
    [InlineData("DejaVu Sans", SKFontStyleWeight.Bold)]
    [InlineData(", DejaVu Sans", SKFontStyleWeight.Normal)]
    public void ResolveTypeface_ResolvesATypeface_ForAnyFamilyAndWeight(string? family, SKFontStyleWeight weight)
    {
        Assert.NotNull(SkiaFonts.Resolve(family, weight, SKFontStyleSlant.Upright).Typeface);
    }

    [Fact]
    public void ResolveTypeface_BundledDejaVuSans_ReturnsBundledTypeface()
    {
        var tf = SkiaFonts.Resolve("DejaVu Sans", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright).Typeface;
        Assert.NotNull(tf);
        Assert.Contains("DejaVu", tf.FamilyName, StringComparison.OrdinalIgnoreCase);
    }

    // ── ResolveTypeface cache/ownership (council K5/F8) ─────────────────────
    // ResolveTypeface must ALWAYS return a cached, process-lifetime SKTypeface: callers
    // (SkiaRenderContext, SkiaFontMetrics, SkiaGlyphPathProvider) never dispose the result.
    // Before the fix, non-bundled ("host OS fallback") resolutions returned a FRESH
    // SKTypeface.FromFamilyName instance on every call — a per-call native leak. These
    // pins assert same-key calls are reference-identical (cache hit) and that the cache
    // key is not collapsed across weight/slant variations.

    /// <summary>"Arial" is not a bundled font (only DejaVu Sans ships in this assembly), so it exercises the SKTypeface.FromFamilyName fallback
    /// path, the one that leaked before the fix; a nonsense family also takes the fallback path (it returns the default system typeface). Both
    /// must be cached and reference-identical across calls.</summary>
    [Theory]
    [InlineData("Arial")]
    [InlineData("NoSuchFontXYZ123")]
    public void ResolveTypeface_SameKey_FallbackFamily_ReturnsSameInstance(string family)
    {
        var first = SkiaFonts.Resolve(family, SKFontStyleWeight.Normal, SKFontStyleSlant.Upright).Typeface;
        var second = SkiaFonts.Resolve(family, SKFontStyleWeight.Normal, SKFontStyleSlant.Upright).Typeface;
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
