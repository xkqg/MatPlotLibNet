// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a box plot series displaying statistical distribution summaries.</summary>
public sealed class BoxSeries : DatasetSeries, IHasColor, IThemedRange, ISeriesKind<BoxSeries>
{
    /// <inheritdoc />
    /// <remarks>2: <c>boxplot</c> draws its box, whiskers, caps and median as <c>Line2D</c>s. matplotlib's <c>boxplot</c> takes the colours of <c>boxplot.*props</c> and asks neither cycle, so under the matplotlib themes the series after a box wears the colour it would have worn without it; a box that a verb put in a colour group stands in the group's slot.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Box;

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>The colour of the median line across each box; <see langword="null"/> (the default) draws it in the theme's median colour
    /// under the matplotlib themes (orange in the default style, red in the classic one) and red under the library's own.</summary>
    public Color? MedianColor { get; set => Change(ref field, value); }

    private const bool RestingShowOutliers = true;
    /// <summary>Whether the observations beyond the whiskers are to be marked as outliers. Default
    /// <see cref="RestingShowOutliers"/>. Under the matplotlib themes they are marked as matplotlib marks its fliers (open circles in the default
    /// style, plus signs in the classic one); under the library's own themes no outlier is marked whatever the value, so it does not
    /// change the picture there. The value is kept and saved with the series.</summary>
    public bool ShowOutliers { get; set => Change(ref field, value); } = RestingShowOutliers;

    /// <summary>Width of each box in data units of the position axis, where positions are 1 apart by default. A box plot given no
    /// width reads <see cref="RestingWidths"/>. It is drawn that wide under the library's own themes and, under the matplotlib themes, as matplotlib's
    /// <c>boxplot</c> draws it: 0.15 of the span between the outer positions, at least 0.15 and at most 0.5 (0.45 for four boxes at 1 to
    /// 4); a width that was given, the default included, is drawn as it is under every theme.</summary>
    public double Widths { get => GivenWidths ?? RestingWidths; set => GivenWidths = value; }

    /// <summary>The value of <see cref="Widths"/> its author gave, <see langword="null"/> while none was.</summary>
    internal double? GivenWidths { get; private set => Change(ref field, value); }

    private const bool RestingVert = true;
    /// <summary>When <see langword="true"/> (the default, <see cref="RestingVert"/>) the boxes stand upright, with positions along X and values
    /// along Y; when <see langword="false"/> they lie down, with positions along Y and values along X.</summary>
    public bool Vert { get; set => Change(ref field, value); } = RestingVert;

    /// <summary>How far a whisker reaches past its box, as a multiple of the interquartile range; the whisker stops at
    /// the most extreme observation inside that reach. Default <see cref="RestingWhis"/>.</summary>
    public double Whis { get; set => Change(ref field, value); } = RestingWhis;

    private const double RestingWidths = 0.5;
    private const double RestingWhis = 1.5;

    /// <summary>When <see langword="true"/> each box also gets a marker at the mean of its data (a triangle in the default matplotlib
    /// style, a square in the classic one, a green dot under the library's own themes). Default <see langword="false"/>.</summary>
    public bool ShowMeans { get; set => Change(ref field, value); }

    /// <summary>Where each box stands along the axis that carries the positions (X for upright boxes, Y for boxes that lie
    /// down), one position per dataset. A dataset the array does not reach, or every dataset when it is <see langword="null"/>,
    /// stands at its index under the library's own themes and at its index plus one under the matplotlib themes, which stand the
    /// boxes at 1, 2, ... as matplotlib's <c>boxplot</c> does, end the axis half a unit past the outer boxes with no margin and put
    /// the ticks at the boxes; positions that were given are the positions under every theme. The automatic range follows: it covers the
    /// places the boxes are drawn at, half a box's <see cref="Widths"/> to either side.</summary>
    public double[]? Positions { get; set => Change(ref field, value); }

