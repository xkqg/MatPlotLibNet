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
}
