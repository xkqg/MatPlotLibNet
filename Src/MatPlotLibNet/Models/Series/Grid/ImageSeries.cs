// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents an image series that renders a 2D data matrix as colored pixels using a colormap (imshow).</summary>
/// <remarks>The axes are numbered in cells, the first row on top. Under the matplotlib themes the image stands as matplotlib's
/// <c>imshow</c> stands it: each cell is centred on its column and row number, so the axes show -0.5 to the columns less a half
/// across and the rows less a half down to -0.5, exactly, with the Y axis running down. Under the library's own themes it stands
/// on 0 to the columns across and 0 to the rows up, which a lone image fills exactly. Under every theme a line that reaches past
/// the image widens the view on its own side only, and the cells keep their place. Supports explicit VMin/VMax for color
/// scaling.</remarks>
public sealed class ImageSeries : ChartSeries, IColorBarDataProvider, IColorScaled, IColormappable, INormalizable, IHasAlpha, IThemedRange, IAsksOfAxes, ISeriesKind<ImageSeries>
{
    /// <summary>How far an edge of the image stands from the number of the cell beside it under the matplotlib themes: a cell is
    /// centred on its number, as <c>imshow</c>'s extent <c>(-0.5, cols - 0.5, rows - 0.5, -0.5)</c> centres it.</summary>
    internal const double HalfCell = 0.5;

    /// <inheritdoc />
    /// <remarks>matplotlib's <c>imshow</c> asks neither cycle. 0: <c>imshow</c> draws an <c>AxesImage</c>.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Image;

    /// <summary>Under a theme that stands an image as <c>imshow</c> does (<see cref="ImageLook.LikeImshow"/>): the image's extent, which no
    /// rounding moves (<c>AxesImage.set_extent</c>), the first row on top (<c>set_ylim(rows - 0.5, -0.5)</c>) and the look's aspect
    /// (<see cref="ImageLook.Aspect"/>: <c>imshow</c> hands <c>image.aspect</c> to <c>set_aspect</c>); nothing under the library's own.</summary>
    AxesAsk IAsksOfAxes.AskUnder(Theme theme) =>
        theme.Looks.Image.LikeImshow ? new(Tight: true, RowsDown: true, theme.Looks.Image.Aspect) : default;

    /// <summary>The pixel values, indexed [row, column]; each becomes one coloured pixel.</summary>
    public double[,] Data { get; }

