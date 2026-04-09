// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a heatmap series that renders a 2D data matrix as colored cells.</summary>
public sealed class HeatmapSeries : ChartSeries, IColorBarDataProvider, IColormappable, INormalizable
{
    /// <summary>Gets the two-dimensional data matrix.</summary>
    public double[,] Data { get; }

    /// <summary>Gets or sets the color map used to map data values to colors.</summary>
    public IColorMap? ColorMap { get; set; }

    /// <summary>Gets or sets the normalizer used to map data values to [0, 1] for colormap lookup. Defaults to linear.</summary>
    public INormalizer? Normalizer { get; set; }

    /// <inheritdoc />
    public (double Min, double Max) GetColorBarRange()
    {
        double min = double.MaxValue, max = double.MinValue;
        for (int r = 0; r < Data.GetLength(0); r++)
            for (int c = 0; c < Data.GetLength(1); c++)
            {
                if (Data[r, c] < min) min = Data[r, c];
                if (Data[r, c] > max) max = Data[r, c];
            }
        return min < max ? (min, max) : (0, 1);
    }


    /// <summary>Initializes a new instance of <see cref="HeatmapSeries"/> with the specified 2D data.</summary>
    /// <param name="data">The two-dimensional data matrix to render.</param>
    public HeatmapSeries(double[,] data)
    {
        Data = data;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new() { Type = "heatmap", HeatmapData = ChartSerializer.To2DList(Data), ColorMapName = ColorMap?.Name };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
