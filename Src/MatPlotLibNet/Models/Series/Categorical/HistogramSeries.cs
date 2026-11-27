// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a histogram series that bins continuous data into discrete intervals.</summary>
public sealed class HistogramSeries : ChartSeries, IHasColor, IHasAlpha, IHasEdgeColor, IEdgedPatch, IHatched, ISeriesKind<HistogramSeries>
{
    /// <inheritdoc />
    /// <remarks>The bins' opacity is the one their fill is drawn at, which replaces the opacity of their colours.</remarks>
    GivenPatchEdge IEdgedPatch.GivenEdge => new(EdgeColor, null, Alpha);

    /// <summary>Under the library's own themes a bin is outlined half a pixel wide in its edge colour, else in the theme's patch edge
    /// colour; with neither it has no outline.</summary>
    PatchEdge IEdgedPatch.LibraryEdge(Color? fill, Color? themeEdge) => (EdgeColor ?? themeEdge) is { } color ? PatchEdge.HalfPixel(color) : PatchEdge.None;

    /// <summary>The raw observations to bin; a missing value (NaN) or an infinity is not counted.</summary>
    public double[] Data { get; }

    /// <summary>The number of bins a histogram divides its data into when it is told none (matplotlib's <c>hist.bins</c>): the default
    /// of every verb that draws one, on an axes, a figure, a list of records and a data frame alike.</summary>
    public const int RestingBins = 10;

    /// <summary>The number of equal-width bins the range of the data is divided into. Default <see cref="RestingBins"/>.</summary>
    public int Bins { get; set => Change(ref field, value); } = RestingBins;

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public Color? EdgeColor { get; set => Change(ref field, value); }

    /// <inheritdoc />
    /// <remarks>Edged as the bins are under the matplotlib themes: the edge colour given, else the theme's.</remarks>
    internal override LegendSwatch LegendSwatch => LegendSwatch.EdgedPatch;

    private const double RestingAlpha = 1.0;
    /// <inheritdoc />
    public double Alpha { get; set => Change(ref field, value.ThrowIfNotOpacity(nameof(Alpha))); } = RestingAlpha;

    /// <summary>When <see langword="true"/> the bar heights are scaled so that all bars together cover an area of 1,
    /// instead of showing counts. Default <see langword="false"/>.</summary>
    public bool Density { get; set => Change(ref field, value); } = false;

    /// <summary>When <see langword="true"/> each bar shows the running total of its bin and every bin before it,
    /// instead of its own height. Default <see langword="false"/>.</summary>
    public bool Cumulative { get; set => Change(ref field, value); } = false;

    private const HistType RestingHistType = HistType.Bar;
    /// <summary>How the bins are drawn: as separate bars (<see cref="RestingHistType"/>, the default), as an unfilled step outline
    /// (<c>Step</c>) or as a filled one (<c>StepFilled</c>).</summary>
    public HistType HistType { get; set => Change(ref field, value); } = RestingHistType;

    /// <summary>The weight of each observation, one per entry of <see cref="Data"/>: a bin then rises by the sum of the
    /// weights of its observations instead of their count. Ignored unless there is exactly one weight per
    /// observation.</summary>
    public double[]? Weights { get; set => Change(ref field, value); }

    private const double RestingRWidth = 1.0;
    /// <summary>How wide each bar is, as a fraction of its bin: the default, <see cref="RestingRWidth"/>, makes neighbouring bars touch, smaller
    /// values leave a gap. Only bar histograms use it.</summary>
    public double RWidth { get; set => Change(ref field, value); } = RestingRWidth;

    /// <summary>The pattern laid over the fill of the bars (or of a filled step outline);
    /// <see cref="HatchPattern.None"/>, the default, leaves them plain.</summary>
    public HatchPattern Hatch { get; set => Change(ref field, value); } = HatchPattern.None;

    /// <summary>The colour of the hatch lines. <see langword="null"/> draws them as the theme does: in the colour of the bars' edge, black
    /// when they have none, under v2, black under classic (matplotlib's <c>hatch.color</c>), and under the library's own themes in the
    /// edge colour given, else in a colour that contrasts with the fill. A colour given takes the bars' opacity.</summary>
    public Color? HatchColor { get; set => Change(ref field, value); }

