// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

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
        X = x;
        Y = y;
    }

    /// <inheritdoc />
    public (double Min, double Max) GetColorBarRange()
    {
        if (X.Length == 0) return (0, 1);
        double xMin = X.Min(), xMax = X.Max();
        double yMin = Y.Min(), yMax = Y.Max();
        if (xMin == xMax) xMax = xMin + 1;
        if (yMin == yMax) yMax = yMin + 1;
        var bins = HexbinSeries.ComputeBins(X, Y, xMin, xMax, yMin, yMax, GridSize);
        if (bins.Count == 0) return (0, 1);
        int maxCount = bins.Values.Max();
        return (MinCount, maxCount);
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (X.Length == 0) return new(null, null, null, null);
        return new(X.Min(), X.Max(), Y.Min(), Y.Max());
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "hexbin",
        XData = X,
        YData = Y,
        GridSize = GridSize == 20 ? null : GridSize,
        MinCount = MinCount == 1 ? null : MinCount,
        ColorMapName = ColorMap?.Name
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);

    /// <summary>Computes hex bin counts using the given data range and grid size.</summary>
    internal static Dictionary<(int q, int r), int> ComputeBins(
        double[] x, double[] y, double xMin, double xMax, double yMin, double yMax, int gridSize)
        => HexGrid.ComputeHexBins(x, y, xMin, xMax, yMin, yMax, gridSize);
}
