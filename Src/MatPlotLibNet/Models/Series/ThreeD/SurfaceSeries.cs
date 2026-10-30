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
    /// <inheritdoc />
    public IColorMap? ColorMap { get; set; }

    /// <inheritdoc />
    public double Alpha { get; set; } = 0.8;

    /// <summary>Whether a thin outline is drawn around every surface cell. True by default; false draws the cells as
    /// plain filled patches.</summary>
    public bool ShowWireframe { get; set; } = true;

    /// <inheritdoc />
    public INormalizer? Normalizer { get; set; }

    /// <inheritdoc />
    public Color? EdgeColor { get; set; }

    /// <summary>How many grid rows are stepped over between one drawn cell and the next. Default 1 draws every row; a
    /// larger value draws a coarser, faster surface. A value below 1 counts as 1.</summary>
    public int RowStride { get; set; } = 1;

    /// <summary>How many grid columns are stepped over between one drawn cell and the next. Default 1 draws every column;
    /// a larger value draws a coarser, faster surface. A value below 1 counts as 1.</summary>
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
        ColorMapName = ColorMap.NameForJson(),
        ColorMap = ColorMap.DescriptionForJson(),
        EdgeColor = EdgeColor,
        Label = Label,
        Normalizer = this.DescribeNormalizer(),
    };

    /// <summary>Reconstructs a <see cref="SurfaceSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static SurfaceSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var z = dto.ZGridData.From2DList();
        var s = axes.Surface(dto.XData ?? [0.0, 1.0], dto.YData ?? [0.0, 1.0],
            z.GetLength(0) > 0 ? z : new double[,] { { 0, 0 }, { 0, 0 } });
        if (dto.ShowWireframe.HasValue)
        {
            s.ShowWireframe = dto.ShowWireframe.Value;
        }
        if (dto.RowStride.HasValue)
        {
            s.RowStride = dto.RowStride.Value;
        }
        if (dto.ColStride.HasValue)
        {
            s.ColStride = dto.ColStride.Value;
        }
        if (dto.Alpha.HasValue)
        {
            s.Alpha = dto.Alpha.Value;
        }
        s.RestoreColorMap(dto);
        s.EdgeColor = dto.EdgeColor;
        s.RestoreNormalizer(dto);
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
