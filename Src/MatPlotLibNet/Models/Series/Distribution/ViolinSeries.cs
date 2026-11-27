// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a violin plot series displaying kernel density estimations for datasets.</summary>
public sealed class ViolinSeries : DatasetSeries, IHasColor, IHasAlpha, IKeepsDataCopy, IEdgedPatch, IThemedRange, ISeriesKind<ViolinSeries>
{
    /// <summary>A body gives no edge of its own; its opacity (<see cref="Alpha"/>) is matplotlib's <c>alpha</c> of the body, which the
    /// edge a theme decides takes too.</summary>
    GivenPatchEdge IEdgedPatch.GivenEdge => new(null, null, Alpha);

    /// <summary>Under the library's own themes a body is outlined by one pixel in its own colour.</summary>
    PatchEdge IEdgedPatch.LibraryEdge(Color? fill, Color? themeEdge) => PatchEdge.Pixel(fill);

    /// <inheritdoc />
    /// <remarks>The body as it is drawn under the matplotlib themes (matplotlib's <c>HandlerPolyCollection</c>): its face and edge at its
    /// opacity, black at 1 pt under classic; a plain rectangle in its colour under the library's own themes.</remarks>
    internal override LegendSwatch LegendSwatch => LegendSwatch.EdgedPatch;

    /// <inheritdoc />
    /// <remarks>matplotlib's <c>violinplot</c>.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Violin;

