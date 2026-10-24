// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet;

/// <summary>Charts estimated from samples: one sample comes in as a <c>double[]</c>, groups compared with each other each
/// as a <see cref="NamedSample"/>, of whatever length it has, and survival data as a <see cref="SurvivalGroup"/> per group;
/// the chart draws what is estimated from them. Every verb here composes series the library already draws and sets no axis
/// limits, because a limit clips every series on the axes and overwrites the caller's own. A missing observation (NaN) and
/// an infinity are left out of a sample; a survival duration is refused instead when it is missing, because a subject
/// without a time cannot be counted among those at risk.</summary>
public static class DistributionAxesExtensions
{
    /// <summary>Draws a ridgeline chart (a joyplot): the density of each group as a filled ridge on a baseline of its own,
    /// the first group at the top, with the groups named on the Y axis, so many distributions can be compared at a glance.
    /// <para>Every ridge is estimated the same way, so the ridges compare with each other. They stand on ONE shared grid of
    /// 200 points, from the smallest observation of all the groups minus three bandwidths to the largest plus three, and are
    /// estimated with ONE bandwidth: <paramref name="bandwidth"/> when given, otherwise the mean of the groups' own Silverman
    /// bandwidths (ggridges' joint bandwidth). The shift between the groups is not counted as spread, so groups far apart
    /// are not smoothed into each other; a group narrower than the others is smoothed to the shared width. A group whose
    /// observations are all the same value has no width to give and is left out of that mean; when no group spreads, the
    /// bandwidth is Silverman's rule over all the observations, which takes the size of the value as the spread when they are
    /// all the same.</para>
    /// <para>The ridges are drawn to ONE height scale: the tallest ridge reaches the next baseline, and every other ridge
    /// keeps its height relative to it. So no ridge runs into the one above it, and the heights do not depend on the data's
    /// units. Each ridge stands on the centre of a slot of the Y axis, one slot per group, the first group on the highest;
    /// the lower ridges are drawn in front.</para>
    /// <para>Each ridge is ONE <see cref="AreaSeries"/>, filled from its baseline up to its density, which draws its own
    /// outline along the top, so a ridge wears one colour of the theme's cycle. The ridges carry no legend label: their
    /// names stand on the Y axis. A missing observation (NaN) and an infinity are left out of the estimate. Near the ends of
    /// the grid a ridge stops a little above its baseline, three bandwidths past the outermost observation, where a single
    /// observation's kernel has fallen to 1.1 % of its peak.</para>
    /// <para>The chart is its parts: written to JSON it is one area series per ridge with the names as the Y axis' ticks, and
    /// read back it draws the same picture. It is not estimated again; change the samples and call the verb again.</para></summary>
    /// <param name="axes">The axes the chart is added to.</param>
    /// <param name="groups">The groups, in the order they are drawn from the top down; each with at least two observations
    /// that can be drawn.</param>
    /// <param name="bandwidth">The width of the Gaussian kernel, in the units of the observations, the same for every ridge;
    /// <see langword="null"/> (the default) for the mean of the groups' own Silverman bandwidths (ggridges' joint
    /// bandwidth), which does not count the shift between the groups as spread.</param>
    /// <param name="configure">Optional styling, run once for each ridge, in the order of the groups, after the verb has
    /// built it: its colour, transparency, outline, legend label.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="axes"/> or <paramref name="groups"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="bandwidth"/> is not a finite number above 0.</exception>
    /// <exception cref="ArgumentException">No groups are given; a group carries no values, or fewer than two observations
    /// that can be drawn (the message names the group); or the observations spread so far, or the bandwidth is so far out of
    /// scale, that the density cannot be computed in double precision.</exception>
    /// <example>
    /// <code>
    /// NamedSample[] months =
    /// [
    ///     new("January", [2.1, 3.4, 1.8, 0.9, 2.6, 3.0, 1.2]),
    ///     new("April", [9.8, 12.1, 11.4, 8.7, 10.9, 13.2]),
    ///     new("July", [21.5, 24.0, 19.8, 23.1, 22.7, 25.3, 20.4, 22.0]),
    /// ];
    ///
    /// string svg = Plt.Create()
    ///     .WithTitle("Daily highs")
    ///     .AddSubPlot(1, 1, 1, ax => ax
    ///         .Ridgeline(months, configure: ridge => ridge.Alpha = 0.6)
    ///         .SetXLabel("Temperature (°C)"))
    ///     .ToSvg();
    /// </code>
    /// </example>
    public static AxesBuilder Ridgeline(this AxesBuilder axes, IReadOnlyList<NamedSample> groups, double? bandwidth = null,
        Action<AreaSeries>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(axes);
        ArgumentNullException.ThrowIfNull(groups);
        if (groups.Count == 0)
        {
            throw new ArgumentException("No groups were given; a ridgeline draws one ridge per group.", nameof(groups));
        }

        if (bandwidth is { } given && !(given > 0 && double.IsFinite(given)))
        {
            throw new ArgumentOutOfRangeException(nameof(bandwidth), given, "A bandwidth is a width: a finite number above 0.");
        }

        double[][] observations = [.. groups.Select(g => g.SortedObservations(DensityObservations, nameof(groups)))];
        double[] pooled = [.. observations.SelectMany(o => o).Order()];
        double h = bandwidth ?? GaussianKde.JointBandwidth(observations);
        double[] grid = GaussianKde.Grid(pooled, h, RidgePoints);
        double[][] densities = [.. observations.Select(o => GaussianKde.DensityAt(o, h, grid))];
        double tallest = densities.Max(d => d.Max());

        // The grid, the bandwidth and the densities are finite for every sample a double can spread over; observations
        // from -1e308 to 1e308 have no finite spread, and a bandwidth of 5e-324 no finite density.
        if (!double.IsFinite(h) || !double.IsFinite(grid[0]) || !double.IsFinite(grid[^1]) || !(tallest > 0 && double.IsFinite(tallest)))
        {
            throw new ArgumentException(
                "The density of these observations cannot be computed in double precision: their spread, the bandwidth or the density is beyond what a double holds.",
                bandwidth is null ? nameof(groups) : nameof(bandwidth));
        }

        int count = groups.Count;
        for (int g = 0; g < count; g++)
        {
            // The first group on the highest slot. Each ridge rises from its baseline by its density over the tallest
            // density of all, times the distance to the next baseline (one slot), so the tallest ridge reaches it exactly.
            var slot = CategorySlot.Of(count - 1 - g);
            double baseline = slot.Centre, spacing = slot.Right - slot.Left;
            double[] top = [.. densities[g].Select(d => baseline + d / tallest * spacing)];
            var ridge = new AreaSeries([.. grid], top) { YData2 = [.. grid.Select(_ => baseline)] };

            // Added in the order of the groups, all at the area's resting z-order: the lower ridges are drawn later, in front.
            axes.AddSeries(ridge, configure);
        }

        return axes.SetYCategoryTicks([.. groups.Select(g => g.Label)], topDown: true);
    }