    /// <inheritdoc />
    public IColorMap? ColorMap { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public INormalizer? Normalizer { get; set => Change(ref field, value); }

    /// <summary>The name of the resampling used when the image is scaled to the plot: <c>"nearest"</c>, <c>"bilinear"</c>, <c>"bicubic"</c>,
    /// <c>"hanning"</c>, or any engine registered with <see cref="MatPlotLibNet.Rendering.Interpolation.InterpolationRegistry"/>;
    /// <c>"auto"</c> and <c>"antialiased"</c> are matplotlib's rule, nearest where the image is shown at a whole multiple of its size or more
    /// than three times it and Hanning otherwise. <see langword="null"/> (the default) takes the theme's: nearest under the library's own
    /// themes, <c>"auto"</c> under the default matplotlib style and <c>"bilinear"</c> under the classic one. A name that is given is the one
    /// drawn, under every theme.</summary>
    public string? Interpolation { get; set => Change(ref field, value); }

    private const double RestingAlpha = 1.0;
    /// <inheritdoc />
    public double Alpha { get; set => Change(ref field, value.ThrowIfNotOpacity(nameof(Alpha))); } = RestingAlpha;

    private const BlendMode RestingBlendMode = BlendMode.Normal;
    /// <summary>How the image is combined with what lies behind it: <see cref="RestingBlendMode"/>, the default, is plain alpha
    /// compositing, and the other blend modes combine the colours of the image and its background.</summary>
    public BlendMode BlendMode { get; set => Change(ref field, value); } = RestingBlendMode;

    /// <summary>The data value at the bottom of the colour range; <see langword="null"/> (the default) uses the
    /// smallest drawable value of <see cref="Data"/>.</summary>
    public double? VMin { get; set => Change(ref field, value); }

    /// <summary>The data value at the top of the colour range; <see langword="null"/> (the default) uses the largest
    /// drawable value of <see cref="Data"/>.</summary>
    public double? VMax { get; set => Change(ref field, value); }

    /// <inheritdoc />
    /// <remarks>Over the drawable values only, the range the renderer colours the pixels over: a missing value (NaN)
    /// and an infinity are left out.</remarks>
    public MinMaxRange GetColorBarRange() => this.ColourRangeUnder(Theme.Default);

    /// <inheritdoc />
    /// <remarks>Its values between the ends given: the limits <see cref="VMin"/> and <see cref="VMax"/> an author set stand as set, the
    /// range the values give is over the values above 0 on a log scale.</remarks>
    MinMaxRange IColorScaled.ColourRangeOver(ColorMapping mapping) => mapping.RangeOf(Data, VMin, VMax);

    /// <summary>Initializes a new instance of <see cref="ImageSeries"/> with the specified 2D data.</summary>
    /// <param name="data">The two-dimensional data matrix to render as a colored image.</param>
    public ImageSeries(double[,] data)
    {
        Data = data;
    }

    /// <inheritdoc />
    /// <remarks>0 to the columns across and 0 to the rows up, every edge sticky: the cells fill that extent, so no margin and no
    /// rounding out stands past them (an axis padded past the image would name places where no cell stands). An image with no
    /// row or no column adds nothing.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => DataRangeContribution.OfCells(Data);

    /// <inheritdoc />
    /// <remarks>Under a theme that stands an image as <c>imshow</c> does (<see cref="ImageLook.LikeImshow"/>) the extent of the cells
    /// centred on their numbers, every edge sticky (<c>AxesImage.set_extent</c>: <c>self.sticky_edges.x[:] = [xmin, xmax]</c>), so no margin
    /// reaches past the image; an image with no row or no column adds nothing. Under any other theme <see cref="ComputeDataRange"/>.</remarks>
    DataRangeContribution IThemedRange.RangeUnder(IAxesContext context, Theme theme)
    {
        if (!theme.Looks.Image.LikeImshow) return ComputeDataRange(context);
        int rows = Data.GetLength(0), cols = Data.GetLength(1);
        if (rows == 0 || cols == 0) return default;
        double right = cols - HalfCell, bottom = rows - HalfCell;
        return DataRangeContribution.Pinned(new(-HalfCell, right), new(-HalfCell, bottom));
    }

    static string ISeriesKind<ImageSeries>.Kind => "image";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        HeatmapData = Data.To2DList(),
        ColorScale = this.ColorScaleOnTheWire(),
        VMin = VMin,
        VMax = VMax,
        Interpolation = Interpolation,
        Alpha = Alpha.OrNullAtRest(RestingAlpha),
        BlendMode = BlendMode.OrNullAtRest(RestingBlendMode),
    };

    /// <summary>The reader of a <see cref="ImageSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static ImageSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Image(dto.HeatmapData.From2DList());
        s.RestoreColorScale(dto);
        s.VMin = dto.VMin;
        s.VMax = dto.VMax;
        s.Interpolation = dto.Interpolation;
        if (dto.Alpha is { } alpha) s.Alpha = alpha;
        if (dto.BlendMode is { } blendMode) s.BlendMode = blendMode;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>Long form: one row per cell. A wide grid would make the column headers indices, which say
    /// nothing a reader can use; row and column as numbers are readable at any size.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.PerCell(Data.GetLength(0), Data.GetLength(1), [new("row"), new("column"), new(Label ?? "value")],
            (r, c) => [DataCell.FromNumber(r), DataCell.FromNumber(c), DataCell.FromNumber(Data[r, c])]);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
