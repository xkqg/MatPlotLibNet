// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>Decorator that reverses a colormap by inverting the normalized input value.</summary>
/// <remarks>The figure JSON writes the reverse of a map the library ships by the reversed name (<c>viridis_r</c>), the map
/// the registry holds under it; the reverse of any other map as the map it reverses. The reverse of a map of your own
/// type that nobody registered cannot be written, and is left out.</remarks>
public sealed class ReversedColorMap : IColorMap, IJsonDescribed<ColorMapDto>
{
    /// <inheritdoc />
    /// <remarks>The registry holds the reverse of every map the library ships under this map's name, over the same map,
    /// so that name draws exactly this map.</remarks>
    ColorMapDto? IJsonDescribed<ColorMapDto>.Describe() =>
        ColorMapRegistry.Get(Name) is ReversedColorMap registered && ReferenceEquals(registered._inner, _inner) && ColorMapRegistry.IsShipped(registered)
            ? new NamedColorMapDto(Name)
            : _inner.Describe() is { } inner ? new ReversedColorMapDto(inner) : null;

    private readonly IColorMap _inner;

    /// <inheritdoc />
    public string Name { get; }

    /// <summary>Creates a reversed version of the given colormap.</summary>
    public ReversedColorMap(IColorMap inner)
    {
        _inner = inner;
        Name = inner.Name.EndsWith("_r") ? inner.Name[..^2] : $"{inner.Name}_r";
    }

    /// <inheritdoc />
    public Color GetColor(double value) => _inner.GetColor(1.0 - Math.Clamp(value, 0, 1));

    /// <inheritdoc />
    /// <remarks>When reversed, the under-range corresponds to the inner map's over-range.</remarks>
    Color? IColorMap.GetUnderColor() => _inner.GetOverColor();

    /// <inheritdoc />
    /// <remarks>When reversed, the over-range corresponds to the inner map's under-range.</remarks>
    Color? IColorMap.GetOverColor() => _inner.GetUnderColor();

    /// <inheritdoc />
    Color? IColorMap.GetBadColor() => _inner.GetBadColor();
}