    /// <summary>Draws a Q-Q plot (a normal probability plot): the sample sorted up the Y axis against the quantiles the
    /// standard normal distribution has at the same ranks along the X axis, with a reference line, so a reader sees at a glance
    /// how the sample differs from a normal one. On the line it is normal; an S shape means tails lighter or heavier than
    /// normal; a bow means it is skewed.
    /// <para>The conventions are scipy's (<c>scipy.stats.probplot</c>). The k-th smallest of n observations stands at the
    /// standard normal quantile of Filliben's plotting position for rank k: the median of the k-th smallest of n uniform
    /// observations, 0.5^(1/n) for the last, one minus that for the first, and (k - 0.3175) / (n + 0.365) for the others. The
    /// reference is the least-squares line through the points, drawn from the first theoretical quantile to the last; its
    /// slope estimates the sample's standard deviation and its height at 0 the mean. (R's <c>qqnorm</c> and <c>qqline</c>
    /// differ: other plotting positions, and a line through the quartiles.)</para>
    /// <para>The points are ONE <see cref="ScatterSeries"/> wearing the theme's colour. The reference is a
    /// <see cref="Trendline"/>, a solid red line 1.5 points wide as probplot draws it; it takes no colour slot and has no
    /// legend entry, and where it runs past the range of the points it is cut at the edge of the plot. The axes are titled
    /// "Theoretical quantiles" and "Sample quantiles"; a title of your own set after the call replaces either.</para>
    /// <para>The sample is compared with the normal distribution only; a plot against other distributions is not drawn yet.
    /// A missing observation (NaN) and an infinity are left out, so the plot is that of the other observations. The verb sets
    /// no axis limits.</para>
    /// <para>The chart is its parts: written to JSON it is a scatter series and a trendline, and read back it draws the same
    /// picture. It is not fitted again; change the sample and call the verb again.</para></summary>
    /// <param name="axes">The axes the chart is added to.</param>
    /// <param name="sample">The observations, in any order; at least two that can be drawn.</param>
    /// <param name="configure">Optional styling of the two parts, run once after the verb has built and styled them, so what
    /// it sets wins: the points' colour, marker, transparency and legend label; the reference line's colour, width, style,
    /// or <see cref="Trendline.IsExtended"/> to run it across the whole plot.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="axes"/> or <paramref name="sample"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The sample has fewer than two observations that can be drawn.</exception>
    /// <example>
    /// <code>
    /// double[] returns = [0.8, -1.2, 0.3, 2.1, -0.4, 0.9, -2.6, 0.1, 1.4, -0.7, 0.5, -0.2];
    ///
    /// string svg = Plt.Create()
    ///     .WithTitle("Daily returns against the normal distribution")
    ///     .AddSubPlot(1, 1, 1, ax => ax
    ///         .QQPlot(returns, parts => parts.Points.Alpha = 0.7)
    ///         .SetYLabel("Daily return (%)"))
    ///     .ToSvg();
    /// </code>
    /// </example>
    public static AxesBuilder QQPlot(this AxesBuilder axes, double[] sample, Action<QQPlotParts>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(axes);
        ArgumentNullException.ThrowIfNull(sample);
        double[] ordered = sample.SortedObservations(LineObservations, "The sample", "a Q-Q plot is drawn from", nameof(sample));

        // The k-th smallest observation against the normal quantile of its rank, and the least-squares line through them
        // (scipy's linregress of osr on osm), evaluated at the first and the last quantile.
        double[] theoretical = [.. FillibenPositions(ordered.Length).Select(StandardNormal.Quantile)];
        double[] fit = LeastSquares.PolyFit(theoretical, ordered, 1);
        double[] ends = LeastSquares.PolyEval(fit, [theoretical[0], theoretical[^1]]);

        var points = new ScatterSeries(theoretical, ordered);
        Trendline reference = null!;
        axes.AddSeries(points)
            .AddTrendline(theoretical[0], ends[0], theoretical[^1], ends[1], t =>
            {
                reference = t;
                t.Color = ReferenceColor; t.LineWidth = ReferenceLineWidth;
            })
            .SetXLabel(TheoreticalQuantilesTitle)
            .SetYLabel(SampleQuantilesTitle);

        configure?.Invoke(new QQPlotParts(points, reference));
        return axes;
    }

