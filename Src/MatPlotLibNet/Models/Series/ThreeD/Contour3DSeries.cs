// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents 3D contour lines projected into three-dimensional space from grid data.</summary>
public sealed class Contour3DSeries : GridSeries3D, IColormappable, IHasColor, ISeriesKind<Contour3DSeries>
{
    /// <inheritdoc />
    /// <remarks><c>Axes3D.contour</c> asks neither cycle.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Mesh;

    private const int RestingLevels = GridContourSeries.RestingLevels;
    /// <summary>Number of contour levels to compute. Default <see cref="RestingLevels"/>.</summary>
    public int Levels { get; set => Change(ref field, value); } = RestingLevels;

    /// <summary>Colormap applied to the contour levels. Each level gets a distinct color from the map.</summary>
    public IColorMap? ColorMap { get; set => Change(ref field, value); }

    private const double RestingLineWidth = 1.0;
    /// <summary>Width of the contour lines in pixels. Default <see cref="RestingLineWidth"/>.</summary>
    public double LineWidth { get; set => Change(ref field, value); } = RestingLineWidth;

    /// <summary>Uniform color for all contour lines. Overridden by <see cref="ColorMap"/> when set.</summary>
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>Initializes a new 3D contour series with the specified grid data.</summary>
    public Contour3DSeries(double[] x, double[] y, double[,] z) : base(x, y, z) { }

    static string ISeriesKind<Contour3DSeries>.Kind => "contour3d";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = X,
        YData = Y,
        ZGridData = Z.To2DList(),
        Color = Color,
        Levels = Levels.OrNullAtRest(RestingLevels),
        LineWidth = LineWidth.OrNullAtRest(RestingLineWidth),
        ColorScale = this.ColorScaleOnTheWire(),
        Label = Label
    };

    /// <summary>The reader of a <see cref="Contour3DSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static Contour3DSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Contour3D(dto.XData ?? [], dto.YData ?? [], dto.ZGridData.From2DList());
        if (dto.Color is { } color) s.Color = color;
        if (dto.Levels is { } levels) s.Levels = levels;
        if (dto.LineWidth is { } lineWidth) s.LineWidth = lineWidth;
        s.RestoreColorScale(dto);
        return s;
    }


    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
