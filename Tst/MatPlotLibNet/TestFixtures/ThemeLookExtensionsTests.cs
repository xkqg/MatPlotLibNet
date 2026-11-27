// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests;

/// <summary>The three ways a theme travels in the tests: made again from itself, through its data transfer object, and through the JSON
/// of a figure that wears it. Each keeps the looks the theme carries.</summary>
public sealed class ThemeLookExtensionsTests
{
    /// <summary>The looks that tell the four themes apart, as a theme carries them.</summary>
    private static Looked LooksOf(Theme theme) =>
        new(theme.Looks.Range.PatchesLikeAddPatch, theme.Looks.ColorBar.TakesRoom, theme.Looks.Text.TitlePad, theme.Looks.Frame.SpineWidth);

    private readonly record struct Looked(bool PatchRange, bool BarTakesRoom, double? TitlePad, double? SpineWidth);

    [Theory]
    [MemberData(nameof(ThemeCases.DefaultDarkAndMatplotlib), MemberType = typeof(ThemeCases))]
    public void AClonedTheme_KeepsItsLooks_AndIsAnotherTheme(string name)
    {
        var theme = name.AsTheme();

        var clone = theme.Cloned();

        Assert.Equal(LooksOf(theme), LooksOf(clone));
        Assert.NotSame(theme, clone);
    }

    [Theory]
    [MemberData(nameof(ThemeCases.DefaultDarkAndMatplotlib), MemberType = typeof(ThemeCases))]
    public void ATheme_ThroughItsDto_KeepsItsLooks(string name)
    {
        var theme = name.AsTheme();

        Assert.Equal(LooksOf(theme), LooksOf(theme.ThroughDto()));
    }

    [Theory]
    [MemberData(nameof(ThemeCases.DefaultDarkAndMatplotlib), MemberType = typeof(ThemeCases))]
    public void ATheme_ThroughTheJsonOfAFigure_KeepsItsLooks(string name)
    {
        var theme = name.AsTheme();

        Assert.Equal(LooksOf(theme), LooksOf(theme.ThroughJson()));
    }

    [Fact]
    public void TheMatplotlibThemes_AndTheLibraryOnes_DifferInTheLooksThatAreRead()
    {
        Assert.NotEqual(LooksOf(Theme.MatplotlibV2), LooksOf(Theme.Default));
        Assert.True(Theme.MatplotlibClassic.ThroughJson().Looks.Range.PatchesLikeAddPatch);
        Assert.False(Theme.Dark.ThroughJson().Looks.Range.PatchesLikeAddPatch);
    }
}
