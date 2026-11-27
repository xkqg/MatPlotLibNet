// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>The rc parameters a theme carries are one list (<see cref="ThemeRcKey.OfATheme"/>), each key saying both what a theme
/// states for it and how its value is laid over a theme: a style sheet made from a theme writes exactly these keys, and a set of
/// parameters lays exactly these over a theme, so the two directions cannot name different keys.</summary>
public class ThemeRcKeyTests
{
    private static readonly string[] Keys = [.. ThemeRcKey.OfATheme.Select(key => key.Key)];

    [Fact]
    public void TheKeysATheme_Carries_AreEachNamedOnce()
    {
        Assert.Equal(10, Keys.Length);
        Assert.Equal(Keys.Length, Keys.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>A sheet made from a theme that names a line width writes every key of the list, in its order; one that names none writes
    /// every key but the width of a line.</summary>
    [Fact]
    public void AStyleSheetFromATheme_WritesTheKeysOfTheList_InTheirOrder()
    {
        Assert.Equal(Keys, StyleSheet.FromTheme(Theme.MatplotlibV2).Parameters.Keys);
        Assert.Equal(Keys.Where(key => key != RcParamKeys.LinesLineWidth), StyleSheet.FromTheme(Theme.Default).Parameters.Keys);
    }

    /// <summary>Every key of the list, set off the library's defaults, is laid over a theme: each changes the theme it is laid over.</summary>
    [Theory]
    [MemberData(nameof(KeyNames))]
    public void EveryKeyOfTheList_SetOffTheDefaults_ChangesTheTheme(string key)
    {
        var parameters = new RcParams();
        parameters.Set(key, OffTheDefault(key));

        var theme = parameters.ThemeOver(Theme.Default);

        Assert.NotSame(Theme.Default, theme);
        Assert.Equal(OffTheDefault(key), ThemeRcKey.OfATheme.Single(k => k.Key == key).ValueOf(theme) is { } value
            ? value is double d ? Math.Round(d, 9) : value
            : null);
    }

    public static TheoryData<string> KeyNames => new(ThemeRcKey.OfATheme.Select(key => key.Key));

    /// <summary>A value of <paramref name="key"/>'s type that no default holds.</summary>
    private static object OffTheDefault(string key) => key switch
    {
        RcParamKeys.FigureFaceColor or RcParamKeys.AxesFaceColor or RcParamKeys.TextColor or RcParamKeys.GridColor => Colors.Red,
        RcParamKeys.FontFamily => "serif",
        RcParamKeys.AxesGrid => true,
        RcParamKeys.GridAlpha => 0.5,
        _ => 3.0,
    };
}
