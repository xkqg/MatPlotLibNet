// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models;

/// <summary>Configures a color bar that displays a colormap gradient alongside the plot area.</summary>
public sealed record ColorBar
{
    public bool Visible { get; init; }

    public IColorMap? ColorMap { get; init; }

    public double Min { get; init; }

    public double Max { get; init; } = 1;

    public string? Label { get; init; }

    public double Width { get; init; } = 20;

    public double Padding { get; init; } = 10;

    public ColorBarExtend Extend { get; init; } = ColorBarExtend.Neither;

    public ColorBarOrientation Orientation { get; init; } = ColorBarOrientation.Vertical;

    public double Shrink { get; init; } = 1.0;

    public bool DrawEdges { get; init; }

    public double Aspect { get; init; } = 20;
}
