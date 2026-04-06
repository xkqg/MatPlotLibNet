// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a scatter plot in polar coordinates (r, theta).</summary>
public sealed class PolarScatterSeries : ChartSeries
{
    /// <summary>Gets the radial data values.</summary>
    public double[] R { get; }

    /// <summary>Gets the angular data values in radians.</summary>
    public double[] Theta { get; }

    /// <summary>Gets or sets the marker color.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the marker size.</summary>
    public double MarkerSize { get; set; } = 6;

    /// <summary>Initializes a new polar scatter series.</summary>
    public PolarScatterSeries(double[] r, double[] theta) { R = r; Theta = theta; }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
