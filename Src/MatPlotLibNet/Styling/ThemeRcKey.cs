// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>One rc parameter a theme carries, both ways: what a theme states for it, as the parameter's value
/// (<see cref="ValueOf"/>, which a style sheet made from the theme writes, <see cref="StyleSheet.FromTheme"/>), and how a value of it
/// is laid over a theme (<see cref="LaysOver"/>, which a set of parameters does, <see cref="RcParams"/>). The keys a theme carries
/// are one list, <see cref="OfATheme"/>, so the two directions cannot name different keys or turn a size another way. The font size,
/// the grid width and the width of a line are matplotlib's points in a parameter and pixels in a theme, at 100 / 72 to the point.</summary>
internal abstract class ThemeRcKey
{
    private protected ThemeRcKey(string key) => Key = key;

    /// <summary>The parameter's name (<see cref="RcParamKeys"/>).</summary>
    internal string Key { get; }

    /// <summary>The value <paramref name="theme"/> states for this parameter, in the parameter's unit; <see langword="null"/> where it
    /// states none (a theme that names no width of a line).</summary>
    /// <param name="theme">The theme.</param>
    /// <returns>The value, or <see langword="null"/>.</returns>
    internal abstract object? ValueOf(Theme theme);

    /// <summary>Lays <paramref name="held"/> over <paramref name="parts"/> when it is a value of this parameter's type that differs from
    /// <paramref name="resting"/>, the library's default: a value of another type, or one equal to the default, says nothing.</summary>
    /// <param name="held">The value the parameters hold under this key.</param>
    /// <param name="resting">The library's default of this key.</param>
    /// <param name="parts">The parts of the theme being laid over.</param>
    /// <returns><see langword="true"/> when the value was laid over.</returns>
    internal abstract bool LaysOver(object held, object resting, ref ThemeRcParts parts);

    /// <summary>The rc parameters a theme carries, in the order a style sheet made from a theme writes them.</summary>
    internal static ThemeRcKey[] OfATheme { get; } =
    [
        new Typed<Color>(RcParamKeys.FigureFaceColor, theme => theme.Background, (parts, value) => parts with { Background = value }),
        new Typed<Color>(RcParamKeys.AxesFaceColor, theme => theme.AxesBackground, (parts, value) => parts with { AxesBackground = value }),
        new Typed<Color>(RcParamKeys.TextColor, theme => theme.ForegroundText,
            (parts, value) => parts with { ForegroundText = value, Font = parts.Font with { Color = value } }),
        new Number(RcParamKeys.FontSize, theme => theme.DefaultFont.Size.PixelsInPoints,
            (parts, value) => parts with { Font = parts.Font with { Size = value.PointsInPixels } }),
        new Typed<string>(RcParamKeys.FontFamily, theme => theme.DefaultFont.Family ?? RcParams.RestingFontFamily,
            (parts, value) => parts with { Font = parts.Font with { Family = value } }),
        new Typed<bool>(RcParamKeys.AxesGrid, theme => theme.DefaultGrid.Visible,
            (parts, value) => parts with { Grid = parts.Grid with { Visible = value } }),
        new Typed<Color>(RcParamKeys.GridColor, theme => theme.DefaultGrid.Color,
            (parts, value) => parts with { Grid = parts.Grid with { Color = value } }),
        new Number(RcParamKeys.GridLineWidth, theme => theme.DefaultGrid.LineWidth.PixelsInPoints,
            (parts, value) => parts with { Grid = parts.Grid with { LineWidth = value.PointsInPixels } }),
        new Number(RcParamKeys.GridAlpha, theme => theme.DefaultGrid.Alpha, (parts, value) => parts with { Grid = parts.Grid with { Alpha = value } }),
        new Number(RcParamKeys.LinesLineWidth, theme => theme.Looks.Lines.Width?.PixelsInPoints,
            (parts, value) => parts with { Looks = parts.Looks with { Lines = parts.Looks.Lines with { Width = value.PointsInPixels } } }),
    ];

    /// <summary>A parameter whose value is of one type (a colour, a name, a flag).</summary>
    private sealed class Typed<T>(string key, Func<Theme, T> valueOf, Func<ThemeRcParts, T, ThemeRcParts> lay) : ThemeRcKey(key)
        where T : notnull
    {
        internal override object? ValueOf(Theme theme) => valueOf(theme);

        internal override bool LaysOver(object held, object resting, ref ThemeRcParts parts)
        {
            if (held is not T value || Equals(value, resting)) return false;
            parts = lay(parts, value);
            return true;
        }
    }

    /// <summary>A parameter whose value is a number: a <see cref="double"/>, or an <see cref="int"/> as a dictionary initializer writes a
    /// whole number.</summary>
    private sealed class Number(string key, Func<Theme, double?> valueOf, Func<ThemeRcParts, double, ThemeRcParts> lay) : ThemeRcKey(key)
    {
        internal override object? ValueOf(Theme theme) => valueOf(theme);

        internal override bool LaysOver(object held, object resting, ref ThemeRcParts parts)
        {
            double value = held switch { double d => d, int i => i, _ => double.NaN };
            if (double.IsNaN(value) || value == (double)resting) return false;
            parts = lay(parts, value);
            return true;
        }
    }
}
