// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>An immutable named set of <see cref="RcParams"/> overrides that can be applied globally or in a scope.</summary>
public sealed class StyleSheet
{
    /// <summary>The style sheet's name, such as <c>seaborn</c>; a sheet made from a theme carries the theme's name.</summary>
    public string Name { get; }

    /// <summary>The rcParam overrides this sheet applies, keyed by the names in <see cref="RcParamKeys"/>.</summary>
    public IReadOnlyDictionary<string, object> Parameters { get; }

    /// <summary>Initializes a new <see cref="StyleSheet"/> with the given name and parameters.</summary>
    public StyleSheet(string name, IReadOnlyDictionary<string, object> parameters)
    {
        Name = name;
        Parameters = parameters;
    }

    /// <summary>Initializes a new <see cref="StyleSheet"/> from a mutable dictionary (copied on construction).</summary>
    public StyleSheet(string name, Dictionary<string, object> parameters)
        : this(name, (IReadOnlyDictionary<string, object>)parameters) { }

    /// <summary>Creates a <see cref="StyleSheet"/> from an existing <see cref="Theme"/> by mapping theme properties to rcParam keys.
    /// The font size, the grid width and the width of a line are written in points, the unit matplotlib's rcParams state them in, so
    /// the sheet laid over any theme gives back this theme's sizes; the width of a line (<c>lines.linewidth</c>) only where the theme
    /// names one, as the matplotlib themes do (a library theme names none and keeps each kind's own). As every key of a style, a value
    /// equal to the library's defaults (<see cref="RcParams"/>) says nothing laid over a theme.</summary>
    public static StyleSheet FromTheme(Theme theme)
    {
        var parameters = new Dictionary<string, object>();
        foreach (var key in ThemeRcKey.OfATheme)
        {
            if (key.ValueOf(theme) is { } value) parameters[key.Key] = value;
        }

        return new(theme.Name, parameters);
    }

    // ── Built-in style sheets: each is the sheet of the theme of its name, so a look is written once, as the theme ──────

    /// <summary>The style sheet of the default theme (<see cref="Theme.Default"/>).</summary>
    public static StyleSheet Default { get; } = FromTheme(Theme.Default);

    /// <summary>The style sheet of the dark theme (<see cref="Theme.Dark"/>).</summary>
    public static StyleSheet Dark { get; } = FromTheme(Theme.Dark);

    /// <summary>The style sheet of the seaborn theme (<see cref="Theme.Seaborn"/>): a white figure, light blue-grey axes with white
    /// grid lines, and dark grey text.</summary>
    public static StyleSheet Seaborn { get; } = FromTheme(Theme.Seaborn);

    /// <summary>The style sheet of the ggplot theme (<see cref="Theme.Ggplot"/>): a white figure and grey axes with white grid
    /// lines.</summary>
    public static StyleSheet Ggplot { get; } = FromTheme(Theme.Ggplot);
}
