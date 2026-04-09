// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>
/// Maps a scalar value in the range [0, 1] to a <see cref="Color"/>.
/// </summary>
public interface IColorMap
{
    /// <summary>Gets the name of this color map.</summary>
    string Name { get; }

    /// <summary>
    /// Returns the color corresponding to the given normalized value.
    /// </summary>
    /// <param name="value">A value between 0.0 and 1.0.</param>
    /// <returns>The interpolated color.</returns>
    Color GetColor(double value);
}
