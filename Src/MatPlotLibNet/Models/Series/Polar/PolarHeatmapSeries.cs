// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a heatmap on a polar grid where each cell is a wedge/sector
/// defined by a theta bin and a radial bin.</summary>
public sealed class PolarHeatmapSeries : ChartSeries, IColormappable, INormalizable, IColorBarDataProvider, IColorScaled, ISeriesKind<PolarHeatmapSeries>
{
    /// <inheritdoc />
    internal override CoordinateSystem? DrawnIn => CoordinateSystem.Polar;

    /// <inheritdoc />
    /// <remarks>matplotlib's <c>pcolormesh</c> on a polar axes asks neither cycle.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Mesh;

    /// <summary>2D data array indexed as [thetaBin, rBin].</summary>
    public double[,] Data { get; }

    /// <summary>Number of angular bins (cells around the circle).</summary>
    public int ThetaBins { get; }

    /// <summary>Number of radial bins (cells along the radius).</summary>
    public int RBins { get; }

    private const double RestingRMax = 1.0;
    /// <summary>Maximum data-space radius. Defaults to <see cref="RestingRMax"/>.</summary>
    public double RMax { get; set => Change(ref field, value); } = RestingRMax;

    /// <inheritdoc />
    public IColorMap? ColorMap { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public INormalizer? Normalizer { get; set => Change(ref field, value); }

    /// <summary>The angular divisions a heatmap document that names none is read with.</summary>
    private const int UnnamedThetaBins = 8;

    /// <summary>The radial divisions a heatmap document that names none is read with.</summary>
    private const int UnnamedRBins = 4;

    /// <summary>Initializes a new polar heatmap series.</summary>
    /// <param name="data">2D data array [thetaBins, rBins].</param>
    /// <param name="thetaBins">Number of angular divisions.</param>
    /// <param name="rBins">Number of radial divisions.</param>
    public PolarHeatmapSeries(double[,] data, int thetaBins, int rBins)
    {
        Data = data;
        ThetaBins = thetaBins;
        RBins = rBins;
    }

    /// <inheritdoc />
    public MinMaxRange GetColorBarRange() => this.ColourRangeUnder(Theme.Default);

    /// <inheritdoc />
    MinMaxRange IColorScaled.ColourRangeOver(ColorMapping mapping) => mapping.RangeOf(Data);

    /// <inheritdoc />
    /// <remarks>The heatmap's cells run from the centre to <see cref="RMax"/>, so the scale reaches at least that far, exactly.</remarks>
    internal override RadialExtent? RadialExtent => RMax.IsPositiveFinite() ? new RadialExtent(0, RMax, Sticky: true) : null;

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => default;

    static string ISeriesKind<PolarHeatmapSeries>.Kind => "polarheatmap";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        HeatmapData = Data.To2DList(),
        ThetaBins = ThetaBins,
        RBins = RBins,
        ColorScale = this.ColorScaleOnTheWire(),
        RMax = RMax.OrNullAtRest(RestingRMax),
    };

    /// <summary>The reader of a <see cref="PolarHeatmapSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static PolarHeatmapSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.PolarHeatmap(dto.HeatmapData.From2DList(),
            dto.ThetaBins ?? UnnamedThetaBins, dto.RBins ?? UnnamedRBins);
        s.RestoreColorScale(dto);
        if (dto.RMax is { } rMax) s.RMax = rMax;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>Long form over the wedge grid: which ring, which sector, and the value in that cell.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.PerCell(Data.GetLength(0), Data.GetLength(1), [new("r bin"), new("theta bin"), new(Label ?? "value")],
            (r, c) => [DataCell.FromNumber(r), DataCell.FromNumber(c), DataCell.FromNumber(Data[r, c])]);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
