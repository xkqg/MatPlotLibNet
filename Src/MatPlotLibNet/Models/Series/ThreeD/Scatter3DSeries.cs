// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D scatter plot rendered as projected circles.</summary>
public sealed class Scatter3DSeries : ChartSeries, IHasDataRange
{
    /// <summary>Gets the X data values.</summary>
    public double[] X { get; }

    /// <summary>Gets the Y data values.</summary>
    public double[] Y { get; }

    /// <summary>Gets the Z data values.</summary>
    public double[] Z { get; }

    /// <summary>Gets or sets the marker color.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the marker size in pixels.</summary>
    public double MarkerSize { get; set; } = 6;

    /// <summary>Initializes a new 3D scatter series with the specified data.</summary>
    public Scatter3DSeries(double[] x, double[] y, double[] z) { X = x; Y = y; Z = z; }

    /// <inheritdoc />
    public DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
