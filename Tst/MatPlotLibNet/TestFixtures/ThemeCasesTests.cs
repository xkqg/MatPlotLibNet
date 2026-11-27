// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests;

/// <summary>The theme rows the tests run over, by name, and the one reading of a name as a theme.</summary>
public sealed class ThemeCasesTests
{
    [Theory]
    [InlineData("v2")]
    [InlineData("matplotlib-v2")]
    [InlineData("MatplotlibV2")]
    public void TheMatplotlibDefaultStyle_IsFoundUnderEachOfItsNames(string name) => Assert.Same(Theme.MatplotlibV2, name.AsTheme());

    [Theory]
    [InlineData("classic")]
    [InlineData("matplotlib-classic")]
    [InlineData("MatplotlibClassic")]
    public void TheMatplotlibClassicStyle_IsFoundUnderEachOfItsNames(string name) => Assert.Same(Theme.MatplotlibClassic, name.AsTheme());

    [Fact]
    public void TheLibrariesOwnThemes_AreFoundByTheShortNameAndByTheirName()
    {
        Assert.Same(Theme.Default, "default".AsTheme());
        Assert.Same(Theme.Default, "library".AsTheme());
        Assert.Same(Theme.Default, "Default".AsTheme());
        Assert.Same(Theme.Dark, "dark".AsTheme());
        Assert.Same(Theme.Seaborn, "seaborn".AsTheme());
        Assert.Same(Theme.Ggplot, "Ggplot".AsTheme());
    }

    [Fact]
    public void ANameThatIsNoTheme_IsRefusedByName()
    {
        var ex = Assert.Throws<ArgumentException>(() => "no-such-theme".AsTheme());

        Assert.Contains("no-such-theme", ex.Message, StringComparison.Ordinal);
    }

    private static string[] Names(TheoryData<string> rows) => [.. rows.Select(row => row.Data)];

    [Fact]
    public void TheRowSets_AreTheNamesTheTestsRunOver_AndEveryNameIsATheme()
    {
        Assert.Equal(["v2", "classic"], Names(ThemeCases.Matplotlib));
        Assert.Equal(["v2", "classic", "default"], Names(ThemeCases.MatplotlibAndDefault));
        Assert.Equal(["default", "dark", "seaborn"], Names(ThemeCases.Library));
        Assert.Equal(["default", "dark", "v2", "classic"], Names(ThemeCases.DefaultDarkAndMatplotlib));
        Assert.Equal(8, Names(ThemeCases.Eight).Length);
        string[] shipped = [.. typeof(Theme).GetProperties(BindingFlags.Public | BindingFlags.Static).Where(p => p.PropertyType == typeof(Theme)).Select(p => p.Name)];
        Assert.Equal(shipped, Names(ThemeCases.BuiltIn));
        foreach (var set in new[] { ThemeCases.Matplotlib, ThemeCases.MatplotlibAndDefault, ThemeCases.Library, ThemeCases.DefaultDarkAndMatplotlib, ThemeCases.Eight, ThemeCases.BuiltIn })
        {
            Assert.All(Names(set), name => Assert.NotNull(name.AsTheme()));
        }
    }
}
