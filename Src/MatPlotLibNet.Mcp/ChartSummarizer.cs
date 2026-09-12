// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Mcp;

/// <summary>What one series in a figure turned out to be. <see cref="PointCount"/> and the ranges are nullable
/// because not every chart has them: a pie has slices, not points, and a series whose data is empty has no range
/// at all. Reporting 0 there would read as an empty chart for a chart that draws perfectly well.</summary>
internal readonly record struct SeriesSummary(
    string Discriminator,
    string? Label,
    int? PointCount,
    MinMaxRange? XRange,
    MinMaxRange? YRange);

/// <summary>What the server drew, in the few facts a model can act on without seeing the picture.</summary>
internal readonly record struct FigureSummary(
    string? Title,
    double Width,
    double Height,
    int SubPlotCount,
    IReadOnlyList<SeriesSummary> Series)
{
    /// <summary>The summary as the text block that travels beside the image.</summary>
    public string ToText()
    {
        var text = new StringBuilder();
        text.Append(Title is { Length: > 0 } title ? $"\"{title}\"" : "Untitled chart")
            .Append(Invariant($" — {Width:0.#}×{Height:0.#}, "))
            .Append(SubPlotCount == 1 ? "1 subplot" : $"{SubPlotCount} subplots")
            .Append(Series.Count == 1 ? ", 1 series" : $", {Series.Count} series");

        foreach (var series in Series)
        {
            text.AppendLine().Append("  · ").Append(series.Discriminator);
            if (series.Label is { Length: > 0 } label)
            {
                text.Append(" \"").Append(label).Append('"');
            }
            if (series.PointCount is { } count)
            {
                text.Append(Invariant($", {count} points"));
            }
            if (series.XRange is { } x)
            {
                text.Append(Invariant($", x {x.Min:0.###}…{x.Max:0.###}"));
            }
            if (series.YRange is { } y)
            {
                text.Append(Invariant($", y {y.Min:0.###}…{y.Max:0.###}"));
            }
        }

        return text.ToString();
    }

    private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
}

/// <summary>Describes a rendered figure in words. The ranges come from <see cref="ISeries.ComputeDataRange"/> —
/// the same call the renderer makes to decide the axes — so the text beside the picture and the picture itself can
/// never disagree. A series whose range computation throws (an empty candlestick asks <c>Low.Min()</c>) loses its
/// range, never the whole call.</summary>
internal sealed class ChartSummarizer
{
    /// <summary>The summary of <paramref name="figure"/>, subplot by subplot, series by series.</summary>
    public FigureSummary Describe(Figure figure)
    {
        var series = new List<SeriesSummary>();
        foreach (var axes in figure.SubPlots)
        {
            var context = new AxesSnapshot(axes);
            foreach (var item in axes.AllSeries)
            {
                series.Add(Describe(item, context));
            }
        }

        return new FigureSummary(figure.Title, figure.Width, figure.Height, figure.SubPlots.Count, series);
    }

    private static SeriesSummary Describe(ISeries series, IAxesContext context)
    {
        DataRangeContribution? range = null;
        try
        {
            range = series.ComputeDataRange(context);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or IndexOutOfRangeException)
        {
            // A series with no data cannot say where it sits. That is a gap in the summary, not a failed render.
        }

        return new SeriesSummary(
            series.ToSeriesDto().Type ?? "unknown",
            series.Label,
            PointCountOf(series),
            RangeOf(range?.XMin, range?.XMax),
            RangeOf(range?.YMin, range?.YMax));
    }

    private static MinMaxRange? RangeOf(double? min, double? max) =>
        min is { } lo && max is { } hi && !double.IsNaN(lo) && !double.IsNaN(hi) ? new MinMaxRange(lo, hi) : null;

    /// <summary>How many data points the series holds, for the families that HAVE points. The library exposes no
    /// uniform count — a heatmap is a grid, a pie is a set of slices, a tree has nodes — and inventing one per
    /// discriminator would be a second definition of the data beside the serializer's.</summary>
    private static int? PointCountOf(ISeries series) => series switch
    {
        XYSeries xy => xy.YData.Length,
        OhlcSeries ohlc => ohlc.Close.Length,
        PolarSeries polar => polar.R.Length,
        DatasetSeries datasets => datasets.Datasets.Sum(set => set.Length),
        _ => null,
    };

    /// <summary>The read-only view of a subplot that <see cref="ISeries.ComputeDataRange"/> asks for. The renderer's
    /// own adapter is internal to the core package, and the interface it implements is public precisely so a
    /// consumer can present the same view rather than a second interpretation of it.</summary>
    private sealed class AxesSnapshot(Axes axes) : IAxesContext
    {
        public double? XAxisMin => axes.XAxis.Min;

        public double? XAxisMax => axes.XAxis.Max;

        public double? YAxisMin => axes.YAxis.Min;

        public double? YAxisMax => axes.YAxis.Max;

        public BarMode BarMode => axes.BarMode;

        public IReadOnlyList<ISeries> AllSeries => axes.Series;

        public AxisScale XScale => axes.XAxis.Scale;

        public AxisScale YScale => axes.YAxis.Scale;
    }
}
