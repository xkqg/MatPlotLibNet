// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>An immutable named set of <see cref="RcParams"/> overrides that can be applied globally or in a scope.</summary>
public sealed class StyleSheet
{
    /// <summary>Gets the style sheet name (e.g. "dark", "seaborn").</summary>
    public string Name { get; }

    /// <summary>Gets the parameter overrides defined by this style sheet.</summary>
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

    /// <summary>Creates a <see cref="StyleSheet"/> from an existing <see cref="Theme"/> by mapping theme properties to rcParam keys.</summary>
    public static StyleSheet FromTheme(Theme theme) => new(theme.Name, new Dictionary<string, object>
    {
        [RcParamKeys.FigureFaceColor]     = theme.Background,
        [RcParamKeys.AxesFaceColor]       = theme.AxesBackground,
        [RcParamKeys.TextColor]           = theme.ForegroundText,
        [RcParamKeys.FontSize]            = theme.DefaultFont.Size,
        [RcParamKeys.FontFamily]          = theme.DefaultFont.Family ?? "sans-serif",
        [RcParamKeys.AxesGrid]            = theme.DefaultGrid.Visible,
        [RcParamKeys.GridColor]           = theme.DefaultGrid.Color,
        [RcParamKeys.GridLineWidth]       = theme.DefaultGrid.LineWidth,
        [RcParamKeys.GridAlpha]           = theme.DefaultGrid.Alpha,
    });

    // ── Built-in style sheets ─────────────────────────────────────────────────

    /// <summary>Gets the default matplotlib-like white background style.</summary>
    public static StyleSheet Default { get; } = FromTheme(Theme.Default);

    /// <summary>Gets a dark background style.</summary>
    public static StyleSheet Dark { get; } = FromTheme(Theme.Dark);

    /// <summary>Gets a seaborn-inspired muted style (light grid, muted colors).</summary>
    public static StyleSheet Seaborn { get; } = new("seaborn", new Dictionary<string, object>
    {
        [RcParamKeys.FigureFaceColor]  = Color.FromHex("#eaeaf2"),
        [RcParamKeys.AxesFaceColor]    = Color.FromHex("#eaeaf2"),
        [RcParamKeys.AxesGrid]         = true,
        [RcParamKeys.GridColor]        = Colors.White,
        [RcParamKeys.GridLineWidth]    = 1.0,
        [RcParamKeys.LinesLineWidth]   = 1.5,
        [RcParamKeys.FontSize]         = 11.0,
        [RcParamKeys.TextColor]        = Color.FromHex("#262626"),
    });

    /// <summary>Gets a ggplot2-inspired grey background style.</summary>
    public static StyleSheet Ggplot { get; } = new("ggplot", new Dictionary<string, object>
    {
        [RcParamKeys.FigureFaceColor]  = Color.FromHex("#e5e5e5"),
        [RcParamKeys.AxesFaceColor]    = Color.FromHex("#e5e5e5"),
        [RcParamKeys.AxesGrid]         = true,
        [RcParamKeys.GridColor]        = Colors.White,
        [RcParamKeys.GridLineWidth]    = 0.5,
        [RcParamKeys.LinesLineWidth]   = 1.75,
        [RcParamKeys.FontSize]         = 10.0,
    });
}
