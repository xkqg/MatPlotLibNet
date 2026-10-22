// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a histogram series that bins continuous data into discrete intervals.</summary>
public sealed class HistogramSeries : ChartSeries, IHasColor, IHasAlpha, IHasEdgeColor
{
    public double[] Data { get; }

    public int Bins { get; set; } = 10;

    public Color? Color { get; set; }

    public Color? EdgeColor { get; set; }

    public double Alpha { get; set; } = 1.0;

    public bool Density { get; set; } = false;

    public bool Cumulative { get; set; } = false;

    public HistType HistType { get; set; } = HistType.Bar;

    public double[]? Weights { get; set; }

    public double RWidth { get; set; } = 1.0;

    public HatchPattern Hatch { get; set; } = HatchPattern.None;

    public Color? HatchColor { get; set; }

    /// <summary>Initializes a new instance of <see cref="HistogramSeries"/> with the specified data.</summary>
    /// <param name="data">The raw data values to be binned.</param>
    public HistogramSeries(double[] data)
    {
        Data = data;
    }

    /// <inheritdoc />
    /// <remarks>Along Y the heights the bars are drawn at (<see cref="Heights"/>: counts, weighted, as a density, or
    /// cumulative) and the zero they stand on, over the values the Y axis can place: on a log axis, which has no place for
    /// zero, from the lowest bin.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        var scale = context.YScale;
        var reached = Data.Length > 0 ? Heights(ComputeBins()).DrawableRange(scale) : null;
        var y = (reached ?? Range1D.Empty).Including(0, scale).OrNone();
        // Over the drawable observations, the ones the bins count: a missing one (NaN) and an infinity are left out.
        var x = Data.Length > 0 ? Data.DrawableRange() : new Range1D(0, 1);
        double? xMin = x?.Lo, xMax = x?.Hi;
        return new(xMin, xMax, y?.Lo, y?.Hi,
            StickyXMin: xMin, StickyXMax: xMax, StickyYMin: 0);
    }

    /// <summary>The height each bin's bar is drawn at: its count, or the sum of its observations' <see cref="Weights"/>
    /// when there is one weight per observation; divided by the area of all bars when <see cref="Density"/> is set; summed
    /// from the first bin on when <see cref="Cumulative"/> is set. The renderer draws these and the range takes them in.</summary>
    /// <param name="bins">The bins, from <see cref="ComputeBins"/>.</param>
    /// <returns>One height per bin.</returns>
    internal double[] Heights(HistogramBins bins)
    {
        int n = bins.Counts.Length;
        var heights = new double[n];

        if (Weights is not null && Weights.Length == Data.Length)
        {
            // Weighted binning: accumulate weights per bin.
            for (int j = 0; j < Data.Length; j++)
            {
                int idx = Math.Min((int)((Data[j] - bins.Min) / bins.BinWidth), n - 1);
                heights[idx] += Weights[j];
            }
        }
        else
        {
            for (int i = 0; i < n; i++)
                heights[i] = bins.Counts[i];
        }

        if (Density)
        {
            double total = heights.Sum() * bins.BinWidth;
            if (total > 0)
                for (int i = 0; i < n; i++)
                    heights[i] /= total;
        }

        if (Cumulative)
        {
            for (int i = 1; i < n; i++)
                heights[i] += heights[i - 1];
        }

        return heights;
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "histogram",
        Data = Data, Bins = Bins, Color = Color,
        EdgeColor = EdgeColor,
        Alpha = Alpha.OrNullAtRest(1.0),
        Density = Density ? true : null,
        Cumulative = Cumulative ? true : null,
        HistType = HistType.OrNullAtRest(HistType.Bar),
        Weights = Weights,
        RWidth = RWidth.OrNullAtRest(1.0),
        Hatch = Hatch.OrNullAtRest(HatchPattern.None),
        HatchColor = HatchColor,
    };

    /// <summary>Reconstructs a <see cref="HistogramSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static HistogramSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Hist(dto.Data ?? [], dto.Bins ?? 10);
        s.Color = dto.Color;
        s.EdgeColor = dto.EdgeColor;
        if (dto.Alpha.HasValue)
        {
            s.Alpha = dto.Alpha.Value;
        }
        s.Density = dto.Density ?? false;
        s.Cumulative = dto.Cumulative ?? false;
        if (dto.HistType.HasValue)
        {
            s.HistType = dto.HistType.Value;
        }
        s.Weights = dto.Weights;
        if (dto.RWidth.HasValue)
        {
            s.RWidth = dto.RWidth.Value;
        }
        if (dto.Hatch.HasValue)
        {
            s.Hatch = dto.Hatch.Value;
        }
        s.HatchColor = dto.HatchColor;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>What a histogram draws is its BINS, so that is what the table says: where each bar starts and
    /// how many samples fell in it — the same reduction the renderer makes, through the same call.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var bins = ComputeBins();
        var rows = new IReadOnlyList<DataCell>[bins.Counts.Length];
        for (int i = 0; i < bins.Counts.Length; i++)
        {
            rows[i] = [DataCell.FromNumber(bins.Min + (i * bins.BinWidth)), DataCell.FromNumber(bins.Counts[i])];
        }

        return new ChartDataTable(null, [new("bin start", Axis: DataAxis.X), new(Label ?? "count")], rows);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);

    /// <summary>Computes histogram bin counts. Used by both range computation and rendering.</summary>
    /// <remarks>Delegates to the shared <see cref="MatPlotLibNet.Numerics.HistogramBinning.Compute"/>
    /// — single source-of-truth for equal-width binning across every series that needs it.</remarks>
    public HistogramBins ComputeBins() => Numerics.HistogramBinning.Compute(Data, Bins);
}
