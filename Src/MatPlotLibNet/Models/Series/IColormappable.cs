// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Series that supports colormap assignment for scalar-to-color mapping.</summary>
public interface IColormappable
{
    IColorMap? ColorMap { get; set; }
}
