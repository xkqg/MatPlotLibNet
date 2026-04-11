// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>Decorator that reverses a colormap by inverting the normalized input value.</summary>
public sealed class ReversedColorMap : IColorMap
{
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
