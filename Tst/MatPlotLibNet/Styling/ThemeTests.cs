// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="Theme"/> behavior.</summary>
public class ThemeTests
{
    /// <summary>Verifies that the default theme has a white background.</summary>
    [Fact]
    public void DefaultTheme_HasWhiteBackground()
    {
        Assert.Equal(Colors.White, Theme.Default.Background);
    }

    /// <summary>Verifies that the default theme has at least 10 cycle colors.</summary>
    [Fact]
    public void DefaultTheme_HasColorCycle()
    {
        Assert.NotEmpty(Theme.Default.CycleColors);
        Assert.True(Theme.Default.CycleColors.Length >= 10);
    }

    /// <summary>Verifies that the default theme has black foreground text.</summary>
    [Fact]
    public void DefaultTheme_HasBlackForeground()
    {
        Assert.Equal(Colors.Black, Theme.Default.ForegroundText);
    }

    /// <summary>Verifies that the default theme provides a non-null default font.</summary>
    [Fact]
    public void DefaultTheme_HasDefaultFont()
    {
        Assert.NotNull(Theme.Default.DefaultFont);
    }

    /// <summary>Verifies that the dark theme has a non-white background.</summary>
    [Fact]
    public void DarkTheme_HasDarkBackground()
    {
        Assert.NotEqual(Colors.White, Theme.Dark.Background);
    }

    /// <summary>Verifies that the dark theme has a non-black foreground text.</summary>
    [Fact]
    public void DarkTheme_HasLightForeground()
    {
        Assert.NotEqual(Colors.Black, Theme.Dark.ForegroundText);
    }

    /// <summary>Verifies that the Seaborn theme is available.</summary>
    [Fact]
    public void SeabornTheme_Exists()
    {
        Assert.NotNull(Theme.Seaborn);
    }

    /// <summary>Verifies that the Ggplot theme is available.</summary>
    [Fact]
    public void GgplotTheme_Exists()
    {
        Assert.NotNull(Theme.Ggplot);
    }

    /// <summary>Verifies that the theme builder can override the background color.</summary>
    [Fact]
    public void ThemeBuilder_OverridesBackground()
    {
        var theme = Theme.CreateFrom(Theme.Default)
            .WithBackground(Color.FromHex("#333333"))
            .Build();

        Assert.Equal(Color.FromHex("#333333"), theme.Background);
    }

    /// <summary>Verifies that the theme builder can override the cycle colors array.</summary>
    [Fact]
    public void ThemeBuilder_OverridesCycleColors()
    {
        var colors = new[] { Colors.Red, Colors.Blue, Colors.Green };
        var theme = Theme.CreateFrom(Theme.Default)
            .WithCycleColors(colors)
            .Build();

        Assert.Equal(3, theme.CycleColors.Length);
        Assert.Equal(Colors.Red, theme.CycleColors[0]);
    }

    /// <summary>Verifies that the theme builder can override the default font.</summary>
    [Fact]
    public void ThemeBuilder_OverridesFont()
    {
        var theme = Theme.CreateFrom(Theme.Default)
            .WithFont(f => f with { Size = 18 })
            .Build();

        Assert.Equal(18, theme.DefaultFont.Size);
    }

    /// <summary>Verifies that the theme builder preserves properties that were not overridden.</summary>
    [Fact]
    public void ThemeBuilder_PreservesUnchangedProperties()
    {
        var theme = Theme.CreateFrom(Theme.Default)
            .WithBackground(Colors.Red)
            .Build();

        Assert.Equal(Theme.Default.CycleColors.Length, theme.CycleColors.Length);
        Assert.Equal(Theme.Default.ForegroundText, theme.ForegroundText);
    }
}
