// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D surface plot rendered as colored quadrilaterals with optional wireframe.</summary>
public sealed class SurfaceSeries : GridSeries3D, IColormappable, INormalizable, IHasAlpha, IHasEdgeColor
{
    public IColorMap? ColorMap { get; set; }

    public double Alpha { get; set; } = 0.8;

    public bool ShowWireframe { get; set; } = true;

    public INormalizer? Normalizer { get; set; }

    public Color? EdgeColor { get; set; }

    public int RowStride { get; set; } = 1;

    public int ColStride { get; set; } = 1;

    /// <summary>Initializes a new surface series with the specified grid data.</summary>
    public SurfaceSeries(double[] x, double[] y, double[,] z) : base(x, y, z) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "surface",
        XData = X,
        YData = Y,
        ZGridData = ZToListList(),
        ShowWireframe = ShowWireframe ? null : (bool?)false,
        RowStride = RowStride != 1 ? RowStride : null,
        ColStride = ColStride != 1 ? ColStride : null,
        Alpha = Alpha != 0.8 ? Alpha : null,
        Label = Label
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
