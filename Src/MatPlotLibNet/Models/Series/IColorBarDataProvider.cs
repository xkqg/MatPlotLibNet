// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Series that provides data range and colormap for color bar rendering.</summary>
public interface IColorBarDataProvider
{
    /// <summary>Gets the min and max data values for the color bar gradient.</summary>
    (double Min, double Max) GetColorBarRange();

    IColorMap? ColorMap { get; }
}
