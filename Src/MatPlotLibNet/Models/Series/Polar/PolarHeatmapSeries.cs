// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a heatmap on a polar grid where each cell is a wedge/sector
/// defined by a theta bin and a radial bin.</summary>
public sealed class PolarHeatmapSeries : ChartSeries, IColormappable, INormalizable, IColorBarDataProvider
{
    /// <summary>2D data array indexed as [thetaBin, rBin].</summary>
    public double[,] Data { get; }

    /// <summary>Number of angular bins (cells around the circle).</summary>
    public int ThetaBins { get; }

    /// <summary>Number of radial bins (cells along the radius).</summary>
    public int RBins { get; }

    /// <summary>Maximum data-space radius. Defaults to 1.0.</summary>
    public double RMax { get; set; } = 1.0;

    /// <inheritdoc />
    public IColorMap? ColorMap { get; set; }

    /// <inheritdoc />
    public INormalizer? Normalizer { get; set; }

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
    public MinMaxRange GetColorBarRange() => Data.ScanColorBarRange();

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "polarheatmap",
        HeatmapData = ChartSerializer.To2DList(Data),
        ThetaBins = ThetaBins,
        RBins = RBins,
        ColorMapName = ColorMap?.Name
    };

    /// <summary>Reconstructs a <see cref="PolarHeatmapSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static PolarHeatmapSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.PolarHeatmap(ChartSerializer.From2DList(dto.HeatmapData),
            dto.ThetaBins ?? 8, dto.RBins ?? 4);
        if (dto.ColorMapName is not null)
        {
            s.ColorMap = Styling.ColorMaps.ColorMapRegistry.Get(dto.ColorMapName);
        }
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
