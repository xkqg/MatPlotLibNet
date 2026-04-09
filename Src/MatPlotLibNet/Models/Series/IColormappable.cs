// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Series that supports colormap assignment for scalar-to-color mapping.</summary>
public interface IColormappable
{
    /// <summary>Gets or sets the colormap used to map scalar values to colors, or null for the default.</summary>
    IColorMap? ColorMap { get; set; }
}
