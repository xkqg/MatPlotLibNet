// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Contract for series that back a color bar. Exposes the value range used for the
/// gradient and the colormap applied to it. Implemented by grid / image / density series
/// (heatmap, contourf, hexbin, histogram2D, image, pcolormesh, spectrogram, tripcolor,
/// polar heatmap).</summary>
public interface IColorBarDataProvider
{
    /// <summary>Returns the <c>[Min, Max]</c> data range for the color bar gradient.</summary>
    /// <returns>A <see cref="MinMaxRange"/> spanning the series' scalar range.</returns>
    MinMaxRange GetColorBarRange();

    /// <summary>The colormap used to map values in the range to colors. <see langword="null"/>
    /// falls back to the theme default.</summary>
    IColorMap? ColorMap { get; }
}