    /// <summary>Draws a Kaplan-Meier chart: for each group the estimated probability of surviving past each time (of not
    /// yet having had the event), as a curve that starts at 1 and steps down at every event, so groups can be compared over
    /// the whole follow-up.
    /// <para>Each curve is the product-limit estimate of its group: at each time t_i at which d_i events happen while n_i
    /// subjects are at risk (those followed to t_i or longer), the curve drops to its level times (1 - d_i / n_i). All the
    /// events at one time are one step, and a subject censored at an event time still counts as at risk at it (events
    /// before censorings, as R's survival package and lifelines count them). A censored subject causes no step; it leaves
    /// the risk set after its time. The curve starts at (0, 1) and, when the last follow-up is a censoring, runs on at its
    /// last level to that time. The arithmetic is fractions of whole numbers, with no logarithm or exponential, so the levels
    /// are the same on every platform.</para>
    /// <para>Each curve is ONE <see cref="LineSeries"/> drawn in steps after its points (<see cref="DrawStyle.StepsPost"/>),
    /// with no markers, wearing one colour of the theme's cycle, the group's name as its legend label. Around it stands a
    /// Greenwood confidence band (an <see cref="AreaSeries"/>) and, at every time a subject left without an event, a censor
    /// mark (a <see cref="ScatterSeries"/>, <see cref="MarkerStyle.Plus"/>, at the curve's level there) — a group with no
    /// censored subject has no marks series. The curve, its band and its marks share one colour group: all three wear the
    /// curve's colour, and only the curve carries a legend entry. The Y axis is titled "Survival probability"; a title of
    /// your own set after the call replaces it, and the X axis, whose unit is the durations', is left to you. The verb sets
    /// no axis limits.</para>
    /// <para>The band is S(t) ± 1.96 standard errors, Greenwood's: SE(t)² = S(t)² x the running sum, over every event time
    /// at or before t, of d_i / (n_i x (n_i - d_i)), clamped to [0, 1]. This is the PLAIN form, not R's default log or
    /// log-log transform: those read better near 0 and 1 but need a logarithm and an exponential, and <see cref="Math.Log(double)"/>
    /// and <see cref="Math.Exp"/> are the one platform-dependent bit the rest of this estimator has none of (R's survival
    /// package documents all three forms; <c>conf.type = "plain"</c> is this one). The only transcendental this band costs is
    /// <see cref="Math.Sqrt"/>, which every conformant platform rounds to the same bit (IEEE 754), so the band is the same
    /// on every platform too.</para>
    /// <para>No risk table and no log-rank test: a risk table is a table of counts laid out under the plot, and a log-rank
    /// test is a statistic to compute, neither a mark to draw.</para>
    /// <para>The chart is its parts: written to JSON it is the curve, band and marks of each group, and read back it draws
    /// the same picture. It is not estimated again; change the groups and call the verb again.</para></summary>
    /// <param name="axes">The axes the chart is added to.</param>
    /// <param name="groups">The groups, in the order they take colours; each with at least one subject, a finite duration
    /// at or above 0 and a flag for every subject.</param>
    /// <param name="configure">Optional styling of the CURVE only, run once for each one, in the order of the groups, after
    /// the verb has built it, so what it sets wins: its colour, width, style or legend label. The band and the censor
    /// marks always wear the curve's own colour and are not separately stylable in this release: every chart verb this
    /// library ships takes exactly one <c>configure</c> callback, so reaching the band and the marks too would need
    /// either a breaking change here (replacing <see cref="LineSeries"/> with a parts type every existing caller would
    /// have to rewrite for) or a second overload of the same name, which the verb-shape rule refuses as well (one verb,
    /// one shape).</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="axes"/> or <paramref name="groups"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">No groups are given; or a group carries no durations or no flags, its durations
    /// and flags differ in number, it has no subjects, or a duration is missing (NaN), negative or infinite (the message
    /// names the group).</exception>
    /// <example>
    /// <code>
    /// using MatPlotLibNet.Models.Series;
    ///
    /// SurvivalGroup[] arms =
    /// [
    ///     new("Treatment", [6, 7, 10, 15, 19, 25, 32, 34], [true, true, false, true, false, true, false, false]),
    ///     new("Placebo", [1, 2, 3, 4, 5, 8, 8, 11], [true, true, true, false, true, true, true, true]),
    /// ];
    ///
    /// string svg = Plt.Create()
    ///     .WithTitle("Time to relapse")
    ///     .AddSubPlot(1, 1, 1, ax => ax
    ///         .KaplanMeier(arms)
    ///         .SetXLabel("Weeks"))
    ///     .ToSvg();
    /// </code>
    /// </example>
    public static AxesBuilder KaplanMeier(this AxesBuilder axes, IReadOnlyList<SurvivalGroup> groups, Action<LineSeries>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(axes);
        ArgumentNullException.ThrowIfNull(groups);
        if (groups.Count == 0)
        {
            throw new ArgumentException("No groups were given; a Kaplan-Meier chart draws one curve per group.", nameof(groups));
        }

        // Every group is estimated before the first curve is added, so a refused call leaves the axes as it was.
        SurvivalCurve[] curves = [.. groups.Select(g => g.ProductLimit(nameof(groups)))];
        for (int g = 0; g < curves.Length; g++)
        {
            int colorGroup = axes.NextColorGroup();

            var curve = new LineSeries(curves[g].Times, curves[g].Survival) { DrawStyle = DrawStyle.StepsPost, Label = groups[g].Label };
            curve.ColorGroup = colorGroup;
            axes.AddSeries(curve, configure);

            var (lower, upper) = GreenwoodBand(curves[g]);
            var band = new AreaSeries(curves[g].Times, upper) { YData2 = lower, Alpha = 0.2 };
            band.ColorGroup = colorGroup;
            axes.AddSeries(band);

            double[] censoringTimes = CensoringTimesOf(groups[g]);
            if (censoringTimes.Length > 0)
            {
                double[] marksY = [.. censoringTimes.Select(t => LevelAt(curves[g], t))];
                var marks = new ScatterSeries(censoringTimes, marksY) { Marker = MarkerStyle.Plus };
                marks.ColorGroup = colorGroup;
                axes.AddSeries(marks);
            }
        }

        return axes.SetYLabel(SurvivalProbabilityTitle);
    }