    /// <summary>Initializes a new instance of <see cref="BoxSeries"/> with the specified datasets.</summary>
    /// <param name="datasets">An array of datasets, each containing the values for one box.</param>
    public BoxSeries(double[][] datasets) : base(datasets) { }

    /// <summary>How far a box reaches to either side of its position, in data units: half its width.</summary>
    internal double HalfWidth => Widths / 2;

    /// <summary>Where the box of the dataset at <paramref name="index"/> stands under <paramref name="theme"/>: the position the
    /// series was given for it, otherwise its index from the theme's first position on (<see cref="BoxStanding.Datasets"/>: 1 under the
    /// matplotlib themes, <c>bxp</c>'s default <c>positions</c> 1, 2, ..., n, and 0 under the library's own).</summary>
    /// <param name="index">The index of the dataset.</param>
    /// <param name="theme">The theme the series is drawn under.</param>
    /// <returns>The position along the axis that carries the positions.</returns>
    internal double PositionIn(int index, Theme theme) => Positions.PositionOf(index, theme.Looks.Box.Standing.Datasets);

    /// <summary>How wide each box is drawn under <paramref name="theme"/>: the width the series was given; otherwise the theme's
    /// (<see cref="BoxStanding"/>: <see cref="RestingWidths"/> under the library's own, and under the matplotlib themes matplotlib's
    /// <c>clip(0.15 * ptp(positions), 0.15, 0.5)</c> over the positions of all the datasets).</summary>
    /// <param name="theme">The theme the series is drawn under.</param>
    /// <returns>The width, in units of the position axis.</returns>
    internal double WidthIn(Theme theme)
    {
        if (GivenWidths is { } given) return given;
        var standing = theme.Looks.Box.Standing;
        if (!standing.WidthFollowsSpan) return standing.WidthMost;

        double low = double.PositiveInfinity, high = double.NegativeInfinity;
        for (int i = 0; i < Datasets.Length; i++)
        {
            double at = PositionIn(i, theme);
            low = Math.Min(low, at);
            high = Math.Max(high, at);
        }

        return standing.WidthOver(high >= low ? high - low : 0);
    }

    /// <summary>How far a box reaches to either side of its position under <paramref name="theme"/>: half <see cref="WidthIn"/>.</summary>
    /// <param name="theme">The theme the series is drawn under.</param>
    /// <returns>Half the width.</returns>
    internal double HalfWidthIn(Theme theme) => WidthIn(theme) / 2;

    /// <inheritdoc />
    internal override bool RangesItsPositionsWhenEmpty => true;

    /// <inheritdoc />
    /// <remarks>Under a theme whose boxes reach past the outer ones (<see cref="BoxStanding.EdgeReach"/>, the matplotlib themes) the axis
    /// along the positions ends half a unit past the outer boxes, whatever their width, and the ends are sticky, so no margin pads
    /// them: matplotlib's <c>bxp</c> widens the data limits to <c>min(positions) - 0.5</c> and <c>max(positions) + 0.5</c> and makes
    /// those the sticky edges of the medians. Under a theme that names none the range is the one <see cref="ComputeDataRange"/> gives.</remarks>
    DataRangeContribution IThemedRange.RangeUnder(IAxesContext context, Theme theme)
    {
        var standing = theme.Looks.Box.Standing;
        if (standing.EdgeReach is not { } reach) return ComputeDataRange(context);

        double low = double.PositiveInfinity, high = double.NegativeInfinity;
        for (int i = 0; i < Datasets.Length; i++)
        {
            double at = Positions.PositionOf(i, standing.Datasets);
            low = Math.Min(low, at);
            high = Math.Max(high, at);
        }

        var covered = DrawnAround(standing.Datasets, HalfWidthIn(theme)).DrawableRange();
        double? stickyLo = high >= low ? low - reach : null, stickyHi = high >= low ? high + reach : null;
        double? placesLo = covered is { } c ? Math.Min(c.Lo, stickyLo ?? c.Lo) : stickyLo;
        double? placesHi = covered is { } d ? Math.Max(d.Hi, stickyHi ?? d.Hi) : stickyHi;
        double? limitMin = context.LimitMinAlong(Vert);
        double? limitMax = context.LimitMaxAlong(Vert);
        var y = Datasets.DrawableRange(context.ScaleAlong(!Vert).Rule);
        var upright = new DataRangeContribution(limitMin ?? placesLo, limitMax ?? placesHi, y?.Lo, y?.Hi, StickyXMin: stickyLo, StickyXMax: stickyHi);
        return upright.OnAxes(BarOrientation.ValuesAlong(!Vert));
    }

