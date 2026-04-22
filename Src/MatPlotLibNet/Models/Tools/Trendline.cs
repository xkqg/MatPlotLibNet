// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Tools;

/// <summary>Represents a trendline drawn between two data-coordinate points on an axes.</summary>
public sealed class Trendline
{
    public double X1 { get; }
    public double Y1 { get; }
    public double X2 { get; }
    public double Y2 { get; }

    public Color? Color { get; set; }
    public LineStyle LineStyle { get; set; } = LineStyle.Solid;
    public double LineWidth { get; set; } = 1.0;
    public string? Label { get; set; }

    /// <summary>When true, the line is extended beyond both endpoints across the full plot width.</summary>
    public bool IsExtended { get; set; }

    public Trendline(double x1, double y1, double x2, double y2)
    {
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
    }
}
