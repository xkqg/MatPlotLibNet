// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary><see cref="FigureSkiaExtensions.Initialize"/> runs when the assembly loads, and it is public, so an application may
/// call it again. A second call changes nothing: the same metrics and glyph outlines stay installed over the same kept shapings,
/// every family resolves to the face and shaper it resolved to before, and a font the application registered stays registered.
/// It writes the font and text slots, so it runs alone.</summary>
[Collection("SkiaFontsGlobalState")]
public sealed class SkiaPackageInitializeTests : IDisposable
{
    private static readonly Font Font13 = new() { Family = "DejaVu Sans", Size = 13 };

    private static readonly string NotoSansHebrewPath =
        Path.Combine(AppContext.BaseDirectory, "Fonts", "NotoSansHebrew-Regular.ttf");

    public SkiaPackageInitializeTests() => SkiaFonts.ResetForTests();

    public void Dispose() => SkiaFonts.ResetForTests();

    [Fact]
    public void CalledAgain_TheSameTextAndFont_GiveTheSameOutline()
    {
        var outlines = GlyphPathProviderDefault.Current!;
        string? before = outlines.GetPathData("Initialize again", Font13);

        FigureSkiaExtensions.Initialize();
        string? after = GlyphPathProviderDefault.Current!.GetPathData("Initialize again", Font13);

        Assert.NotNull(before);
        Assert.Same(before, after);
    }

    [Fact]
    public void CalledAgain_TheInstalledMetricsAndOutlinesStay()
    {
        var metrics = FontMetricsDefault.Current;
        var outlines = GlyphPathProviderDefault.Current;

        FigureSkiaExtensions.Initialize();

        Assert.Same(metrics, FontMetricsDefault.Current);
        Assert.Same(outlines, GlyphPathProviderDefault.Current);
    }

    [Fact]
    public void CalledAgain_AFamilyResolvesToTheSameFaceAndShaper()
    {
        var before = SkiaFonts.Resolve(Font13);

        FigureSkiaExtensions.Initialize();
        var after = SkiaFonts.Resolve(Font13);

        Assert.Same(before.Typeface, after.Typeface);
        Assert.Same(before.Shaper, after.Shaper);
    }

    [Fact]
    public void CalledAgain_ARegisteredFontStaysRegistered()
    {
        SkiaFonts.Register(NotoSansHebrewPath);

        FigureSkiaExtensions.Initialize();
        var resolved = SkiaFonts.Resolve("Noto Sans Hebrew", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);

        Assert.Equal("Noto Sans Hebrew", resolved.Typeface.FamilyName);
    }
}