    /// <summary>The standard normal distribution's 97.5th percentile: a two-sided 95 % confidence band is the estimate
    /// plus and minus this many standard errors.</summary>
    private const double Z95 = 1.959963984540054;

    /// <summary>Greenwood's confidence band around <paramref name="curve"/>, PLAIN form (<see cref="KaplanMeier(AxesBuilder, IReadOnlyList{SurvivalGroup}, Action{LineSeries})"/>'s
    /// own doc explains why not the log or log-log transform): the variance accumulates only at a point with an event
    /// (<see cref="SurvivalCurve.Events"/> above 0) and only while every subject at risk there did not all have the event
    /// (dividing by n_i - d_i = 0 is undefined; a level of exactly 0 already has a band of [0, 0], so nothing is lost by
    /// stopping the sum there).</summary>
    /// <param name="curve">The curve the band surrounds.</param>
    /// <returns>The lower and the upper bound, one pair per point of <paramref name="curve"/>, each clamped to [0, 1].</returns>
    private static (double[] Lower, double[] Upper) GreenwoodBand(SurvivalCurve curve)
    {
        int n = curve.Times.Length;
        var lower = new double[n];
        var upper = new double[n];
        double cumulative = 0;
        for (int i = 0; i < n; i++)
        {
            if (curve.Events[i] > 0 && curve.AtRisk[i] > curve.Events[i])
            {
                cumulative += (double)curve.Events[i] / (curve.AtRisk[i] * (curve.AtRisk[i] - curve.Events[i]));
            }

            double s = curve.Survival[i];
            double se = Math.Sqrt(s * s * cumulative);
            lower[i] = Math.Clamp(s - Z95 * se, 0, 1);
            upper[i] = Math.Clamp(s + Z95 * se, 0, 1);
        }

        return (lower, upper);
    }

