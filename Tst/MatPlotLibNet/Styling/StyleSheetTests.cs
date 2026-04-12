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
}
