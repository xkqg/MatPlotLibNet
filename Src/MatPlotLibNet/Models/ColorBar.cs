// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models;

/// <summary>Configures a color bar that displays a colormap gradient alongside the plot area.</summary>
public sealed record ColorBar
{
    /// <summary>Whether the colour bar is drawn. Off by default, so a configured bar stays hidden until this is switched on.</summary>
    public bool Visible { get; init; }

    /// <summary>The colour map the gradient is drawn with. <see langword="null"/> (the default) takes the colour map of the
    /// first series that provides one, and viridis when no series does.</summary>
    public IColorMap? ColorMap { get; init; }

    /// <summary>The value at the low end of the bar. Used when no series on the axes provides a range of its own, which
    /// takes precedence; default 0.</summary>
    public double Min { get; init; }

    /// <summary>The value at the high end of the bar. Used when no series on the axes provides a range of its own, which
    /// takes precedence; default 1.</summary>
    public double Max { get; init; } = 1;

    /// <summary>A caption drawn beside the bar, rotated to run along it for a vertical bar. <see langword="null"/> (the
    /// default) draws none.</summary>
    public string? Label { get; init; }

    /// <summary>The thickness of the bar, in pixels. Default 20. A horizontal bar uses it only when <see cref="Aspect"/>
    /// is 0 or less.</summary>
    public double Width { get; init; } = 20;

    /// <summary>The gap between the plot area and the bar, in pixels. Default 10.</summary>
    public double Padding { get; init; } = 10;

    /// <summary>Whether the bar grows an extra slot past its low end, its high end or both, for the values outside the
    /// range; a slot shows the colour map's under or over colour. Neither end by default.</summary>
    public ColorBarExtend Extend { get; init; } = ColorBarExtend.Neither;

    /// <summary>Whether the bar runs vertically beside the plot area (the default) or horizontally below it.</summary>
    public ColorBarOrientation Orientation { get; init; } = ColorBarOrientation.Vertical;

    /// <summary>The length of the bar as a fraction of the plot area's side it runs along, centred on it: 1 (the default)
    /// spans the whole side, 0.5 half of it.</summary>
    public double Shrink { get; init; } = 1.0;

    /// <summary>Whether thin lines are drawn between the steps of the gradient. Off by default.</summary>
    public bool DrawEdges { get; init; }

    /// <summary>The ratio of a horizontal bar's length to its thickness. Default 20; with a value of 0 or less the bar is
    /// <see cref="Width"/> thick instead. A vertical bar always takes its thickness from <see cref="Width"/>.</summary>
    public double Aspect { get; init; } = 20;
}
