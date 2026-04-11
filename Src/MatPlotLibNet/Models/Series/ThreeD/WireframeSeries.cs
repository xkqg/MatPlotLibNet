// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D wireframe plot rendered as grid lines on a surface.</summary>
public sealed class WireframeSeries : GridSeries3D
{
    public Color? Color { get; set; }

    public double LineWidth { get; set; } = 0.5;

    /// <summary>Initializes a new wireframe series with the specified grid data.</summary>
    public WireframeSeries(double[] x, double[] y, double[,] z) : base(x, y, z) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new() { Type = "wireframe" };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
