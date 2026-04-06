// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a bar chart in polar coordinates with wedge-shaped bars.</summary>
public sealed class PolarBarSeries : ChartSeries
{
    /// <summary>Gets the radial data values (bar heights).</summary>
    public double[] R { get; }

    /// <summary>Gets the angular data values in radians (bar center angles).</summary>
    public double[] Theta { get; }

    /// <summary>Gets or sets the angular width of each bar in radians.</summary>
    public double BarWidth { get; set; } = 0.3;

    /// <summary>Gets or sets the bar color.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the bar opacity.</summary>
    public double Alpha { get; set; } = 0.8;

    /// <summary>Initializes a new polar bar series.</summary>
    public PolarBarSeries(double[] r, double[] theta) { R = r; Theta = theta; }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
