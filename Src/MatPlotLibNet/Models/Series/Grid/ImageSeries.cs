// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents an image series that renders a 2D data matrix as colored pixels using a colormap (imshow).</summary>
/// <remarks>Unlike <see cref="HeatmapSeries"/>, ImageSeries uses pixel-coordinate axes (0 to width, 0 to height)
/// and supports explicit VMin/VMax for color scaling.</remarks>
public sealed class ImageSeries : ChartSeries, IColorBarDataProvider, IColormappable, INormalizable, IHasAlpha
{
    /// <summary>The pixel values, indexed [row, column]; each becomes one coloured pixel.</summary>
    public double[,] Data { get; }

    /// <inheritdoc />
    public IColorMap? ColorMap { get; set; }

    /// <inheritdoc />
    public INormalizer? Normalizer { get; set; }

    /// <summary>The name of the resampling used when the image is scaled to the plot: <c>"nearest"</c> (also used when
    /// this is <see langword="null"/>), <c>"bilinear"</c>, <c>"bicubic"</c>, or any engine registered with
    /// <see cref="MatPlotLibNet.Rendering.Interpolation.InterpolationRegistry"/>.</summary>
    public string? Interpolation { get; set; }

    /// <inheritdoc />
    public double Alpha { get; set; } = 1.0;

    /// <summary>How the image is combined with what lies behind it: <c>Normal</c> (the default) is plain alpha
    /// compositing, and the other blend modes combine the colours of the image and its background.</summary>
    public BlendMode BlendMode { get; set; } = BlendMode.Normal;

    /// <summary>The data value at the bottom of the colour range; <see langword="null"/> (the default) uses the
    /// smallest drawable value of <see cref="Data"/>.</summary>
    public double? VMin { get; set; }

    /// <summary>The data value at the top of the colour range; <see langword="null"/> (the default) uses the largest
    /// drawable value of <see cref="Data"/>.</summary>
    public double? VMax { get; set; }

    /// <inheritdoc />
    /// <remarks>Over the drawable values only, the range the renderer colours the pixels over: a missing value (NaN)
    /// and an infinity are left out.</remarks>
    public MinMaxRange GetColorBarRange()
    {
        var drawable = Data.DrawableRange() ?? new Range1D(0, 1);
        double min = VMin ?? drawable.Lo;
        double max = VMax ?? drawable.Hi;
        return min < max ? new(min, max) : new(0, 1);
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
        HeatmapData = Data.To2DList(),
        ColorMapName = ColorMap.NameForJson(),
        ColorMap = ColorMap.DescriptionForJson(),
        VMin = VMin,
        VMax = VMax,
        Interpolation = Interpolation,
        Alpha = Alpha.OrNullAtRest(1.0),
        BlendMode = BlendMode.OrNullAtRest(BlendMode.Normal),
        Normalizer = this.DescribeNormalizer(),
    };

    /// <summary>Reconstructs an <see cref="ImageSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static ImageSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Image(dto.HeatmapData.From2DList());
        s.RestoreColorMap(dto);
        s.VMin = dto.VMin;
        s.VMax = dto.VMax;
        s.Interpolation = dto.Interpolation;
        if (dto.Alpha.HasValue)
        {
            s.Alpha = dto.Alpha.Value;
        }
        if (dto.BlendMode.HasValue)
        {
            s.BlendMode = dto.BlendMode.Value;
        }
        s.RestoreNormalizer(dto);
        return s;
    }


    /// <inheritdoc />
    /// <remarks>Long form: one row per cell. A wide grid would make the column headers indices, which say
    /// nothing a reader can use; row and column as numbers are readable at any size.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        int rows = Data.GetLength(0), cols = Data.GetLength(1);
        var out_ = new List<IReadOnlyList<DataCell>>(rows * cols);
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                out_.Add([DataCell.FromNumber(r), DataCell.FromNumber(c), DataCell.FromNumber(Data[r, c])]);
            }
        }

        return new ChartDataTable(null, [new("row"), new("column"), new(Label ?? "value")], out_);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
