// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a hexagonal binning series that aggregates scatter data into a hex grid and maps counts to colors.</summary>
/// <remarks>Analogous to matplotlib's <c>hexbin</c>. Uses flat-top hexagons in axial (q, r) coordinates.</remarks>
public sealed class HexbinSeries : ChartSeries, IColormappable, INormalizable, IColorBarDataProvider
{
    public double[] X { get; }

    public double[] Y { get; }

    public int GridSize { get; set; } = 20;

    public int MinCount { get; set; } = 1;

    public IColorMap? ColorMap { get; set; }

    public INormalizer? Normalizer { get; set; }

    /// <summary>Initializes a new instance of <see cref="HexbinSeries"/> with the specified scatter data.</summary>
    public HexbinSeries(double[] x, double[] y)
    {
        x.ThrowIfMisaligned(y);
        X = x;
        Y = y;
    }

    /// <inheritdoc />
    public MinMaxRange GetColorBarRange()
    {
        // The bins span the points that can be placed: a missing coordinate (NaN) and an infinity are not binned.
        if (X.DrawableRange() is not { } xs || Y.DrawableRange() is not { } ys) return new(0, 1);
        double xMin = xs.Lo, xMax = xs.Hi;
        double yMin = ys.Lo, yMax = ys.Hi;
        if (xMin == xMax) xMax = xMin + 1;
        if (yMin == yMax) yMax = yMin + 1;
        var bins = HexbinSeries.ComputeBins(X, Y, xMin, xMax, yMin, yMax, GridSize);
        if (bins.Count == 0) return new(0, 1);
        int maxCount = bins.Values.Max();
        return new(MinCount, maxCount);
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (X.Length == 0) return new(null, null, null, null);
        // Over the drawable points only: a missing coordinate (NaN) and an infinity are left out.
        var x = X.DrawableRange();
        var y = Y.DrawableRange();
        return new(x?.Lo, x?.Hi, y?.Lo, y?.Hi);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "hexbin",
        XData = X,
        YData = Y,
        GridSize = GridSize == 20 ? null : GridSize,
        MinCount = MinCount == 1 ? null : MinCount,
        ColorMapName = ColorMap.NameForJson(),
        ColorMap = ColorMap.DescriptionForJson(),
        Normalizer = this.DescribeNormalizer(),
    };

    /// <summary>Reconstructs a <see cref="HexbinSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static HexbinSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Hexbin(dto.XData ?? [], dto.YData ?? []);
        if (dto.GridSize.HasValue)
        {
            s.GridSize = dto.GridSize.Value;
        }
        if (dto.MinCount.HasValue)
        {
            s.MinCount = dto.MinCount.Value;
        }
        s.RestoreColorMap(dto);
        s.RestoreNormalizer(dto);
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The observations the hexagons are binned from.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.FromNumberColumns([new("x", Axis: DataAxis.X), new("y", Axis: DataAxis.Y)], [X, Y]);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);

    /// <summary>Computes hex bin counts using the given data range and grid size.</summary>
    internal static Dictionary<AxialHex, int> ComputeBins(
        double[] x, double[] y, double xMin, double xMax, double yMin, double yMax, int gridSize)
        => HexGrid.ComputeHexBins(x, y, xMin, xMax, yMin, yMax, gridSize);
}
