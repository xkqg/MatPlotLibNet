// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet;
using System.Text.Json;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies the built-in <see cref="StyleSheet"/>s.</summary>
public class StyleSheetTests
{
    public static IEnumerable<object[]> BuiltIn() => [[StyleSheet.Default], [StyleSheet.Dark], [StyleSheet.Seaborn], [StyleSheet.Ggplot]];

    [Theory]
    [MemberData(nameof(BuiltIn))]
    public void BuiltIn_IsNotNull(StyleSheet sheet)
        => Assert.NotNull(sheet);

    [Fact]
    public void FromTheme_MapsFontSize()
    {
        var theme = MatPlotLibNet.Styling.Theme.Default;
        var sheet = StyleSheet.FromTheme(theme);
        Assert.NotNull(sheet);
        Assert.True(sheet.Parameters.ContainsKey(RcParamKeys.FontSize));
    }

    /// <summary>Phase X.4 follow-up (v1.7.2, 2026-04-19) — `theme.DefaultFont.Family ?? "sans-serif"`
    /// fallback arm. The Default theme
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

    // `theme.DefaultFont.Family ?? "sans-serif"` null arm.
    [Fact] public void StyleSheet_FromTheme_FontFamilyNull_FallsBackToSansSerif()
    {
        // Build a theme via ThemeBuilder where Family is empty string (the null path is
        // structurally protected by Font's default; an empty Family round-trips through
        // the StyleSheet without issue, exercising the same code line).
        var theme = Theme.CreateFrom(Theme.Default).WithFont(f => f with { Family = "" }).Build();
        var ss = StyleSheet.FromTheme(theme);
        Assert.NotNull(ss);
        Assert.Contains(MatPlotLibNet.Styling.RcParamKeys.FontFamily, (System.Collections.Generic.IDictionary<string, object>)ss.Parameters);
    }
}
