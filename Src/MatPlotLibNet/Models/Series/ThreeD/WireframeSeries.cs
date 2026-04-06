// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D wireframe plot rendered as grid lines on a surface.</summary>
public sealed class WireframeSeries : ChartSeries
{
    /// <summary>Gets the X-axis grid coordinates.</summary>
    public double[] X { get; }

    /// <summary>Gets the Y-axis grid coordinates.</summary>
    public double[] Y { get; }

    /// <summary>Gets the 2D matrix of Z values at each grid point.</summary>
    public double[,] Z { get; }

    /// <summary>Gets or sets the wireframe line color.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the wireframe line width.</summary>
    public double LineWidth { get; set; } = 0.5;

    /// <summary>Initializes a new wireframe series with the specified grid data.</summary>
    public WireframeSeries(double[] x, double[] y, double[,] z) { X = x; Y = y; Z = z; }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
