// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>The parts of a theme a set of rc parameters lays over (<see cref="ThemeRcKey.LaysOver"/>): its colours, its font, its grid and
/// its looks, taken from a base theme and made into a copy of it once every parameter is laid.</summary>
/// <param name="Background">The colour behind the figure.</param>
/// <param name="AxesBackground">The colour of the plot area.</param>
/// <param name="ForegroundText">The colour of the text and the foreground marks.</param>
/// <param name="Font">The font of text that names none.</param>
/// <param name="Grid">The grid.</param>
/// <param name="Looks">The looks (the width of a line).</param>
internal readonly record struct ThemeRcParts(Color Background, Color AxesBackground, Color ForegroundText, Font Font, GridStyle Grid, ThemeLooks Looks)
{
    /// <summary>The parts of <paramref name="theme"/>.</summary>
    /// <param name="theme">The base theme.</param>
    /// <returns>Its parts.</returns>
    internal static ThemeRcParts Of(Theme theme) =>
        new(theme.Background, theme.AxesBackground, theme.ForegroundText, theme.DefaultFont, theme.DefaultGrid, theme.Looks);

    /// <summary>A copy of <paramref name="baseTheme"/>, under its name, with these parts.</summary>
    /// <param name="baseTheme">The theme the parts were taken from.</param>
    /// <returns>The theme.</returns>
    internal Theme Over(Theme baseTheme) => new(baseTheme.Name, baseTheme)
    {
        Background = Background,
        AxesBackground = AxesBackground,
        ForegroundText = ForegroundText,
        DefaultFont = Font,
        DefaultGrid = Grid,
        Looks = Looks,
    };
}
