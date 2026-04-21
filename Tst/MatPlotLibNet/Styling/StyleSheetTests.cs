// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="StyleSheet"/> construction and built-in sheets.</summary>
public class StyleSheetTests
{
    [Fact]
    public void Constructor_StoresNameAndParameters()
    {
        var sheet = new StyleSheet("my-style", new Dictionary<string, object>
        {
            [RcParamKeys.FontSize] = 12.0
        });
        Assert.Equal("my-style", sheet.Name);
        Assert.True(sheet.Parameters.ContainsKey(RcParamKeys.FontSize));
    }

    [Fact]
    public void BuiltIn_Default_IsNotNull()
        => Assert.NotNull(StyleSheet.Default);

    [Fact]
    public void BuiltIn_Dark_IsNotNull()
        => Assert.NotNull(StyleSheet.Dark);

    [Fact]
    public void BuiltIn_Seaborn_IsNotNull()
        => Assert.NotNull(StyleSheet.Seaborn);

    [Fact]
    public void BuiltIn_Ggplot_IsNotNull()
        => Assert.NotNull(StyleSheet.Ggplot);

    [Fact]
    public void FromTheme_MapsFontSize()
    {
        var theme = MatPlotLibNet.Styling.Theme.Default;
        var sheet = StyleSheet.FromTheme(theme);
        Assert.NotNull(sheet);
        Assert.True(sheet.Parameters.ContainsKey(RcParamKeys.FontSize));
    }

    /// <summary>Phase X.4 follow-up (v1.7.2, 2026-04-19) — `theme.DefaultFont.Family ?? "sans-serif"`
    /// fallback arm at line 31. Pre-X StyleSheet was 100%L / 50%B because the Default theme
    /// always carries a non-null Family; this constructs a theme with an explicitly null Family
    /// so the ?? short-circuit's right arm runs.</summary>
    [Fact]
    public void FromTheme_NullFontFamily_FallsBackToSansSerif()
    {
        var themeWithNullFamily = Theme.CreateFrom(Theme.Default)
            .WithFont(f => f with { Family = null! })
            .Build();
        var sheet = StyleSheet.FromTheme(themeWithNullFamily);
        Assert.Equal("sans-serif", sheet.Parameters[RcParamKeys.FontFamily]);
    }
}
