// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>
/// Maps a scalar value in the range [0, 1] to a <see cref="Color"/>.
/// </summary>
public interface IColorMap
{
    string Name { get; }

    /// <summary>
    /// Returns the color corresponding to the given normalized value.
    /// </summary>
    /// <param name="value">A value between 0.0 and 1.0.</param>
    /// <returns>The interpolated color.</returns>
    Color GetColor(double value);

    /// <summary>Returns the color for values below the minimum (under-range), or <c>null</c> to clamp to the first stop.</summary>
    Color? GetUnderColor() => null;

    /// <summary>Returns the color for values above the maximum (over-range), or <c>null</c> to clamp to the last stop.</summary>
    Color? GetOverColor() => null;

    /// <summary>Returns the color for masked or invalid (NaN) values, or <c>null</c> if not set.</summary>
    Color? GetBadColor() => null;
}