    /// <summary>The times <paramref name="group"/>'s subjects left without an event (<see cref="SurvivalGroup.Observed"/> is
    /// <see langword="false"/>), ascending.</summary>
    private static double[] CensoringTimesOf(SurvivalGroup group) =>
        [.. group.Durations.Where((_, i) => !group.Observed[i]).Order()];

    /// <summary>The level <paramref name="curve"/> is drawn at AT <paramref name="time"/>: the survival of the last point
    /// at or before it, read "steps-post" exactly as the curve itself is drawn (a level holds from its point to the
    /// next).</summary>
    private static double LevelAt(SurvivalCurve curve, double time)
    {
        double level = curve.Survival[0];
        for (int i = 0; i < curve.Times.Length && curve.Times[i] <= time; i++)
        {
            level = curve.Survival[i];
        }

        return level;
    }

    /// <summary>Draws a raincloud chart: a half violin, a narrow box and the raw observations of each group, side by
    /// side at the group's own index, all three in one colour from the theme's cycle, so the three read as one group's
    /// three views of its own distribution.
    /// <para>A multi-dataset <see cref="ViolinSeries"/> and <see cref="BoxSeries"/> already draw one shape per group on
    /// their own (one position per dataset, left at the dataset's own index), so this is ONE violin and ONE box for the
    /// whole chart, not one pair per group — and ONE <see cref="ScatterSeries"/> carries every group's raw points
    /// together, jittered a FIXED, deterministic amount so two runs draw the same figure: a point at index i of its
    /// group, standing at the group's own index g, is drawn at x = g + 0.35 + (i mod 4) x 0.03, a four-step cycle that
    /// spreads the points clear of the violin's own half-width (0.3) and its whisker caps, so neither hides a point
    /// nor a point a probe of them.</para>
    /// <para>Only the violin carries a label ("Samples"): the violin, box and points together stand for one group of
    /// three views sharing one colour per group, so one legend entry names the group as a whole rather than one
    /// swatch per group's many series. The groups themselves are named along the X axis, which every reader already
    /// reads positionally. The verb sets no axis limits.</para>
    /// <para>The chart is its parts: written to JSON it is the violin, the box and the points, and read back it draws
    /// the same picture. It is not estimated again; change the groups and call the verb again.</para></summary>
    /// <param name="axes">The axes the chart is added to.</param>
    /// <param name="groups">The groups, in the order they stand along the X axis and take their colour; each with one
    /// or more observations.</param>
    /// <param name="configure">Optional styling of all three parts together, run once after the verb has built them, so
    /// what it sets wins.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="axes"/> or <paramref name="groups"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">No groups are given.</exception>
    /// <example>
    /// <code>
    /// using MatPlotLibNet.Models.Series;
    ///
    /// NamedSample[] groups =
    /// [
    ///     new("Control", [4.1, 4.8, 5.0, 5.3, 5.5, 5.9, 6.2, 6.4, 6.8, 7.1, 7.7, 8.4]),
    ///     new("Treated", [5.6, 6.1, 6.5, 6.9, 7.2, 7.4, 7.9, 8.3, 8.8, 9.1, 9.6, 10.2]),
    /// ];
    ///
    /// string svg = Plt.Create()
    ///     .AddSubPlot(1, 1, 1, ax => ax.Raincloud(groups))
    ///     .ToSvg();
    /// </code>
    /// </example>
    public static AxesBuilder Raincloud(this AxesBuilder axes, IReadOnlyList<NamedSample> groups, Action<RaincloudParts>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(axes);
        ArgumentNullException.ThrowIfNull(groups);
        if (groups.Count == 0)
        {
            throw new ArgumentException("No groups were given; a raincloud chart draws one violin, one box and one scatter per set of groups.", nameof(groups));
        }

        double[][] samples = [.. groups.Select(g => g.Values)];
        double[] pointX = [.. samples.SelectMany((s, g) => s.Select((_, i) => g + 0.35 + (i % 4) * 0.03))];
        double[] pointY = [.. samples.SelectMany(s => s)];

        int colorGroup = axes.NextColorGroup();
        var violin = new ViolinSeries(samples) { Side = ViolinSide.High, Alpha = 0.5, Label = "Samples" };
        violin.ColorGroup = colorGroup;
        axes.AddSeries(violin);

        var box = new BoxSeries(samples) { Widths = 0.1, ShowOutliers = false };
        box.ColorGroup = colorGroup;
        axes.AddSeries(box);

        var points = new ScatterSeries(pointX, pointY) { MarkerSize = 6, Alpha = 1 };
        points.ColorGroup = colorGroup;
        axes.AddSeries(points);

        configure?.Invoke(new RaincloudParts(violin, box, points));

        // One tick per group AT ITS DATASET INDEX, not the bar-style half-slot SetXCategoryTicks places a name at:
        // the violin and the box already stand at their dataset's own index (the default a multi-dataset series takes
        // when it is given no Positions), so the name belongs on that same number, not half a slot further on.
        double[] positions = [.. Enumerable.Range(0, groups.Count).Select(k => (double)k)];
        return axes
            .SetXTickLocator(new Rendering.TickLocators.FixedLocator(positions))
            .SetXTickFormatter(new Rendering.TickFormatters.CategoryFormatter([.. groups.Select(g => g.Label)], positions));
    }

