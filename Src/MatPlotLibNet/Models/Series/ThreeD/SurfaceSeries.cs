// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D surface plot rendered as colored quadrilaterals with optional wireframe.</summary>
public sealed class SurfaceSeries : GridSeries3D
{
    /// <summary>Gets or sets the color map used to color the surface by Z value.</summary>
    public IColorMap? ColorMap { get; set; }

    /// <summary>Gets or sets the surface opacity (0.0 to 1.0).</summary>
    public double Alpha { get; set; } = 0.8;

    /// <summary>Gets or sets whether wireframe edges are drawn on the surface quads.</summary>
    public bool ShowWireframe { get; set; } = true;

    /// <summary>Initializes a new surface series with the specified grid data.</summary>
    public SurfaceSeries(double[] x, double[] y, double[,] z) : base(x, y, z) { }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