    /// <summary>The colour of the violin's body and of the lines drawn over it. Left unset, the violin wears the colour its
    /// theme gives violins (matplotlib's classic style: a yellow body with red lines) and, when the theme gives none, the
    /// theme's colour at the series' place in the cycle. The legend swatch wears the colour of the body, by the same rule.</summary>
    public Color? Color { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public double Alpha { get; set => Change(ref field, value.ThrowIfNotOpacity(nameof(Alpha))); } = RestingAlpha;

    /// <summary>When <see langword="true"/> a line is drawn across each violin at the mean of its data. Default
    /// <see langword="false"/>.</summary>
    public bool ShowMeans { get; set => Change(ref field, value); }

    /// <summary>When <see langword="true"/> a line is drawn across each violin at the median of its data. Default
    /// <see langword="false"/>.</summary>
    public bool ShowMedians { get; set => Change(ref field, value); }

    private const bool RestingShowExtrema = true;
    /// <summary>When <see langword="true"/> (the default, <see cref="RestingShowExtrema"/>) a short tick marks the smallest and the largest observation
    /// of each violin.</summary>
    public bool ShowExtrema { get; set => Change(ref field, value); } = RestingShowExtrema;

    /// <summary>Where each violin stands along the X axis, one position per dataset; a dataset the array does not reach, or every
    /// dataset when it is <see langword="null"/>, stands at its index under the library's own themes and at its index plus one under the
    /// matplotlib themes, as matplotlib's <c>violinplot</c> stands them at 1, 2, ... (a box plot stands by the same rule). The automatic X
    /// range follows: it covers the places the violins are drawn at, a violin's <see cref="Widths"/> to either side (one side only for a
    /// half violin, see <see cref="Side"/>).</summary>
    public double[]? Positions { get; set => Change(ref field, value); }

    /// <summary>How wide a violin's body is at its widest, in data units: under the library's own themes it reaches this far to either
    /// side of its position; under the matplotlib themes it is the whole width, as matplotlib's <c>widths</c> is (half of it to either
    /// side), and the lines over the body reach a quarter of it to either side.</summary>
    public double Widths { get; set => Change(ref field, value); } = RestingWidths;

    /// <summary>Which side of its position each violin is drawn on: <see cref="RestingSide"/>, the default, draws the whole violin,
    /// <c>Low</c> only its left half and <c>High</c> only its right half.</summary>
    public ViolinSide Side { get; set => Change(ref field, value); } = RestingSide;

    private const double RestingAlpha = 0.3;
    private const double RestingWidths = 0.5;
    private const ViolinSide RestingSide = ViolinSide.Both;

    /// <summary>Initializes a new instance of <see cref="ViolinSeries"/> with the specified datasets.</summary>
    /// <param name="datasets">An array of datasets, each containing the values for one violin.</param>
    public ViolinSeries(double[][] datasets) : base(datasets) { }

    /// <summary>The bodies the last estimate made, with what they were made from.</summary>
    [RenderCache]
    private KeptEstimate<ViolinBody[]>? _bodies;

    [RenderCache]
    private DataSnapshot? _dataCopy;

    ref DataSnapshot? IKeepsDataCopy.DataCopy => ref _dataCopy;

    /// <summary>The violins as their data makes them under <paramref name="looks"/>, one per dataset in order: the sample each is drawn
    /// from and the outline of its body (<see cref="ViolinBody"/>), smoothed by the theme's density look
    /// (<see cref="DensityLook.BandwidthFactor"/>: Silverman's rule under the library's themes, Scott's, as matplotlib's
    /// <c>violinplot</c>, under the matplotlib themes) on <see cref="Models.Series.ViolinSeries.Heights"/> heights. The range of the axes and
    /// the renderer both read them, so what is drawn and what is ranged over are one estimate. They are estimated once and kept until a
    /// setting or a value of <see cref="DatasetSeries.Datasets"/> changes or they are asked under another rule
    /// (<see cref="KeptEstimate{T}"/>), so neither the second reader nor a figure drawn again estimates a density twice.</summary>
    /// <param name="looks">The looks of the theme the violins are drawn under.</param>
    /// <returns>One body per dataset; the caller reads it and never writes into it.</returns>
    internal ViolinBody[] BodiesUnder(ThemeLooks looks) =>
        KeptEstimate<ViolinBody[]>.Of(ref _bodies, this, new DensityRule(looks.Density.BandwidthFactor, Heights),
            static (series, rule) => series.EstimateBodies(rule));

    /// <summary>Estimates every violin's body under <paramref name="rule"/>: its drawable values sorted, and the density of that
    /// sample at the series' width.</summary>
    private ViolinBody[] EstimateBodies(DensityRule rule)
    {
        var bodies = new ViolinBody[Datasets.Length];
        for (int i = 0; i < bodies.Length; i++)
        {
            var sample = Datasets[i].SortedDrawableValues();
            bodies[i] = new ViolinBody(sample, sample.Length == 0 ? [] : Outline(sample, Widths, rule));
        }

        return bodies;
    }

    /// <summary>How far the extrema ticks reach to either side of a violin's position under <paramref name="look"/>, in data units: the
    /// look's share of the width (<see cref="ViolinLook.ExtremaReach"/>).</summary>
    /// <param name="look">The violin look of the theme the series is drawn under.</param>
    /// <returns>The reach.</returns>
    internal double ExtremaHalfWidthUnder(ViolinLook look) => Widths * look.ExtremaReach;

    /// <summary>How far the median and mean lines reach to either side of a violin's position under <paramref name="look"/>, in data
    /// units: the look's share of the width (<see cref="ViolinLook.StatsReach"/>).</summary>
    /// <param name="look">The violin look of the theme the series is drawn under.</param>
    /// <returns>The reach.</returns>
    internal double StatsHalfWidthUnder(ViolinLook look) => Widths * look.StatsReach;

    /// <summary>Where the violin of the dataset at <paramref name="index"/> stands under <paramref name="theme"/>: the position the series
    /// was given for it, otherwise its index from the theme's first position on (<see cref="ViolinLook.Datasets"/>).</summary>
    /// <param name="index">The index of the dataset.</param>
    /// <param name="theme">The theme the series is drawn under.</param>
    /// <returns>The position along X.</returns>
    internal double PositionIn(int index, Theme theme) => Positions.PositionOf(index, theme.Looks.Violin.Datasets);

    /// <summary>How far the body reaches left of its position where it is <paramref name="halfWidth"/> wide: not at all for a
    /// body drawn on its high side only.</summary>
    /// <param name="halfWidth">The width of the body to either side of its position at one height.</param>
    /// <returns>The reach to the left.</returns>
    internal double BodyLeft(double halfWidth) => Side == ViolinSide.High ? 0 : halfWidth;

    /// <summary>How far the body reaches right of its position where it is <paramref name="halfWidth"/> wide: not at all for
    /// a body drawn on its low side only.</summary>
    /// <param name="halfWidth">The width of the body to either side of its position at one height.</param>
    /// <returns>The reach to the right.</returns>
    internal double BodyRight(double halfWidth) => Side == ViolinSide.Low ? 0 : halfWidth;

    /// <summary>The interval a violin whose body has <paramref name="outline"/> covers around its position, in data units: the
    /// widest of its body (one side only for a half violin), its extrema ticks and its median and mean lines, each to the
    /// side it is drawn on. The renderer reads the same body outline and the same widths, so the range never lies about
    /// what is drawn.</summary>
    /// <param name="outline">The outline of the body (<see cref="ViolinBody.Outline"/>); at least one height.</param>
    /// <param name="look">The violin look of the theme the series is drawn under: how far its lines reach.</param>
    /// <returns>An interval around 0: <see cref="Range1D.Lo"/> is the reach to the left, negated.</returns>
    internal Range1D ReachAround(ViolinSlice[] outline, ViolinLook look)
    {
        double body = outline.Max(slice => slice.HalfWidth) * look.BodyReach;
        double marks = Math.Max(ShowExtrema ? ExtremaHalfWidthUnder(look) : 0, ShowMedians || ShowMeans ? StatsHalfWidthUnder(look) : 0);
        return new(-Math.Max(BodyLeft(body), marks), Math.Max(BodyRight(body), marks));
    }

    /// <inheritdoc />
    /// <remarks>As the library's own themes range it (the range under the library's own look).</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => RangeOver(context, ThemeLooks.Library);

    /// <inheritdoc />
    internal override bool RangesItsPositionsWhenEmpty => true;

    /// <inheritdoc />
    DataRangeContribution IThemedRange.RangeUnder(IAxesContext context, Theme theme) => RangeOver(context, theme.Looks);

    /// <summary>The range the violins ask of their axes under <paramref name="looks"/>. Along X, the room each violin is drawn in around
    /// its position (<see cref="ReachAround"/>), over the bodies the theme draws (<see cref="BodiesUnder"/>). A series that sets
    /// <see cref="Positions"/> ranges over those places alone; one that does not keeps the room the violin look keeps past the outer
    /// violins (<see cref="ViolinLook.RoomPastTheViolins"/>: one unit under the library's own themes, none under the matplotlib themes,
    /// whose axis covers the bodies as <c>violinplot</c>'s does), widened for a violin wider than that. A limit set on the X axis is the
    /// range.</summary>
    /// <param name="context">The axes the series is drawn on.</param>
    /// <param name="looks">The looks of the theme.</param>
    /// <returns>The contribution.</returns>
    private DataRangeContribution RangeOver(IAxesContext context, ThemeLooks looks)
    {
        var look = looks.Violin;
        double first = look.Datasets.FirstPosition;
        Range1D? room = look.RoomPastTheViolins is { } past ? new Range1D(first - past, first + Datasets.Length - 1 + past) : null;
        var x = DrawnAround(BodiesUnder(looks), look).CoverAlongPositions(Positions, room);
        // Over the values the Y axis can place: a missing sample (NaN) and an infinity are not part of the extent, and on a
        // log axis neither is 0 or below (the body is clipped to the plot's edge there), so the range starts at the lowest
        // value it can place.
        var y = Datasets.DrawableRange(context.YScale.Rule);
        return new(context.XAxisMin ?? x?.Lo, context.XAxisMax ?? x?.Hi, y?.Lo, y?.Hi);
    }

    /// <summary>What each of <paramref name="bodies"/> that can be drawn covers under <paramref name="look"/>, at the position it stands
    /// at.</summary>
    private IEnumerable<Range1D> DrawnAround(ViolinBody[] bodies, ViolinLook look)
    {
        for (int i = 0; i < bodies.Length; i++)
        {
            if (bodies[i].Sample.Length == 0) continue;
            double at = Positions.PositionOf(i, look.Datasets);
            var reach = ReachAround(bodies[i].Outline, look);
            yield return new Range1D(at + reach.Lo, at + reach.Hi);
        }
    }

    static string ISeriesKind<ViolinSeries>.Kind => "violin";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(), Datasets = Datasets,
        Color = Color,
        Alpha = Alpha.OrNullAtRest(RestingAlpha),
        ShowMeans = ShowMeans.OrNullAtRest(false),
        ShowMedians = ShowMedians.OrNullAtRest(false),
        ShowExtrema = ShowExtrema.OrNullAtRest(RestingShowExtrema),
        Positions = Positions,
        Widths = Widths.OrNullAtRest(RestingWidths),
        Side = Side.OrNullAtRest(RestingSide),
    };

