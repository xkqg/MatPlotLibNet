// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a line plot in polar coordinates (r, theta).</summary>
public sealed class PolarLineSeries : ChartSeries
{
    /// <summary>Gets the radial data values.</summary>
    public double[] R { get; }

    /// <summary>Gets the angular data values in radians.</summary>
    public double[] Theta { get; }

    /// <summary>Gets or sets the line color.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the line style.</summary>
    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    /// <summary>Gets or sets the line width.</summary>
    public double LineWidth { get; set; } = 1.5;

    /// <summary>Initializes a new polar line series.</summary>
    public PolarLineSeries(double[] r, double[] theta) { R = r; Theta = theta; }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
