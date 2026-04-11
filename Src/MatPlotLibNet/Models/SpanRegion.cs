// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Represents a shaded rectangular region spanning the full width or height of the axes.</summary>
public sealed class SpanRegion
{
    public double Min { get; }

    public double Max { get; }

    public Orientation Orientation { get; }

    public Color? Color { get; set; }

    public double Alpha { get; set; } = 0.2;

    /// <summary>Creates a new span region between the given data values.</summary>
    public SpanRegion(double min, double max, Orientation orientation)
    {
        Min = min;
        Max = max;
        Orientation = orientation;
    }
}
