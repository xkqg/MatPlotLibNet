// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents an image series that renders a 2D data matrix as colored pixels using a colormap (imshow).</summary>
/// <remarks>Unlike <see cref="HeatmapSeries"/>, ImageSeries uses pixel-coordinate axes (0 to width, 0 to height)
/// and supports explicit VMin/VMax for color scaling.</remarks>
public sealed class ImageSeries : ChartSeries, IColorBarDataProvider, IColormappable, INormalizable
{
    /// <summary>Gets the two-dimensional data matrix.</summary>
    public double[,] Data { get; }

    /// <summary>Gets or sets the color map used to map data values to colors.</summary>
    public IColorMap? ColorMap { get; set; }

    /// <summary>Gets or sets the normalizer used to map data values to [0, 1] for colormap lookup. Defaults to linear.</summary>
    public INormalizer? Normalizer { get; set; }

    /// <summary>Gets or sets the interpolation method. Currently "nearest" (default); "bilinear" reserved for future use.</summary>
    public string? Interpolation { get; set; }

    /// <summary>Gets or sets the explicit minimum value for color scaling. When null, auto-detected from data.</summary>
    public double? VMin { get; set; }

    /// <summary>Gets or sets the explicit maximum value for color scaling. When null, auto-detected from data.</summary>
    public double? VMax { get; set; }

    /// <inheritdoc />
    public (double Min, double Max) GetColorBarRange()
    {
        double min = VMin ?? double.MaxValue;
        double max = VMax ?? double.MinValue;
        if (!VMin.HasValue || !VMax.HasValue)
        {
            for (int r = 0; r < Data.GetLength(0); r++)
                for (int c = 0; c < Data.GetLength(1); c++)
                {
                    if (!VMin.HasValue && Data[r, c] < min) min = Data[r, c];
                    if (!VMax.HasValue && Data[r, c] > max) max = Data[r, c];
                }
        }
        return min < max ? (min, max) : (0, 1);
    }

    /// <summary>Initializes a new instance of <see cref="ImageSeries"/> with the specified 2D data.</summary>
    /// <param name="data">The two-dimensional data matrix to render as a colored image.</param>
    public ImageSeries(double[,] data)
    {
        Data = data;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(0, Data.GetLength(1), 0, Data.GetLength(0));

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "image",
        HeatmapData = ChartSerializer.To2DList(Data),
        ColorMapName = ColorMap?.Name,
        VMin = VMin,
        VMax = VMax,
        Interpolation = Interpolation
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