    /// <inheritdoc />
    /// <remarks>Measured as boxes standing upright measure it, the positions along X and the values along Y, and put on the
    /// axes the boxes are drawn on: boxes that lie down (<see cref="Vert"/> false) range their positions along Y and their
    /// values along X. Along the positions, the room each box is drawn in around its position (half its width either
    /// side). A series that sets <see cref="Positions"/> ranges over those places alone; one that does not keeps the half
    /// unit around the default positions that it always had, widened for a box wider than that. A limit set on the axis
    /// that carries the positions is their range.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        var places = DrawnAround(DatasetStanding.Library, HalfWidth).CoverAlongPositions(Positions, new Range1D(-0.5, Datasets.Length - 0.5));
        // The positions run along X for boxes that stand up (their limits are the X axis'), along Y for boxes that lie down.
        double? limitMin = context.LimitMinAlong(Vert);
        double? limitMax = context.LimitMaxAlong(Vert);
        // Over the values the value axis can place: a missing sample (NaN) and an infinity are not part of the extent, and
        // on a log axis neither is 0 or below (the box clips its lower statistic to the plot's edge there), so the range
        // starts at the lowest value it can place.
        var y = Datasets.DrawableRange(context.ScaleAlong(!Vert).Rule);
        var upright = new DataRangeContribution(limitMin ?? places?.Lo, limitMax ?? places?.Hi, y?.Lo, y?.Hi);
        return upright.OnAxes(BarOrientation.ValuesAlong(!Vert));
    }

    /// <summary>What each box that can be drawn covers, at the position it stands at (by <paramref name="standing"/> where none was
    /// named) and <paramref name="halfWidth"/> to either side.</summary>
    private IEnumerable<Range1D> DrawnAround(DatasetStanding standing, double halfWidth)
    {
        for (int i = 0; i < Datasets.Length; i++)
        {
            if (Datasets[i].DrawableRange() is null) continue;
            double at = Positions.PositionOf(i, standing);
            yield return new Range1D(at - halfWidth, at + halfWidth);
        }
    }

    static string ISeriesKind<BoxSeries>.Kind => "box";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(), Datasets = Datasets,
        Color = Color,
        MedianColor = MedianColor,
        ShowOutliers = ShowOutliers.OrNullAtRest(RestingShowOutliers),
        Widths = GivenWidths,
        Vert = Vert.OrNullAtRest(RestingVert),
        Whis = Whis.OrNullAtRest(RestingWhis),
        ShowMeans = ShowMeans.OrNullAtRest(false),
        Positions = Positions,
    };

    /// <summary>The reader of a <see cref="BoxSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static BoxSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.BoxPlot(dto.Datasets ?? []);
        s.Color = dto.Color;
        s.MedianColor = dto.MedianColor;
        if (dto.ShowOutliers is { } showOutliers) s.ShowOutliers = showOutliers;
        if (dto.Widths is { } widths) s.Widths = widths;
        if (dto.Vert is { } vert) s.Vert = vert;
        if (dto.Whis is { } whis) s.Whis = whis;
        if (dto.ShowMeans is { } showMeans) s.ShowMeans = showMeans;
        s.Positions = dto.Positions;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
