// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Series that supports colormap assignment for scalar-to-color mapping.</summary>
public interface IColormappable
{
    /// <summary>The colour map the series' values are drawn through; <see langword="null"/> for its renderer's default.
    /// A map built in code keeps its colours and its under, over and bad colours through JSON; a map of a type of your
    /// own does not unless it is registered (see <see cref="IColorMap"/>).</summary>
    IColorMap? ColorMap { get; set; }
}
