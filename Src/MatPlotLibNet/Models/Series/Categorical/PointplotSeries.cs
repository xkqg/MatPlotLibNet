// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a point plot series that shows the mean ± confidence interval for each category.</summary>
public sealed class PointplotSeries : DatasetSeries, IHasColor, IThemedRange, IDotSeries, ISeriesKind<PointplotSeries>
{
    /// <inheritdoc />
    /// <remarks>2: seaborn's <c>pointplot</c> draws its estimates and bars with <c>plot</c>, <c>Line2D</c>s.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Line;

    /// <summary>The name of each category, in the order of the datasets; it names the rows of the series' data table.
    /// <see langword="null"/> numbers the categories from 1.</summary>
    public string[]? Categories { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>Diameter of the dot that marks the mean of each category, in pixels. Given none, the dot is
    /// <c>lines.linewidth * 1.8 * sqrt(2 * pi)</c> points across under the matplotlib themes, as seaborn's <c>pointplot</c> sets it (6.77 points
    /// under the default style), and as <see cref="Rests"/> says under every other theme; a size that is given is drawn as pixels.</summary>
    public double MarkerSize { get => Dot.Diameter ?? Rests.Diameter; set => Dot = Dot with { Diameter = value }; }

    /// <summary>What was named of the dot (the diameter); <see langword="null"/> while none was.</summary>
    internal GivenDot Dot { get; set => Change(ref field, value); }

    /// <summary>What a point plot's dot rests at under the library's own themes.</summary>
    private static readonly RestingDot Rests = new(8);

    GivenDot IDotSeries.Given => Dot;

    RestingDot IDotSeries.Resting => Rests;

    /// <inheritdoc />
    /// <remarks>seaborn's <c>pointplot</c> sets <c>markersize = lines.linewidth * 1.8 * sqrt(2 * pi)</c> (6.7679 points under the default
    /// style, 4.5119 under classic, read off <c>ax.lines[0].get_markersize()</c>); the theme's line width is in pixels already.</remarks>
    double IDotSeries.SeabornDiameter(SeriesSlot slot, DotLook look) =>
        slot.LineWidthOf(null, look.PointplotLine) * (look.MeanPerLine ?? 1);

    /// <summary>Width of the caps at the two ends of each interval, in category units (neighbouring categories are 1 apart). A series
    /// given none reads <see cref="RestingCapSize"/> and draws that wide caps under the library's own themes; under the matplotlib themes it draws none, as
    /// seaborn's <c>pointplot</c> does (<c>capsize=0</c>). A width that was given, the default included, is drawn as it is under every theme.</summary>
    public double CapSize { get => GivenCapSize ?? RestingCapSize; set => GivenCapSize = value; }

    /// <summary>The value of <see cref="CapSize"/> its author gave, <see langword="null"/> while none was.</summary>
    internal double? GivenCapSize { get; private set => Change(ref field, value); }

    private const double RestingCapSize = 0.2;

    /// <summary>The width of the caps under <paramref name="theme"/>, in category units: the one given, else none where the theme draws a
    /// point plot as seaborn does (<see cref="DotLook.LikeSeaborn"/>), else <see cref="RestingCapSize"/>.</summary>
    /// <param name="theme">The theme the series is drawn under.</param>
    /// <returns>The width of a cap.</returns>
    internal double CapSizeIn(Theme theme) => theme.Looks.Dots.CapSizeOver(GivenCapSize, RestingCapSize);

    /// <summary>The share of the observations of each category that its interval covers, between 0 and 1: the default <see cref="Numerics.ConfidenceBand.DefaultLevel"/>. Under the
    /// library's own themes the interval runs from the 2.5th to the 97.5th percentile of the observations, and a category with fewer than
    /// two observations gets an interval of zero length. Under the matplotlib themes it is the confidence interval of the mean of the
    /// category (Student's t with the category's own spread), as seaborn's <c>pointplot</c> draws it (<c>errorbar=("ci", 95)</c>).</summary>
    public double ConfidenceLevel { get; set => Change(ref field, value); } = Numerics.ConfidenceBand.DefaultLevel;

    /// <summary>The width of the lines of the plot (the intervals and the line through the means), in pixels. A series given none reads
    /// the rest of the library's own width and is drawn that wide under the library's own themes; under the matplotlib themes its
    /// lines are as wide as seaborn draws them, <c>lines.linewidth * 1.8</c> points (2.7 under the default style, 1.8 under the classic
    /// one). A width that was given is drawn as it is under every theme.</summary>
    public double LineWidth { get => GivenLineWidth ?? DotLook.Library.PointplotLine.Resting; set => GivenLineWidth = value; }

    /// <summary>The width the point plot was given; <see langword="null"/> when it was given none, and its look decides
    /// (<see cref="DotLook.PointplotLine"/>).</summary>
    internal double? GivenLineWidth { get; private set => Change(ref field, value); }

    /// <summary>The width the lines are drawn at in <paramref name="slot"/>: the one given, else the rule of the theme's dot look
    /// (<see cref="DotLook.PointplotLine"/>: seaborn's <c>lines.linewidth * 1.8</c> under its themes).</summary>
    /// <param name="slot">The slot the series is drawn in.</param>
    /// <returns>The width, in pixels.</returns>
    internal double LineWidthIn(Rendering.SeriesSlot slot) => slot.LineWidthOf(GivenLineWidth, slot.Theme.Looks.Dots.PointplotLine);

    /// <summary>Whether a line joins the means of neighbouring categories. A series given none does not join them under the library's
    /// own themes and does under the matplotlib themes, as seaborn's <c>pointplot</c> does (<c>join=True</c>); a choice that was given
    /// is followed under every theme.</summary>
    public bool JoinPoints { get => GivenJoinPoints ?? false; set => GivenJoinPoints = value; }

    /// <summary>The value of <see cref="JoinPoints"/> its author gave, <see langword="null"/> while none was.</summary>
    internal bool? GivenJoinPoints { get; private set => Change(ref field, value); }

    /// <summary>Whether the means are joined under <paramref name="theme"/>: the choice given, else yes where the theme draws a point plot
    /// as seaborn does (<see cref="DotLook.LikeSeaborn"/>), else no.</summary>
    /// <param name="theme">The theme the series is drawn under.</param>
    /// <returns><see langword="true"/> to draw the line through the means.</returns>
    internal bool IsJoinedIn(Theme theme) => theme.Looks.Dots.JoinsPointsOver(GivenJoinPoints);

    /// <summary>The mean of category <paramref name="index"/> and the two ends of its interval under <paramref name="theme"/>, the one place
    /// the renderer and the range ask: the confidence interval of the mean where the theme draws a point plot as seaborn does
    /// (<see cref="DotLook.LikeSeaborn"/>), else half the percentile range of the observations either side of the mean.</summary>
    /// <param name="index">The category, from 0.</param>
    /// <param name="theme">The theme the series is drawn under.</param>
    /// <returns>The estimate; <see langword="null"/> for a category with no observation.</returns>
    internal PointEstimate? EstimateOf(int index, Theme theme)
    {
        var data = Datasets[index];
        if (data.Length == 0) return null;

        Numerics.Vec v = data;
        double mean = v.Mean();
        double half = v.Length < 2 ? 0
            : theme.Looks.Dots.LikeSeaborn ? CriticalValue(v.Length - 1) * StandardError(data, mean)
            : PercentileHalfRange(v, ConfidenceLevel);
        return new PointEstimate(mean, mean - half, mean + half);
    }

    /// <summary>The standard error of the mean of <paramref name="data"/>: the sample variance over the count, under a square root (the
    /// run is centred in a buffer on the stack or from the pool, so a draw allocates none).</summary>
    private static double StandardError(double[] data, double mean) => Math.Sqrt(data.AsSpan().Variance(mean, ddof: 1) / data.Length);

    /// <summary>The two-sided critical value of Student's t at <see cref="ConfidenceLevel"/> for <paramref name="degreesOfFreedom"/>,
    /// found by bisection (<see cref="Numerics.StudentT.Quantile"/>); the last one asked is kept with the level and the degrees of
    /// freedom it answers, so every group of the same size in a draw, and every draw, shares one bisection. Nothing can go stale: the
    /// key is what the value is made from.</summary>
    private double CriticalValue(int degreesOfFreedom)
    {
        var kept = _critical;
        if (kept is { } k && k.Level == ConfidenceLevel && k.DegreesOfFreedom == degreesOfFreedom) return k.Value;

        double value = Numerics.StudentT.Quantile((1.0 + ConfidenceLevel) / 2.0, degreesOfFreedom);
        _critical = new CriticalValueKept(ConfidenceLevel, degreesOfFreedom, value);
        return value;
    }

    private CriticalValueKept? _critical;

    private sealed record CriticalValueKept(double Level, int DegreesOfFreedom, double Value);

    private static double PercentileHalfRange(Numerics.Vec v, double level)
    {
        double p = (1.0 - level) / 2.0 * 100.0;
        return (v.Percentile(100.0 - p) - v.Percentile(p)) / 2.0;
    }

    /// <inheritdoc />
    /// <remarks>Under the matplotlib themes the Y range is the lines': the lowest end of an interval and the highest, as seaborn's axes
    /// end at the lines it draws; under the library's own themes it is the observations' (<see cref="DatasetSeries"/>).</remarks>
    DataRangeContribution IThemedRange.RangeUnder(IAxesContext context, Theme theme)
    {
        if (!theme.Looks.Dots.LikeSeaborn) return ComputeDataRange(context);

        double low = double.PositiveInfinity, high = double.NegativeInfinity;
        for (int i = 0; i < Datasets.Length; i++)
        {
            if (EstimateOf(i, theme) is not { } estimate || !double.IsFinite(estimate.Lower) || !double.IsFinite(estimate.Upper)) continue;
            low = Math.Min(low, estimate.Lower);
            high = Math.Max(high, estimate.Upper);
        }

        return high >= low ? new(-0.5, Datasets.Length - 0.5, low, high) : ComputeDataRange(context);
    }

    /// <summary>Initializes a new instance of <see cref="PointplotSeries"/> with the specified datasets.</summary>
    /// <param name="datasets">An array of datasets, each containing values for one category.</param>
    public PointplotSeries(double[][] datasets) : base(datasets) { }

    static string ISeriesKind<PointplotSeries>.Kind => "pointplot";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        Datasets = Datasets,
        Categories = Categories,
        Color = Color,
        MarkerSize = Dot.Diameter,
        CapSize = GivenCapSize,
        ConfidenceLevel = ConfidenceLevel,
        LineWidth = GivenLineWidth,
        JoinPoints = GivenJoinPoints,
    };

    /// <summary>The reader of a <see cref="PointplotSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static PointplotSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Pointplot(dto.Datasets ?? []);
        if (dto.MarkerSize is { } markerSize) s.MarkerSize = markerSize;
        if (dto.CapSize is { } capSize) s.CapSize = capSize;
        if (dto.ConfidenceLevel is { } confidenceLevel) s.ConfidenceLevel = confidenceLevel;
        if (dto.LineWidth is { } lineWidth) s.LineWidth = lineWidth;
        if (dto.JoinPoints is { } joinPoints) s.JoinPoints = joinPoints;
        if (dto.Color is { } color) s.Color = color;
        if (dto.Categories is { } categories) s.Categories = categories;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>Long form over the samples, named by the category the point plot draws them under.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.OfGroups([new("category", DataColumnKind.Text), new(Label ?? "value")], Categories, "", Datasets,
            static value => [DataCell.FromNumber(value)]);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}

/// <summary>A category of a point plot as it is drawn: the mean and the two ends of its interval.</summary>
/// <param name="Mean">The mean of the category's observations.</param>
/// <param name="Lower">The lower end of the interval.</param>
/// <param name="Upper">The upper end of the interval.</param>
internal readonly record struct PointEstimate(double Mean, double Lower, double Upper);
