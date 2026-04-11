// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
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

    /// <summary>Gets whether to draw extension slots for under-range and/or over-range values.</summary>
    public ColorBarExtend Extend { get; init; } = ColorBarExtend.Neither;

    /// <summary>Gets the orientation of the color bar. Default is <see cref="ColorBarOrientation.Vertical"/>.</summary>
    public ColorBarOrientation Orientation { get; init; } = ColorBarOrientation.Vertical;

    /// <summary>
    /// Gets the fraction of the plot height (or width for horizontal) used for the bar length.
    /// A value of 1.0 (default) means the full plot dimension; 0.75 means 75%.
    /// </summary>
    public double Shrink { get; init; } = 1.0;

    /// <summary>Gets whether thin edges are drawn between color steps in the gradient. Default is <c>false</c>.</summary>
    public bool DrawEdges { get; init; }

    /// <summary>
    /// Gets the length-to-width aspect ratio of the bar. Used to compute bar width from height when
    /// <see cref="Width"/> has not been set explicitly. Default is 20.
    /// </summary>
    public double Aspect { get; init; } = 20;
}
