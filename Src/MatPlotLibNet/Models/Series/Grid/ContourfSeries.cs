// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a filled contour plot that fills the regions between iso-levels using a painter's algorithm.</summary>
public sealed class ContourfSeries : GridContourSeries, INormalizable, IHasAlpha, ISeriesKind<ContourfSeries>
{
    /// <inheritdoc />
    /// <remarks>matplotlib's <c>contourf</c> asks neither cycle.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Mesh;

    private const double RestingAlpha = 1.0;
    /// <inheritdoc />
    public double Alpha { get; set => Change(ref field, value.ThrowIfNotOpacity(nameof(Alpha))); } = RestingAlpha;

    private const bool RestingShowLines = true;
    /// <summary>When <see langword="true"/> iso-lines are drawn on top of the fill, along the edges of the bands. A filled contour
    /// that was given no value reads <see cref="RestingShowLines"/> and draws them under the library's own themes, and none under the
    /// matplotlib themes, as matplotlib's <c>contourf</c> draws its bands with no edge line; a value that was given, the default
    /// included, is drawn as it is under every theme.</summary>
    public bool ShowLines { get => GivenShowLines ?? RestingShowLines; set => GivenShowLines = value; }

    /// <summary>Whether the lines were asked for or refused, <see langword="null"/> when neither was and the theme decides.</summary>
    internal bool? GivenShowLines { get; private set => Change(ref field, value); }

    private const double RestingLineWidth = 0.5;
    /// <summary>Width of the iso-lines, in pixels. Default <see cref="RestingLineWidth"/>.</summary>
    public double LineWidth { get; set => Change(ref field, value); } = RestingLineWidth;

    /// <inheritdoc />
    public INormalizer? Normalizer { get; set => Change(ref field, value); }

    /// <summary>The hatch pattern of each band, from the lowest band up; a band past the end of the array, or every
    /// band when this is <see langword="null"/>, is left plain.</summary>
    public HatchPattern[]? Hatches { get; set => Change(ref field, value); }

    /// <summary>Initializes a new instance of <see cref="ContourfSeries"/> with grid data.</summary>
    /// <param name="xData">X-axis grid coordinates.</param>
    /// <param name="yData">Y-axis grid coordinates.</param>
    /// <param name="zData">2D Z values indexed as [row, col].</param>
    public ContourfSeries(double[] xData, double[] yData, double[,] zData) : base(xData, yData, zData)
    {
    }

    /// <inheritdoc />
    /// <remarks>The scale of the series' normaliser: decades under a <see cref="LogNormalizer"/>.</remarks>
    private protected override ScaleRule? LevelScale => this.NormalizerOrLinear().Rule.LevelScale;

    /// <inheritdoc />
    /// <remarks>The normaliser the series draws its bands through under <paramref name="theme"/>.</remarks>
    private protected override INormalizer BarNormalizerUnder(Theme theme) => this.ColorMappingUnder(theme).Normalizer;

    static string ISeriesKind<ContourfSeries>.Kind => "contourf";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => GridOnTheWire() with
    {
        Type = this.WireKind(),
        Alpha = Alpha.OrNullAtRest(RestingAlpha),
        ShowLines = GivenShowLines,
        LineWidth = LineWidth.OrNullAtRest(RestingLineWidth),
        ColorScale = this.ColorScaleOnTheWire(),
        Hatches = Hatches is null ? null : [.. Hatches],
    };

    /// <summary>The reader of a <see cref="ContourfSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static ContourfSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Contourf(dto.XData ?? [], dto.YData ?? [], dto.HeatmapData.From2DList());
        s.RestoreGrid(dto);
        if (dto.Alpha is { } alpha) s.Alpha = alpha;
        if (dto.ShowLines is { } showLines) s.ShowLines = showLines;
        if (dto.LineWidth is { } lineWidth) s.LineWidth = lineWidth;
        s.RestoreColorScale(dto);
        s.Hatches = dto.Hatches?.ToArray();
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
