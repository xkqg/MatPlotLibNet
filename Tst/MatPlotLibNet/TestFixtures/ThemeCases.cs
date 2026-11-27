// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests;

/// <summary>The theme rows the tests run over, by name, and the one reading of a name as a theme: a theory takes the name and the test
/// reads it with <see cref="AsTheme"/>.</summary>
public static class ThemeCases
{
    /// <summary>The two styles that copy matplotlib: its default one and its classic one.</summary>
    public static TheoryData<string> Matplotlib => ["v2", "classic"];

    /// <summary>The two matplotlib styles and the library's own default theme.</summary>
    public static TheoryData<string> MatplotlibAndDefault => ["v2", "classic", "default"];

    /// <summary>The library's own themes: the default, the dark one and the seaborn one.</summary>
    public static TheoryData<string> Library => ["default", "dark", "seaborn"];

    /// <summary>The library's default and dark themes and the two matplotlib styles.</summary>
    public static TheoryData<string> DefaultDarkAndMatplotlib => ["default", "dark", "v2", "classic"];

    /// <summary>Eight themes that differ in their colour cycle: the library's six and the two matplotlib styles.</summary>
    public static TheoryData<string> Eight => ["Default", "Dark", "Seaborn", "Ggplot", "ColorBlindSafe", "HighContrast", "MatplotlibV2", "MatplotlibClassic"];

    /// <summary>Every theme the library ships, by the name of its property.</summary>
    public static TheoryData<string> BuiltIn { get; } =
        new(typeof(Theme).GetProperties(BindingFlags.Public | BindingFlags.Static).Where(p => p.PropertyType == typeof(Theme)).Select(p => p.Name));

    extension(string name)
    {
        /// <summary>The theme a row names: the short names the rows use (<c>v2</c>, <c>classic</c>, <c>default</c>, <c>dark</c>, <c>seaborn</c>),
        /// the matplotlib style names, or the name of any built-in theme.</summary>
        /// <exception cref="ArgumentException">The name is no theme's.</exception>
        public Theme AsTheme() => name switch
        {
            "v2" or "matplotlib-v2" => Theme.MatplotlibV2,
            "classic" or "matplotlib-classic" => Theme.MatplotlibClassic,
            "default" or "library" => Theme.Default,
            "dark" => Theme.Dark,
            "seaborn" => Theme.Seaborn,
            _ => (Theme?)typeof(Theme).GetProperty(name, BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
                ?? throw new ArgumentException($"No theme is called '{name}'.", nameof(name)),
        };
    }
}
