// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 2D density histogram that bins X,Y scatter data into a grid and renders density as colors.</summary>
/// <remarks>Similar to matplotlib's <c>hist2d</c>. Takes raw scatter data and computes bin counts at render time.</remarks>
public sealed class Histogram2DSeries : ChartSeries, IColorBarDataProvider, IColormappable, INormalizable
{
    public double[] X { get; }

    public double[] Y { get; }

    /// <summary>The number of bins along the X axis: at least one.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value set is below one; the series keeps the bins it had.</exception>
    public int BinsX
    {
        get => _binsX;
        set => _binsX = AtLeastOneBin(value);
    }

    /// <summary>The number of bins along the Y axis: at least one.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value set is below one; the series keeps the bins it had.</exception>
    public int BinsY
    {
        get => _binsY;
        set => _binsY = AtLeastOneBin(value);
    }

    private int _binsX, _binsY;

    private static int AtLeastOneBin(int bins, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(bins))] string? paramName = null) =>
        bins >= 1 ? bins : throw new ArgumentOutOfRangeException(paramName, bins, "A 2-D histogram has at least one bin along each axis.");

    public IColorMap? ColorMap { get; set; }

    public INormalizer? Normalizer { get; set; }

    /// <summary>Initializes a new instance of <see cref="Histogram2DSeries"/> with the specified scatter data.</summary>
    /// <param name="x">The X-axis data values.</param>
    /// <param name="y">The Y-axis data values.</param>
    /// <param name="binsX">The number of bins along the X axis.</param>
    /// <param name="binsY">The number of bins along the Y axis.</param>
    /// <exception cref="ArgumentOutOfRangeException">A number of bins is below one.</exception>
    public Histogram2DSeries(double[] x, double[] y, int binsX = 20, int binsY = 20)
    {
        X = x;
        Y = y;
        BinsX = binsX;
        BinsY = binsY;
    }

    /// <inheritdoc />
    public MinMaxRange GetColorBarRange()
    {
        if (X.Length == 0 || Y.Length == 0) return new(0, 1);
        var counts = ComputeBinCounts();
        double min = double.MaxValue, max = double.MinValue;
        for (int r = 0; r < BinsY; r++)
        for (int c = 0; c < BinsX; c++)
        {
            if (counts[r, c] < min) min = counts[r, c];
            if (counts[r, c] > max) max = counts[r, c];
        }
        return min < max ? new(min, max) : new(0, 1);
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (X.Length == 0 || Y.Length == 0) return new(null, null, null, null);
        // Over the drawable points only: a missing coordinate (NaN) and an infinity are left out.
        var x = X.DrawableRange();
        var y = Y.DrawableRange();
        return new(x?.Lo, x?.Hi, y?.Lo, y?.Hi);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "histogram2d",
        XData = X,
        YData = Y,
        Bins = BinsX,
        BinsY = BinsY,
        ColorMapName = ColorMap.NameForJson(),
        ColorMap = ColorMap.DescriptionForJson(),
        Normalizer = this.DescribeNormalizer(),
    };

    /// <summary>Reconstructs a <see cref="Histogram2DSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static Histogram2DSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Histogram2D(dto.XData ?? [], dto.YData ?? [], dto.Bins ?? 20);
        if (dto.BinsY.HasValue)
        {
            s.BinsY = dto.BinsY.Value;
        }
        s.RestoreColorMap(dto);
        s.RestoreNormalizer(dto);
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The observations the bins are computed from; the binning is the renderer's reduction of them.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.FromNumberColumns([new("x", Axis: DataAxis.X), new("y", Axis: DataAxis.Y)], [X, Y]);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);

    /// <summary>Computes the 2D histogram bin counts from the raw scatter data.</summary>
    internal int[,] ComputeBinCounts()
    {
        var counts = new int[BinsY, BinsX];
        if (X.Length == 0) return counts;

        // A point that cannot be placed (a missing coordinate, NaN, or an infinity) is not counted, and the bins span
        // the points that are counted: matplotlib's hist2d drops it as numpy does a value outside every bin.
        var pairs = X.DrawablePairs(Y);
        if (pairs.X.DrawableRange() is not { } xs || pairs.Y.DrawableRange() is not { } ys) return counts;
        double xMin = xs.Lo, xMax = xs.Hi;
        double yMin = ys.Lo, yMax = ys.Hi;

        // Handle degenerate case where all values are the same
        if (xMax == xMin) xMax = xMin + 1;
        if (yMax == yMin) yMax = yMin + 1;

        int n = Math.Min(pairs.X.Length, pairs.Y.Length);
        for (int i = 0; i < n; i++)
        {
            if (!pairs.X[i].IsDrawable() || !pairs.Y[i].IsDrawable()) continue;
            int bx = (int)((pairs.X[i] - xMin) / (xMax - xMin) * BinsX);
            int by = (int)((pairs.Y[i] - yMin) / (yMax - yMin) * BinsY);
            // Clamp to valid range (values exactly at max map to BinsX/BinsY)
            if (bx >= BinsX) bx = BinsX - 1;
            if (by >= BinsY) by = BinsY - 1;
            if (bx < 0) bx = 0;
            if (by < 0) by = 0;
            counts[by, bx]++;
        }

        return counts;
    }
}
