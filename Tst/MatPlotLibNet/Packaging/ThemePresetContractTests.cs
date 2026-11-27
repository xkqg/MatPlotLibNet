// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>A theme preset is a base theme and what differs from it, said once: declared as the theme's own property, built by the one
/// fluent builder (<c>ThemeBuilder</c>, each of its verbs making the next theme in one place), its grounds (the figure, the plot, the
/// ink) one <c>ThemeGround</c>, and a palette it shares with a colour map read from the map's table. The rows of
/// <c>OwnedRules.ThemeRules</c> hold the source rules; the facts below hold the table, the constructor and the lists of names.</summary>
public class ThemePresetContractTests
{
    /// <summary>The Okabe-Ito palette (the eight colours a colour-blind reader tells apart) is a table of the qualitative colour maps; the
    /// colour-blind-safe theme cycles it from there, so no second file writes the eight colours.</summary>
    [Fact]
    public void TheOkabeItoTable_IsWrittenInOneFile()
    {
        string[] hexes = ["#E69F00", "#56B4E9", "#009E73", "#F0E442", "#0072B2", "#D55E00", "#CC79A7", "#000000"];

        var writing = RepositoryTree.SourceFiles()
            .Where(file => file.Path.StartsWith("Src/", StringComparison.Ordinal)
                           && hexes.All(hex => file.Text.Contains(hex, StringComparison.OrdinalIgnoreCase)))
            .Select(file => file.Path);

        Assert.Equal(["Src/MatPlotLibNet/Styling/ColorMaps/QualitativeColorMaps.cs"], writing);
    }

    /// <summary>The themes built with the full constructor, each naming every value itself: DATA, because each transcribes a published
    /// style whose values must not follow the library's default theme. Every other theme is a preset of one of them.</summary>
    private static readonly string[] RootThemes =
    [
        "Default",         // the library's own plain light theme
        "Dark",            // the library's own dark theme
        "Seaborn",         // seaborn's darkgrid style (seaborn-v0_8-darkgrid): its axes, grid, ink, font and cycle
        "Ggplot",          // matplotlib's ggplot.mplstyle
        "Bmh",             // matplotlib's bmh.mplstyle
        "FiveThirtyEight", // matplotlib's fivethirtyeight.mplstyle
        "HighContrast",    // the WCAG AAA (7 : 1) target: pure white and pure black are the requirement, not the default's choice
    ];

    /// <summary>A theme built with the full constructor names every value, so it restates every value it shares with a base; only the
    /// root themes may (<see cref="RootThemes"/>), and a theme that keeps a base's ground, font and grid is that base's preset.</summary>
    [Fact]
    public void OnlyTheRootThemes_AreBuiltWithTheFullConstructor()
    {
        var theme = RepositoryTree.SourceFiles().Single(f => f.Path == "Src/MatPlotLibNet/Styling/Theme.cs");

        var built = Regex.Matches(theme.Text, @"public static Theme (\w+) \{ get; \} = new\(").Select(m => m.Groups[1].Value);

        Assert.Equal(RootThemes, built);
    }
}