    /// <summary>Initializes a new instance of <see cref="HistogramSeries"/> with the specified data.</summary>
    /// <param name="data">The raw data values to be binned.</param>
    public HistogramSeries(double[] data)
    {
        Data = data;
    }

    /// <inheritdoc />
    /// <remarks>Along X the bins drawn, not the observations: from the low edge of the first bin to the high edge of the last
    /// (numpy's, so observations of one value span it and half a unit either side), a bar
    /// narrowed in its bin by <see cref="RWidth"/> as it is drawn. Along Y the heights the bars are drawn at (counts,
    /// weighted, as a density, or cumulative) and the zero they stand on, over the values the Y axis can place: on a log axis, which has
    /// no place for zero, from the lowest bin.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        var scale = context.YScale.Rule;
        var reached = Data.Length > 0 ? Heights(ComputeBins()).DrawableRange(scale) : null;
        var y = (reached ?? Range1D.Empty).Including(0, scale).OrNone();
        // The bins span the observations they count: a missing one (NaN) and an infinity are left out, and with none there is no bin.
        var x = Data.Length > 0 ? BinsDrawn() : new Range1D(0, 1);
        double? xMin = x?.Lo, xMax = x?.Hi;
        // Only the foot is sticky: matplotlib's hist draws its bars through bar, which registers each bar's foot and no other edge, so
        // the margin pads the extent of the data along X.
        return new(xMin, xMax, y?.Lo, y?.Hi, StickyYMin: 0);

        // A bar stands in its bin as the renderer stands it (Range1D.CentredIn), so the first bar's inset is the last one's.
        Range1D? BinsDrawn()
        {
            if (Numerics.HistogramBinning.GridOf(Data, Bins).Spread is not { } bins) return null;
            double inset = HistType == HistType.Bar ? Range1D.CentredIn(bins.Low, bins.Width, bins.Width * RWidth).Lo - bins.Low : 0;
            return new Range1D(bins.Low + inset, bins.High - inset);
        }
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
            // Weighted binning: each counted observation's weight in its bin, the bins the counts were made in.
            var grid = Numerics.HistogramBinning.GridOf(Data, n);
            for (int j = 0; j < Data.Length; j++)
            {
                if (grid.BinOf(Data[j]) is var bin and >= 0) heights[bin] += Weights[j];
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

    static string ISeriesKind<HistogramSeries>.Kind => "histogram";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        Data = Data, Bins = Bins, Color = Color,
        EdgeColor = EdgeColor,
        Alpha = Alpha.OrNullAtRest(RestingAlpha),
        Density = Density.OrNullAtRest(false),
        Cumulative = Cumulative.OrNullAtRest(false),
        HistType = HistType.OrNullAtRest(RestingHistType),
        Weights = Weights,
        RWidth = RWidth.OrNullAtRest(RestingRWidth),
        Hatching = this.Hatching,
    };

    /// <summary>The reader of a <see cref="HistogramSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static HistogramSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Hist(dto.Data ?? [], dto.Bins ?? RestingBins);
        s.Color = dto.Color;
        s.EdgeColor = dto.EdgeColor;
        if (dto.Alpha is { } alpha) s.Alpha = alpha;
        if (dto.Density is { } density) s.Density = density;
        if (dto.Cumulative is { } cumulative) s.Cumulative = cumulative;
        if (dto.HistType is { } histType) s.HistType = histType;
        s.Weights = dto.Weights;
        if (dto.RWidth is { } rWidth) s.RWidth = rWidth;
        s.LayHatching(dto.Hatching);
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
            rows[i] = [DataCell.FromNumber(bins.EdgeAt(i)), DataCell.FromNumber(bins.Counts[i])];
        }

        return new ChartDataTable(null, [ChartDataColumn.Position(DataAxis.X, "bin start"), new(Label ?? "count")], rows);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);

    /// <summary>Computes histogram bin counts. Used by both range computation and rendering.</summary>
    /// <remarks>Delegates to the shared binning
    /// — single source-of-truth for equal-width binning across every series that needs it.</remarks>
    public HistogramBins ComputeBins() => Numerics.HistogramBinning.Compute(Data, Bins);
}
