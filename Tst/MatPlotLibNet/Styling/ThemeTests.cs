// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

public class ThemeTests
{
    [Fact]
    public void DefaultTheme_HasWhiteBackground()
    {
        Assert.Equal(Color.White, Theme.Default.Background);
    }

    [Fact]
    public void DefaultTheme_HasColorCycle()
    {
        Assert.NotEmpty(Theme.Default.CycleColors);
        Assert.True(Theme.Default.CycleColors.Length >= 10);
    }

    [Fact]
    public void DefaultTheme_HasBlackForeground()
    {
        Assert.Equal(Color.Black, Theme.Default.ForegroundText);
    }

    [Fact]
    public void DefaultTheme_HasDefaultFont()
    {
        Assert.NotNull(Theme.Default.DefaultFont);
    }

    [Fact]
    public void DarkTheme_HasDarkBackground()
    {
        Assert.NotEqual(Color.White, Theme.Dark.Background);
    }

    [Fact]
    public void DarkTheme_HasLightForeground()
    {
        Assert.NotEqual(Color.Black, Theme.Dark.ForegroundText);
    }

    [Fact]
    public void SeabornTheme_Exists()
    {
        Assert.NotNull(Theme.Seaborn);
    }

    [Fact]
    public void GgplotTheme_Exists()
    {
        Assert.NotNull(Theme.Ggplot);
    }

    [Fact]
    public void ThemeBuilder_OverridesBackground()
    {
        var theme = Theme.CreateFrom(Theme.Default)
            .WithBackground(Color.FromHex("#333333"))
            .Build();

        Assert.Equal(Color.FromHex("#333333"), theme.Background);
    }

    [Fact]
    public void ThemeBuilder_OverridesCycleColors()
    {
        var colors = new[] { Color.Red, Color.Blue, Color.Green };
        var theme = Theme.CreateFrom(Theme.Default)
            .WithCycleColors(colors)
            .Build();

        Assert.Equal(3, theme.CycleColors.Length);
        Assert.Equal(Color.Red, theme.CycleColors[0]);
    }

    [Fact]
    public void ThemeBuilder_OverridesFont()
    {
        var theme = Theme.CreateFrom(Theme.Default)
            .WithFont(f => f with { Size = 18 })
            .Build();

        Assert.Equal(18, theme.DefaultFont.Size);
    }

    [Fact]
    public void ThemeBuilder_PreservesUnchangedProperties()
    {
        var theme = Theme.CreateFrom(Theme.Default)
            .WithBackground(Color.Red)
            .Build();

        Assert.Equal(Theme.Default.CycleColors.Length, theme.CycleColors.Length);
        Assert.Equal(Theme.Default.ForegroundText, theme.ForegroundText);
    }
}
