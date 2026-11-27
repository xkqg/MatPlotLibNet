// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Playground;

/// <summary>The themes the Playground offers, in the groups its picker shows them in, and the one place that names each by the
/// property of <see cref="Theme"/> that holds it: the picker's label and the <c>Theme.X</c> of the snippet the reader copies.</summary>
public static class ThemeChoices
{
    /// <summary>The light themes.</summary>
    public static IReadOnlyList<ThemeChoice> Light { get; } =
    [
        new(Theme.Default,          "Default"),
        new(Theme.Seaborn,          "Seaborn"),
        new(Theme.Ggplot,           "Ggplot"),
        new(Theme.FiveThirtyEight,  "FiveThirtyEight"),
        new(Theme.Bmh,              "Bmh"),
        new(Theme.Solarize,         "Solarize"),
        new(Theme.Grayscale,        "Grayscale"),
        new(Theme.Paper,            "Paper"),
        new(Theme.Presentation,     "Presentation"),
        new(Theme.Poster,           "Poster"),
        new(Theme.GitHub,           "GitHub"),
        new(Theme.Minimal,          "Minimal"),
        new(Theme.Retro,            "Retro"),
        new(Theme.MatplotlibClassic, "MatplotlibClassic"),
        new(Theme.MatplotlibV2,     "MatplotlibV2"),
    ];

    /// <summary>The dark themes.</summary>
    public static IReadOnlyList<ThemeChoice> Dark { get; } =
    [
        new(Theme.Dark,       "Dark"),
        new(Theme.Cyberpunk,  "Cyberpunk"),
        new(Theme.Nord,       "Nord"),
        new(Theme.Dracula,    "Dracula"),
        new(Theme.Monokai,    "Monokai"),
        new(Theme.Catppuccin, "Catppuccin"),
        new(Theme.Gruvbox,    "Gruvbox"),
        new(Theme.OneDark,    "OneDark"),
        new(Theme.Neon,       "Neon"),
    ];

    /// <summary>The themes made for readers who need more contrast or tell colours apart less.</summary>
    public static IReadOnlyList<ThemeChoice> Accessibility { get; } =
    [
        new(Theme.ColorBlindSafe, "ColorBlindSafe"),
        new(Theme.HighContrast,   "HighContrast"),
    ];

    /// <summary>Every choice, the light ones first, then the dark, then the accessible: the order of the picker's values.</summary>
    public static IReadOnlyList<ThemeChoice> All { get; } = [.. Light, .. Dark, .. Accessibility];

    /// <summary>The name of the property of <see cref="Theme"/> that holds <paramref name="theme"/>, as the snippet writes it.</summary>
    /// <param name="theme">The theme the figure is drawn with.</param>
    /// <returns>Its label among the choices, <c>Default</c> for a theme that is not one of them.</returns>
    public static string CodeNameOf(Theme theme)
    {
        foreach (var choice in All)
            if (ReferenceEquals(choice.Theme, theme)) return choice.Label;
        return "Default";
    }
}
