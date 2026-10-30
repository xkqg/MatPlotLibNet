// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>Discrete colormap that returns the nearest indexed color without interpolation.
/// Suitable for categorical data where no ordering is implied.
/// Satisfies the same <see cref="IColorMap"/> contract as <see cref="LinearColorMap"/>:
/// <c>GetColor(0)</c> returns the first color, <c>GetColor(1)</c> the last.</summary>
/// <remarks>A map built in code travels through the figure JSON as its colours and its under, over and bad colours; a
/// map the library ships travels by its name.</remarks>
public sealed class ListedColorMap : IColorMap, IJsonDescribed<ColorMapDto>
{
    /// <inheritdoc />
    ColorMapDto IJsonDescribed<ColorMapDto>.Describe() => new ListedColorMapDto(Name, _colors, UnderColor, OverColor, BadColor);

    /// <inheritdoc />
    public string Name { get; }

    private readonly Color[] _colors;

    /// <summary>The colour of values below the scale; <see langword="null"/> draws them in the first colour.</summary>
    public Color? UnderColor { get; init; }

    /// <summary>The colour of values above the scale; <see langword="null"/> draws them in the last colour.</summary>
    public Color? OverColor { get; init; }

    /// <summary>The colour of missing (NaN) values; <see langword="null"/> draws them transparent.</summary>
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
        // A colour index is never computed from NaN: net10.0 cast it to 0 (the first colour), net8.0 threw.
        if (value.IsMissing()) return this.MissingValueColor();
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
