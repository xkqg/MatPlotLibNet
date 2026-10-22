// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>
/// Maps a scalar value in the range [0, 1] to a <see cref="Color"/>.
/// </summary>
/// <remarks>How a map travels through the figure JSON (<c>ToJson</c>/<c>FromJson</c>, the MCP server, the transports):
/// a map the library ships by its name; a <see cref="LinearColorMap"/>, <see cref="ListedColorMap"/> or
/// <see cref="ReversedColorMap"/> built in code as what it is made of (its colours, their positions, its under, over and
/// bad colours), so it comes back as itself. A map of a type you write yourself is code, which a JSON document cannot
/// carry: it travels only by the name it is registered under (<see cref="ColorMapRegistry.Register"/>), so a reader that
/// registered it too makes it again; unregistered, it is left out, and the series reads back on its renderer's default
/// colour map.</remarks>
public interface IColorMap
{
    /// <summary>The map's name: the name it is registered and written to JSON under when it travels by its name.</summary>
    string Name { get; }

    /// <summary>
    /// Returns the color corresponding to the given normalized value.
    /// </summary>
    /// <param name="value">A value between 0.0 and 1.0, or NaN for a missing value. The built-in maps draw a missing
    /// value in <see cref="GetBadColor"/>, transparent when that is <see langword="null"/>, as matplotlib does.</param>
    /// <returns>The interpolated color.</returns>
    Color GetColor(double value);

    /// <summary>Returns the color for values below the minimum (under-range), or <c>null</c> to clamp to the first stop.</summary>
    Color? GetUnderColor() => null;

    /// <summary>Returns the color for values above the maximum (over-range), or <c>null</c> to clamp to the last stop.</summary>
    Color? GetOverColor() => null;

    /// <summary>Returns the color for masked or invalid (NaN) values, or <c>null</c> if not set.</summary>
    Color? GetBadColor() => null;
}
