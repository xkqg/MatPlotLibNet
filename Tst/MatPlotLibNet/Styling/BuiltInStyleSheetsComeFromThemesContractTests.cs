// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>A look the library ships is written once, as a <see cref="Theme"/>; the style sheet of the same name is that theme made
/// into rc parameters (<see cref="StyleSheet.FromTheme"/>), so <c>Plt.Style.Use(StyleSheet.Ggplot)</c> and
/// <c>WithTheme(Theme.Ggplot)</c> describe one look and cannot drift apart.</summary>
public class BuiltInStyleSheetsComeFromThemesContractTests
{
    /// <summary>Every built-in sheet, by the name of its property.</summary>
    public static TheoryData<string> BuiltInSheets() =>
    [
        .. typeof(StyleSheet).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(StyleSheet))
            .Select(property => property.Name),
    ];

    /// <summary>The sheets the library ships are the four it registers by name.</summary>
    [Fact]
    public void TheBuiltInSheets_AreTheFourTheLibraryShips() =>
        Assert.Equal(["Dark", "Default", "Ggplot", "Seaborn"], BuiltInSheets().Select(row => (string)row.Data).Order());

    /// <summary>A built-in sheet is its theme's: the same name and the same parameters, value for value.</summary>
    [Theory]
    [MemberData(nameof(BuiltInSheets))]
    public void ABuiltInSheet_IsTheSheetOfTheThemeOfItsName(string name)
    {
        var sheet = (StyleSheet)typeof(StyleSheet).GetProperty(name, BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        var theme = name.AsTheme();
        var fromTheme = StyleSheet.FromTheme(theme);

        Assert.Equal(fromTheme.Name, sheet.Name);
        Assert.Equal(fromTheme.Parameters.Keys.Order(StringComparer.Ordinal), sheet.Parameters.Keys.Order(StringComparer.Ordinal));
        foreach (var (key, value) in fromTheme.Parameters)
        {
            Assert.Equal(value, sheet.Parameters[key]);
        }
    }

    /// <summary>A figure dressed by a built-in sheet wears its theme's look: the backgrounds, the text colour, the grid and the font
    /// size of the theme of the same name.</summary>
    [Theory]
    [MemberData(nameof(BuiltInSheets))]
    public void AFigureDressedByABuiltInSheet_WearsItsThemesLook(string name)
    {
        var sheet = (StyleSheet)typeof(StyleSheet).GetProperty(name, BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        var theme = name.AsTheme();

        MatPlotLibNet.Models.Figure figure;
        using (Plt.Style.Context(sheet))
        {
            figure = Plt.Create().Plot([1.0, 2.0], [2.0, 1.0]).Build();
        }

        Assert.Equal(theme.Background, figure.Theme.Background);
        Assert.Equal(theme.AxesBackground, figure.Theme.AxesBackground);
        Assert.Equal(theme.DefaultGrid.Visible, figure.Theme.DefaultGrid.Visible);
        Assert.Equal(theme.DefaultGrid.Color, figure.Theme.DefaultGrid.Color);
        Assert.Equal(theme.DefaultFont.Size, figure.Theme.DefaultFont.Size, 9);
    }
}
