// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D surface plot rendered as colored quadrilaterals with optional wireframe.</summary>
public sealed class SurfaceSeries : GridSeries3D, IColormappable, INormalizable
{
    /// <summary>Gets or sets the color map used to color the surface by Z value.</summary>
    public IColorMap? ColorMap { get; set; }

    /// <summary>Gets or sets the surface opacity (0.0 to 1.0).</summary>
    public double Alpha { get; set; } = 0.8;

    /// <summary>Gets or sets whether wireframe edges are drawn on the surface quads.</summary>
    public bool ShowWireframe { get; set; } = true;

    /// <summary>Gets or sets the normalizer for mapping Z values to colormap range, or null for linear normalization.</summary>
    public INormalizer? Normalizer { get; set; }

    /// <summary>Gets or sets the wireframe edge color. When null, a semi-transparent black (<c>rgba(0,0,0,80)</c>) is used.</summary>
    public Color? EdgeColor { get; set; }

    /// <summary>Gets or sets how many rows to skip between rendered wireframe lines (1 = every row, 2 = every other row). Default 1.</summary>
    public int RowStride { get; set; } = 1;

    /// <summary>Gets or sets how many columns to skip between rendered wireframe lines (1 = every column). Default 1.</summary>
    public int ColStride { get; set; } = 1;

    /// <summary>Initializes a new surface series with the specified grid data.</summary>
    public SurfaceSeries(double[] x, double[] y, double[,] z) : base(x, y, z) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new() { Type = "surface" };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