    /// <summary>The title of a Kaplan-Meier chart's Y axis.</summary>
    private const string SurvivalProbabilityTitle = "Survival probability";

    /// <summary>Filliben's plotting positions for <paramref name="count"/> observations (Filliben 1975, scipy's
    /// <c>_calc_uniform_order_statistic_medians</c>): the median of the k-th smallest of <paramref name="count"/> uniform
    /// observations on (0, 1), in ascending order. The last is 0.5^(1/n), the first one minus that, and the ones between
    /// (k - 0.3175) / (n + 0.365) for rank k; they are symmetric around 0.5.</summary>
    /// <param name="count">The number of observations, two or more.</param>
    private static double[] FillibenPositions(int count)
    {
        var positions = new double[count];
        positions[^1] = Math.Pow(0.5, 1.0 / count);
        positions[0] = 1 - positions[^1];
        for (int rank = 2; rank < count; rank++)
        {
            positions[rank - 1] = (rank - 0.3175) / (count + 0.365);
        }

        return positions;
    }

    /// <summary>How many observations a Q-Q plot is drawn from at the least: a reference line is fitted through two points
    /// or more.</summary>
    private const int LineObservations = 2;

    /// <summary>The colour of a Q-Q plot's reference line: red, as scipy's probplot draws it (<c>'r-'</c>).</summary>
    private static readonly Color ReferenceColor = Colors.Red;

    /// <summary>The width of a Q-Q plot's reference line, in points: matplotlib's line width.</summary>
    private const double ReferenceLineWidth = 1.5;

    /// <summary>The title of a Q-Q plot's X axis.</summary>
    private const string TheoreticalQuantilesTitle = "Theoretical quantiles";

    /// <summary>The title of a Q-Q plot's Y axis.</summary>
    private const string SampleQuantilesTitle = "Sample quantiles";

    /// <summary>How many observations a density is estimated from at the least: with one there is no spread to estimate a
    /// bandwidth from, and a single kernel says nothing about the shape of the group.</summary>
    private const int DensityObservations = 2;

    /// <summary>The number of points of a ridgeline's shared grid: seaborn's grid for a density curve, fine enough that a
    /// ridge drawn through them strays from the true density by less than a pixel at the usual sizes.</summary>
    private const int RidgePoints = 200;
}
