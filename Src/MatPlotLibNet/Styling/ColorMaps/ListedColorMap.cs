// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>Discrete colormap that returns the nearest indexed color without interpolation.
/// Suitable for categorical data where no ordering is implied.
/// Satisfies the same <see cref="IColorMap"/> contract as <see cref="LinearColorMap"/>:
/// <c>GetColor(0)</c> returns the first color, <c>GetColor(1)</c> the last.</summary>
public sealed class ListedColorMap : IColorMap
{
    public string Name { get; }

    private readonly Color[] _colors;

    public Color? UnderColor { get; init; }

    public Color? OverColor { get; init; }

    public Color? BadColor { get; init; }

    /// <summary>Creates a listed colormap with the specified discrete colors.</summary>
    public ListedColorMap(string name, Color[] colors)
    {
        Name = name;
        _colors = colors;
    }

    /// <inheritdoc />
    public Color GetColor(double value)
    {
        value = Math.Clamp(value, 0, 1);
        int n = _colors.Length;
        int index = Math.Min((int)(value * n), n - 1);
        return _colors[index];
    }

    /// <inheritdoc />
    Color? IColorMap.GetUnderColor() => UnderColor;

    /// <inheritdoc />
    Color? IColorMap.GetOverColor() => OverColor;

    /// <inheritdoc />
    Color? IColorMap.GetBadColor() => BadColor;
}
