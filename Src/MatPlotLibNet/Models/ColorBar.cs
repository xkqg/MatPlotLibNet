// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models;

/// <summary>Configures a color bar that displays a colormap gradient alongside the plot area.</summary>
public sealed record ColorBar
{
    /// <summary>Gets whether the color bar is visible.</summary>
    public bool Visible { get; init; }

    /// <summary>Gets the color map to display.</summary>
    public IColorMap? ColorMap { get; init; }

    /// <summary>Gets the minimum data value for the color bar range.</summary>
    public double Min { get; init; }

    /// <summary>Gets the maximum data value for the color bar range.</summary>
    public double Max { get; init; } = 1;

    /// <summary>Gets the label text shown alongside the color bar.</summary>
    public string? Label { get; init; }

    /// <summary>Gets the width of the color bar in pixels.</summary>
    public double Width { get; init; } = 20;

    /// <summary>Gets the padding between the plot area and the color bar in pixels.</summary>
    public double Padding { get; init; } = 10;
}
