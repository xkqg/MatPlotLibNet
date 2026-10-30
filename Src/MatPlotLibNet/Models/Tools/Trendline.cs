// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Tools;

/// <summary>Represents a trendline drawn between two data-coordinate points on an axes.</summary>
public sealed class Trendline
{
    /// <summary>The X data coordinate of the first endpoint.</summary>
    public double X1 { get; }
    /// <summary>The Y data coordinate of the first endpoint.</summary>
    public double Y1 { get; }
    /// <summary>The X data coordinate of the second endpoint.</summary>
    public double X2 { get; }
    /// <summary>The Y data coordinate of the second endpoint.</summary>
    public double Y2 { get; }

    /// <summary>The colour of the line and its label. <see langword="null"/> (the default) takes the theme's neutral
    /// colour.</summary>
    public Color? Color { get; set; }
    /// <summary>The dash pattern of the line. Solid by default.</summary>
    public LineStyle LineStyle { get; set; } = LineStyle.Solid;
    /// <summary>The width of the line, in pixels. Default 1.</summary>
    public double LineWidth { get; set; } = 1.0;
    /// <summary>Text drawn at the end of the line that is drawn, in the line's colour. <see langword="null"/> (the default)
    /// draws none.</summary>
    public string? Label { get; set; }

    /// <summary>When true, the line is extended beyond both endpoints across the full plot width.</summary>
    public bool IsExtended { get; set; }

    /// <summary>Creates a trendline between two data-coordinate points.</summary>
    /// <param name="x1">The X data coordinate of the first endpoint.</param>
    /// <param name="y1">The Y data coordinate of the first endpoint.</param>
    /// <param name="x2">The X data coordinate of the second endpoint.</param>
    /// <param name="y2">The Y data coordinate of the second endpoint.</param>
    public Trendline(double x1, double y1, double x2, double y2)
    {
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
    }
}