    /// <summary>The reader of a <see cref="ViolinSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static ViolinSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Violin(dto.Datasets ?? []);
        s.Color = dto.Color;
        if (dto.Alpha is { } alpha) s.Alpha = alpha;
        if (dto.ShowMeans is { } showMeans) s.ShowMeans = showMeans;
        if (dto.ShowMedians is { } showMedians) s.ShowMedians = showMedians;
        if (dto.ShowExtrema is { } showExtrema) s.ShowExtrema = showExtrema;
        s.Positions = dto.Positions;
        if (dto.Widths is { } widths) s.Widths = widths;
        if (dto.Side is { } side) s.Side = side;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);

    /// <summary>The number of heights the outline stands at, matplotlib's <c>violinplot</c> <c>points=100</c>: evenly from the
    /// lowest value to the highest.</summary>
    internal const int Heights = 100;

    /// <summary>The outline of the body of a violin, from the lowest value to the highest: the Gaussian kernel density of the
    /// sample at the rule's heights, with the rule's bandwidth (<see cref="GaussianKde"/>, the estimator the density curve and
    /// the ridgeline are drawn with), scaled so the widest point of the body is <paramref name="widths"/>. A sample without a
    /// spread (one observation, or every value the same) has no density: its body is a spike, the whole width at its value at
    /// every height, as matplotlib draws it.</summary>
    /// <param name="widths">The series' <see cref="ViolinSeries.Widths"/>: the half-width of the body at its widest.</param>
    /// <param name="rule">The bandwidth factor of the theme and the number of heights (<see cref="Models.Series.ViolinSeries.Heights"/>).</param>
    /// <returns>The outline at the rule's heights, lowest first.</returns>
    /// <param name="sorted">The value it works on.</param>
    internal static ViolinSlice[] Outline(double[] sorted, double widths, DensityRule rule)
    {
        double min = sorted[0], max = sorted[^1];
        if (min == max) return [.. Enumerable.Repeat(new ViolinSlice(min, widths), rule.Points)];

        // matplotlib's violinplot: np.linspace(min, max, points).
        double[] heights = new EqualWidthBins(min, max, rule.Points - 1).Edges();
        double[] density = GaussianKde.DensityAt(sorted, GaussianKde.Bandwidth(sorted, rule.BandwidthFactor), heights);
        double peak = density.Max();
        return [.. heights.Select((y, i) => new ViolinSlice(y, density[i] / peak * widths))];
    }
}
