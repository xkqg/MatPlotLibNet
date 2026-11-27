// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The outline store is keyed on the font as RESOLVED, so a registration, which resolves every family again,
/// makes every outline again: a family that fell back to another face before its file was registered is never drawn
/// in that other face afterwards.</summary>
[Collection("SkiaFontsGlobalState")]
public sealed class GlyphPathCacheResolutionTests
{
    private static readonly Font Font13 = new() { Family = "DejaVu Sans", Size = 13 };

    public GlyphPathCacheResolutionTests() => SkiaFonts.ResetForTests();

    [Fact]
    public void AfterTheFontsAreResolvedAgain_TheOutlineIsMadeAgain()
    {
        var provider = new SkiaGlyphPathProvider();
        string? before = provider.GetPathData("Ta", Font13);

        SkiaFonts.ResetForTests();
        string? after = provider.GetPathData("Ta", Font13);

        Assert.Equal(before, after);
        Assert.NotSame(before, after);
    }
}
