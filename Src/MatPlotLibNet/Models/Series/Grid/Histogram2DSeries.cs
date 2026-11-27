// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 2D density histogram that bins X,Y scatter data into a grid and renders density as colors.</summary>
/// <remarks>Similar to matplotlib's <c>hist2d</c>. Takes raw scatter data and computes bin counts at render time.</remarks>
public sealed class Histogram2DSeries : ChartSeries, IColorBarDataProvider, IColorScaled, IColormappable, INormalizable, ISeriesKind<Histogram2DSeries>
{
    /// <inheritdoc />
    /// <remarks>matplotlib's <c>hist2d</c> asks neither cycle.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Mesh;

    /// <summary>The X coordinate of each observation, paired by index with <see cref="Y"/>.</summary>
    public double[] X { get; }

    /// <summary>The Y coordinate of each observation, paired by index with <see cref="X"/>.</summary>
    public double[] Y { get; }

    /// <summary>The number of bins along each axis a 2-D histogram divides its data into when it is told none.</summary>
    internal const int RestingBins = 20;

    /// <summary>The number of bins along the X axis: at least one.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value set is below one; the series keeps the bins it had.</exception>
    public int BinsX
    {
        get => _binsX;
        set => Change(ref _binsX, value.ThrowIfBelow(1, nameof(BinsX), "The bins of a 2-D histogram along the X axis"));
    }

    /// <summary>The number of bins along the Y axis: at least one.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value set is below one; the series keeps the bins it had.</exception>
    public int BinsY
    {
        get => _binsY;
        set => Change(ref _binsY, value.ThrowIfBelow(1, nameof(BinsY), "The bins of a 2-D histogram along the Y axis"));
    }

    private int _binsX, _binsY;

    /// <inheritdoc />
    public IColorMap? ColorMap { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public INormalizer? Normalizer { get; set => Change(ref field, value); }

    /// <summary>Initializes a new instance of <see cref="Histogram2DSeries"/> with the specified scatter data.</summary>
    /// <param name="x">The X-axis data values.</param>
    /// <param name="y">The Y-axis data values.</param>
    /// <param name="binsX">The number of bins along the X axis.</param>
    /// <param name="binsY">The number of bins along the Y axis.</param>
    /// <exception cref="ArgumentOutOfRangeException">A number of bins is below one.</exception>
    public Histogram2DSeries(double[] x, double[] y, int binsX = RestingBins, int binsY = RestingBins)
    {
        X = x;
        Y = y;
        BinsX = binsX;
        BinsY = binsY;
    }

    /// <inheritdoc />
    public MinMaxRange GetColorBarRange() => this.ColourRangeUnder(Theme.Default);

    /// <inheritdoc />
    /// <remarks>The counts of the bins (<see cref="ComputeBinCounts"/>); none when no point can be placed.</remarks>
    MinMaxRange IColorScaled.ColourRangeOver(ColorMapping mapping) =>
        BinExtent() is null ? mapping.RangeOf((Range1D?)null) : mapping.RangeOf(ComputeBinCounts());

    /// <inheritdoc />
    /// <remarks>The mesh its bins make (over the points that can be placed), its edges sticky as
    /// matplotlib's <c>pcolormesh</c> makes them (<c>Axes._update_pcolor_lims</c>), so a margin stops at the mesh.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (X.Length == 0 || Y.Length == 0 || BinExtent() is not var (xMin, xMax, yMin, yMax)) return default;
        return DataRangeContribution.Pinned(new(xMin, xMax), new(yMin, yMax));
    }

    static string ISeriesKind<Histogram2DSeries>.Kind => "histogram2d";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = X,
        YData = Y,
        Bins = BinsX,
        BinsY = BinsY,
        ColorScale = this.ColorScaleOnTheWire(),
    };

    /// <summary>The reader of a <see cref="Histogram2DSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static Histogram2DSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Histogram2D(dto.XData ?? [], dto.YData ?? [], dto.Bins ?? RestingBins);
        if (dto.BinsY is { } binsY) s.BinsY = binsY;
        s.RestoreColorScale(dto);
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The observations the bins are computed from; the binning is the renderer's reduction of them.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.XY(null, X, Y);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);

    /// <summary>The extent the bins span, in data units: the points that can be placed, as <c>np.histogram2d</c> with no range
    /// spans them, a side of one value spanning it and half a unit either side (<see cref="Range1D.OpenedWhenFlat"/>);
    /// <see langword="null"/> when no point can be placed. The one rule the counting and the drawing of the cells read.</summary>
    /// <returns>The extent, or <see langword="null"/>.</returns>
    internal Histogram2DExtent? BinExtent()
    {
        var pairs = X.DrawablePairs(Y);
        if (pairs.X.DrawableRange() is not { } xs || pairs.Y.DrawableRange() is not { } ys) return null;
        var (x, y) = (xs.OpenedWhenFlat(), ys.OpenedWhenFlat());
        return new Histogram2DExtent(x.Lo, x.Hi, y.Lo, y.Hi);
    }

    /// <summary>Computes the 2D histogram bin counts from the raw scatter data: row 0 holds the lowest Y bin, column 0 the lowest
    /// X bin, over <see cref="BinExtent"/>.</summary>
    internal int[,] ComputeBinCounts()
    {
        var counts = new int[BinsY, BinsX];
        if (X.Length == 0) return counts;

        // A point that cannot be placed (a missing coordinate, NaN, or an infinity) is not counted, and the bins span
        // the points that are counted: matplotlib's hist2d drops it as numpy does a value outside every bin.
        if (BinExtent() is not var (xMin, xMax, yMin, yMax)) return counts;
        var pairs = X.DrawablePairs(Y);

        var columns = new EqualWidthBins(xMin, xMax, BinsX);
        var rows = new EqualWidthBins(yMin, yMax, BinsY);
        int n = Math.Min(pairs.X.Length, pairs.Y.Length);
        for (int i = 0; i < n; i++)
        {
            if (!pairs.X[i].IsDrawable() || !pairs.Y[i].IsDrawable()) continue;
            counts[rows.IndexOf(pairs.Y[i]), columns.IndexOf(pairs.X[i])]++;
        }

        return counts;
    }

    /// <summary>The extent the bins of a 2-D histogram span, in data units (<see cref="Histogram2DSeries.BinExtent"/>).</summary>
    /// <param name="XMin">The low end of the first X bin.</param>
    /// <param name="XMax">The high end of the last X bin.</param>
    /// <param name="YMin">The low end of the first Y bin.</param>
    /// <param name="YMax">The high end of the last Y bin.</param>
    internal readonly record struct Histogram2DExtent(double XMin, double XMax, double YMin, double YMax);
}
