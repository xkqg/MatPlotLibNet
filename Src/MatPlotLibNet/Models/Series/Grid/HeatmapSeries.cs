// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a heatmap series that renders a 2D data matrix as colored cells.</summary>
public sealed class HeatmapSeries : ChartSeries, IColorBarDataProvider, IColormappable, INormalizable
{
    public double[,] Data { get; }

    public IColorMap? ColorMap { get; set; }

    public INormalizer? Normalizer { get; set; }

    /// <inheritdoc />
    public MinMaxRange GetColorBarRange()
    {
        double min = double.MaxValue, max = double.MinValue;
        for (int r = 0; r < Data.GetLength(0); r++)
            for (int c = 0; c < Data.GetLength(1); c++)
            {
                if (Data[r, c] < min) min = Data[r, c];
                if (Data[r, c] > max) max = Data[r, c];
            }
        return min < max ? new(min, max) : new(0, 1);
    }


    /// <summary>Initializes a new instance of <see cref="HeatmapSeries"/> with the specified 2D data.</summary>
    /// <param name="data">The two-dimensional data matrix to render.</param>
    public HeatmapSeries(double[,] data)
    {
        Data = data;
    }

    /// <inheritdoc />
    /// <remarks>Heatmaps fill their plot rectangle exactly (cells of <c>cols × rows</c>).
    /// Report the grid extent with sticky edges on all four sides so the axes show
    /// meaningful row/column indices instead of the default <c>[0, 1]</c> fallback, and
    /// so the 5 % axis margin never introduces whitespace between the cells and the spines.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        int rows = Data.GetLength(0);
        int cols = Data.GetLength(1);
        if (rows == 0 || cols == 0) return new(null, null, null, null);
        return new(0, cols, 0, rows,
            StickyXMin: 0, StickyXMax: cols, StickyYMin: 0, StickyYMax: rows);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new() { Type = "heatmap", HeatmapData = ChartSerializer.To2DList(Data), ColorMapName = ColorMap?.Name };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
