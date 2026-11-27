// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet;

/// <summary>Charts estimated from samples: one sample comes in as a <c>double[]</c>, groups compared with each other each
/// as a <see cref="NamedSample"/>, of whatever length it has, and survival data as a <see cref="SurvivalGroup"/> per group;
/// the chart draws what is estimated from them. Every verb here composes series the library already draws and sets no axis
/// limits, because a limit clips every series on the axes and overwrites the caller's own. A missing observation (NaN) and
/// an infinity are left out of a sample; a survival duration is refused instead when it is missing, because a subject
/// without a time cannot be counted among those at risk.</summary>
public static partial class DistributionAxesExtensions
{
    extension(AxesBuilder axes)
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
        /// <param name="groups">The groups, in the order they are drawn from the top down; each with at least two observations
        /// that can be drawn.</param>
        /// <param name="bandwidth">The width of the Gaussian kernel, in the units of the observations, the same for every ridge;
        /// <see langword="null"/> (the default) for the mean of the groups' own Silverman bandwidths (ggridges' joint
        /// bandwidth), which does not count the shift between the groups as spread.</param>
        /// <param name="configure">Optional styling, run once for each ridge, in the order of the groups, after the verb has
        /// built it: its colour, transparency, outline, legend label.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="groups"/> is
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
        public AxesBuilder Ridgeline(IReadOnlyList<NamedSample> groups, double? bandwidth = null,
            Action<AreaSeries>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);
            ArgumentNullException.ThrowIfNull(groups);
            groups.ThrowIfEmpty("No groups were given; a ridgeline draws one ridge per group.");

            bandwidth?.ThrowIfNotFinite(nameof(bandwidth)).ThrowIfNotAbove(0, nameof(bandwidth));
            double[][] observations = [.. groups.Select(g => g.SortedObservations(DensityObservations, nameof(groups)))];
            double h = bandwidth ?? GaussianKde.JointBandwidth(observations);

            // Each group is sorted, so the smallest observation of all is the first of some group and the largest the last of
            // some group: the shared grid needs no pooled copy of every observation sorted again.
            double min = observations[0][0], max = observations[0][^1];
            foreach (double[] group in observations)
            {
                min = Math.Min(min, group[0]);
                max = Math.Max(max, group[^1]);
            }

            double[] grid = GaussianKde.Grid(min, max, h, RidgePoints);
            double[][] densities = GaussianKde.JointDensities(observations, h, grid);
            double tallest = densities.Max(d => d.Max());

            // The grid, the bandwidth and the densities are finite for every sample a double can spread over; observations
            // from -1e308 to 1e308 have no finite spread, and a bandwidth of 5e-324 no finite density.
            string from = bandwidth is null ? nameof(groups) : nameof(bandwidth);
            h.ThrowIfBeyondADouble(from, "The bandwidth of these observations");
            grid[0].ThrowIfBeyondADouble(from, "The spread of these observations");
            grid[^1].ThrowIfBeyondADouble(from, "The spread of these observations");
            tallest.ThrowIfBeyondADouble(from, "The density of these observations");

            int count = groups.Count;
            for (int g = 0; g < count; g++)
            {
                // The first group on the highest slot. Each ridge rises from its baseline by its density over the tallest
                // density of all, times the distance to the next baseline (one slot), so the tallest ridge reaches it exactly.
                var slot = CategorySlot.FromTop(g, count);
                double baseline = slot.Centre, spacing = slot.Width;
                double[] top = [.. densities[g].Select(d => baseline + d / tallest * spacing)];
                var ridge = AreaSeries.Band([.. grid], top, [.. grid.Select(_ => baseline)], AreaSeries.RestingAlpha);

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
        /// <para>The sample is compared with the normal distribution; <see cref="ProbabilityPlot"/> compares it with another one.
        /// A missing observation (NaN) and an infinity are left out, so the plot is that of the other observations. The verb sets
        /// no axis limits.</para>
        /// <para>The chart is its parts: written to JSON it is a scatter series and a trendline, and read back it draws the same
        /// picture. It is not fitted again; change the sample and call the verb again.</para></summary>
        /// <param name="sample">The observations, in any order; at least two that can be drawn.</param>
        /// <param name="configure">Optional styling of the two parts, run once after the verb has built and styled them, so what
        /// it sets wins: the points' colour, marker, transparency and legend label; the reference line's colour, width, style,
        /// or <see cref="Trendline.IsExtended"/> to run it across the whole plot.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="sample"/> is
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
        public AxesBuilder QQPlot(double[] sample, Action<QQPlotParts>? configure = null) =>
            axes.ProbabilityPlot(sample, QQReference.Normal, configure);

        /// <summary>Draws a probability plot: the sample sorted up the Y axis against the quantiles a reference distribution has
        /// at the same ranks along the X axis, with a reference line, so a reader sees at a glance how the sample differs from
        /// that distribution. It is the Q-Q plot (<see cref="QQPlot"/>) for any distribution: against the normal one the two
        /// draw the same picture, byte for byte.
        /// <para>On the line the sample follows the reference, up to location and scale: the line is the least-squares fit, so
        /// its slope estimates the sample's scale and its height at 0 the location. A curve away from the line at the ends means
        /// tails heavier or lighter than the reference's, a bow a skew the reference does not have.</para>
        /// <para>The conventions are scipy's (<c>scipy.stats.probplot</c> with <c>dist</c> and <c>sparams</c>). The k-th smallest of
        /// n observations stands at the reference's quantile of Filliben's plotting position for rank k: the median of the k-th
        /// smallest of n uniform observations, 0.5^(1/n) for the last, one minus that for the first, and (k - 0.3175) / (n + 0.365)
        /// for the others. The reference line is the least-squares line through the points, drawn from the first theoretical
        /// quantile to the last.</para>
        /// <para>The parts are those of the Q-Q plot: the points are ONE <see cref="ScatterSeries"/> wearing the theme's colour, the
        /// reference a <see cref="Trendline"/>, a solid red line 1.5 points wide that takes no colour slot and has no legend
        /// entry. The Y axis is titled "Sample quantiles"; the X axis "Theoretical quantiles" and, unless the reference is the
        /// normal distribution, its name in brackets ("Theoretical quantiles (exponential)"). A title of your own set after the
        /// call replaces either. A missing observation (NaN) and an infinity are left out, so the plot is that of the other
        /// observations. The verb sets no axis limits.</para>
        /// <para>The chart is its parts: written to JSON it is a scatter series and a trendline, and read back it draws the same
        /// picture. It is not fitted again; change the sample or the reference and call the verb again.</para></summary>
        /// <param name="sample">The observations, in any order; at least two that can be drawn.</param>
        /// <param name="reference">The distribution to compare the sample with: <see cref="QQReference.Normal"/>,
        /// <see cref="QQReference.Exponential"/>, <see cref="QQReference.Uniform"/>, <see cref="QQReference.LogNormal"/>,
        /// <see cref="QQReference.FittedLogNormal"/>, <see cref="QQReference.StudentT"/>, or one of your own made with
        /// <see cref="QQReference.Of"/>.</param>
        /// <param name="configure">Optional styling of the two parts, run once after the verb has built and styled them, so what
        /// it sets wins: the points' colour, marker, transparency and legend label; the reference line's colour, width, style,
        /// or <see cref="Trendline.IsExtended"/> to run it across the whole plot.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="sample"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The sample has fewer than two observations that can be drawn.</exception>
        /// <exception cref="InvalidOperationException">The reference has no finite quantile at one of the plotting positions: its
        /// quantile function broke its contract, as a window function that gives a weight that is not finite does.</exception>
        /// <example>
        /// <code>
        /// double[] waits = [2.1, 0.4, 5.6, 1.3, 0.9, 3.8, 0.2, 1.7, 2.6, 0.6, 8.4, 1.1];
        ///
        /// string svg = Plt.Create()
        ///     .WithTitle("Minutes between arrivals against the exponential distribution")
        ///     .AddSubPlot(1, 1, 1, ax => ax
        ///         .ProbabilityPlot(waits, QQReference.Exponential, parts => parts.Points.Alpha = 0.7)
        ///         .SetYLabel("Minutes between arrivals"))
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder ProbabilityPlot(double[] sample, QQReference reference, Action<QQPlotParts>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);
            ArgumentNullException.ThrowIfNull(sample);
            double[] ordered = sample.SortedObservations(LineObservations, "The sample", "a Q-Q plot", nameof(sample));

            var fit = reference.FitTo(ordered);
            double[] theoretical = fit.Theoretical;
            var points = new ScatterSeries(theoretical, ordered);
            Trendline line = null!;
            axes.AddSeries(points)
                .AddTrendline(theoretical[0], fit.StartY, theoretical[^1], fit.EndY, t =>
                {
                    line = t;
                    t.Color = ReferenceColor; t.LineWidth = LineWidthRule.Line.Resting;
                })
                .SetXLabel(reference.XAxisTitle)
                .SetYLabel(SampleQuantilesTitle);

            configure?.Invoke(new QQPlotParts(points, line));
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
        /// <para>No risk table and no log-rank test: they are the table under the plot, drawn on an axes of its own by
        /// <see cref="RiskTable"/>.</para>
        /// <para>The chart is its parts: written to JSON it is the curve, band and marks of each group, and read back it draws
        /// the same picture. It is not estimated again; change the groups and call the verb again.</para></summary>
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
        /// <exception cref="ArgumentNullException"><paramref name="groups"/> is
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
        public AxesBuilder KaplanMeier(IReadOnlyList<SurvivalGroup> groups, Action<LineSeries>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);
            ArgumentNullException.ThrowIfNull(groups);
            groups.ThrowIfEmpty("No groups were given; a Kaplan-Meier chart draws one curve per group.");

            // Every group is estimated before the first curve is added, so a refused call leaves the axes as it was.
            SurvivalCurve[] curves = [.. groups.Select(g => g.ProductLimit(nameof(groups)))];
            for (int g = 0; g < curves.Length; g++)
            {
                // The curve, its band and its censor marks are one group: they wear the curve's colour.
                var curve = new LineSeries(curves[g].Times, curves[g].Survival) { DrawStyle = DrawStyle.StepsPost, Label = groups[g].Label };
                var greenwood = curves[g].GreenwoodBand();
                var band = AreaSeries.Band(curves[g].Times, greenwood.Upper, greenwood.Lower, GreenwoodAlpha);
                double[] censoringTimes = groups[g].CensoringTimes();
                ScatterSeries? marks = censoringTimes.Length > 0
                    ? new ScatterSeries(censoringTimes, [.. censoringTimes.Select(t => curves[g].LevelAt(t))]) { Marker = MarkerStyle.Plus }
                    : null;
                ChartSeries[] members = marks is null ? [curve, band] : [curve, band, marks];
                axes.Axes.InColorGroup(members);

                axes.AddSeries(curve, configure);
                axes.AddSeries(band);
                if (marks is not null) axes.AddSeries(marks);
            }

            return axes.SetYLabel(SurvivalProbabilityTitle);
        }

        /// <summary>Draws the risk table of a Kaplan-Meier chart: a row for each group and, in it, at each time, the number of
        /// subjects still at risk (followed to that time or longer: the count the survival curve is estimated with, R's
        /// <c>n.risk</c>), and below the rows one line stating the log-rank test of whether the groups' survival differs.
        /// <para>The table is an axes of its own: stack it under the survival axes and share the X axis with
        /// <see cref="AxesBuilderLinkedAxesVerbs.ShareX"/>, so a count stands directly under its time on the survival curve, and the two plot
        /// areas have the same width (see the example). Without the shared X axis the table has no range of its own to place the
        /// times in.</para>
        /// <para>Each count is an <see cref="Annotation"/> at its time along the X axis and on the row of its group, centred, with
        /// the first group on the top row; a count at time 0, the left edge of the plot, stands just inside it and left-aligned, so
        /// it does not run into the names of the rows. The rows are named by the groups along the Y axis, which is titled "Number at risk" (a
        /// title of your own set after the call replaces it) and spans the rows: the verb sets these limits, since the table has
        /// no data to range over. The test is one more annotation, pinned to the right end of a slot of its own below the last row
        /// (a share of the axes, not of the data), so it never stands on a count: <c>Log-rank χ²(1) = 16.79, p &lt; 0.0001</c>,
        /// the statistic to two decimals and the p-value to four, or <c>Log-rank test: no events to compare</c> when there is
        /// nothing to compare. A single group has nothing to be compared with: no test, and no slot for it.</para>
        /// <para>The log-rank test is the Mantel-Cox form (R's <c>survdiff</c> with <c>rho = 0</c>, lifelines'
        /// <c>multivariate_logrank_test</c>): at every time an event happens, each group is expected to have its share of the
        /// events, its share of the subjects at risk; the statistic compares the events seen with those expected, with a
        /// variance that allows for ties, and has the number of groups less one degrees of freedom. A subject censored at an
        /// event time counts as at risk at it, as everywhere in the survival chart.</para>
        /// <para>The chart is its parts: written to JSON it is the annotations and the row names, and read back it draws the same
        /// picture. It is not counted again; change the groups and call the verb again.</para></summary>
        /// <param name="groups">The groups, in the order of the rows and of the curves they sit under; each with at least one
        /// subject, a finite duration at or above 0 and a flag for every subject (the same groups as the survival chart's).</param>
        /// <param name="times">The times to count at, in the unit of the durations, in the order they stand along the X axis. When
        /// left out, the ticks the automatic locator gives on 0 to the longest follow-up (at most nine).</param>
        /// <param name="configure">Optional styling of the parts, run once after the verb has built them, so what it sets wins:
        /// the counts' font and colour, the test line's position and box.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="groups"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">No groups are given; or a group carries no durations or no flags, its durations
        /// and flags differ in number, it has no subjects, or a duration is missing (NaN), negative or infinite (the message
        /// names the group); or <paramref name="times"/> is empty or holds a number that is not finite.</exception>
        /// <example>
        /// <code>
        /// using MatPlotLibNet.Models;
        /// using MatPlotLibNet.Models.Series;
        ///
        /// SurvivalGroup[] arms =
        /// [
        ///     new("Treatment", [6, 7, 10, 15, 19, 25, 32, 34], [true, true, false, true, false, true, false, false]),
        ///     new("Placebo", [1, 2, 3, 4, 5, 8, 8, 11], [true, true, true, false, true, true, true, true]),
        /// ];
        ///
        /// string svg = Plt.Create()
        ///     .WithSize(800, 600)
        ///     .WithGridSpec(2, 1, heightRatios: [4, 1])
        ///     .AddSubPlot(GridPosition.Single(0, 0), ax => ax.KaplanMeier(arms).WithLegend(), key: "survival")
        ///     .AddSubPlot(GridPosition.Single(1, 0), ax => ax.ShareX("survival").RiskTable(arms).SetXLabel("Weeks"))
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder RiskTable(IReadOnlyList<SurvivalGroup> groups, double[]? times = null,
            Action<RiskTableParts>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);
            ArgumentNullException.ThrowIfNull(groups);
            groups.ThrowIfEmpty("No groups were given; a risk table counts one row per group.");

            // Everything is checked before the first annotation is added, so a refused call leaves the axes as it was.
            double[][] followed = [.. groups.Select(g => g.ThrowIfNotSurvivalData(nameof(groups)))];
            double[] readAt = times ?? [.. new Rendering.TickLocators.MaxNLocator(Rendering.TickLocators.TickBudget.Standard.Most).Locate(0, followed.Max(d => d.Max()))];
            readAt.ThrowIfEmpty("The times of a risk table are one or more finite numbers.", nameof(times));

            foreach (double time in readAt) time.ThrowIfNotFinite(nameof(times), "A time of a risk table");

            // The groups take the rows from the top; one slot below them holds the test, when there is anything to compare.
            var logRank = LogRank.Test(groups);
            bool compared = groups.Count > 1;
            string[] names = [.. groups.Select(g => g.Label ?? string.Empty), .. compared ? new[] { string.Empty } : []];
            int rows = names.Length;

            // The centre of each group's row, worked out once: the counts stand on them and so do the names.
            double[] rowCentres = CategorySlot.CentresFromTop(groups.Count, rows);

            var counts = new List<Annotation>(groups.Count * readAt.Length);
            for (int g = 0; g < groups.Count; g++)
            {
                double row = rowCentres[g];
                foreach (double time in readAt)
                {
                    axes.Annotate(groups[g].AtRiskAt(time).InvariantText(), time, row, count =>
                    {
                        // The start of follow-up is the left edge of the plot, where a centred number would run into the row names.
                        bool atStart = time <= 0;
                        count.Alignment = atStart ? TextAlignment.Left : TextAlignment.Center;
                        count.OffsetX = atStart ? StartCountInset : 0;
                        count.VerticalAlignment = TextVerticalAlignment.Center;
                        counts.Add(count);
                    });
                }
            }

            Annotation? test = null;
            if (compared)
            {
                axes.Annotate(logRank.Describe(), 1, CategorySlot.Of(0).Centre / rows, line =>
                {
                    line.Coordinates = AnnotationCoordinates.AxesFraction;
                    line.Alignment = TextAlignment.Right;
                    line.VerticalAlignment = TextVerticalAlignment.Center;
                    line.OffsetX = -TestInset;
                    test = line;
                });
            }

            // The names stand on the rows of the groups; the slot of the test below them has no tick, so no mark stands without a name.
            axes.SetYTickLocator(new Rendering.TickLocators.FixedLocator(rowCentres))
                .SetYTickFormatter(names.AsCategoryTicks(topDown: true).Formatter)
                .SetYLim(0, rows)
                .SetYLabel(NumberAtRiskTitle);

            configure?.Invoke(new RiskTableParts(counts, test, logRank));
            return axes;
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
        /// <param name="groups">The groups, in the order they stand along the X axis and take their colour; each with one
        /// or more observations.</param>
        /// <param name="configure">Optional styling of all three parts together, run once after the verb has built them, so
        /// what it sets wins.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="groups"/> is
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
        public AxesBuilder Raincloud(IReadOnlyList<NamedSample> groups, Action<RaincloudParts>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);
            ArgumentNullException.ThrowIfNull(groups);
            groups.ThrowIfEmpty("No groups were given; a raincloud chart draws one violin, one box and one scatter per set of groups.");

            double[][] samples = [.. groups.Select(g => g.Values)];
            double[] pointX = [.. samples.SelectMany((s, g) => s.Select((_, i) => g + 0.35 + (i % 4) * 0.03))];
            double[] pointY = [.. samples.SelectMany(s => s)];

            // The violin and the box stand at their dataset's index, whatever theme draws them: a violin or a box that is given no
            // position stands at 1, 2, ... under the matplotlib themes, and the points and the names stand at the index.
            double[] positions = [.. Enumerable.Range(0, samples.Length).Select(k => (double)k)];
            var violin = new ViolinSeries(samples) { Side = ViolinSide.High, Alpha = 0.5, Label = "Samples", Positions = positions };
            var box = new BoxSeries(samples) { Widths = 0.1, ShowOutliers = false, Positions = positions };
            var points = new ScatterSeries(pointX, pointY) { MarkerSize = 6, Alpha = 1 };
            axes.Axes.InColorGroup(violin, box, points);
            axes.AddSeries(violin).AddSeries(box).AddSeries(points);

            configure?.Invoke(new RaincloudParts(violin, box, points));

            // One tick per group AT ITS DATASET INDEX, not the bar-style half-slot SetXCategoryTicks places a name at:
            // the violin and the box stand at their dataset's own index, so the name belongs on that same number.
            return axes.SetXCategoryTicks([.. groups.Select(g => g.Label)], positions);
        }

        /// <summary>Draws a stem-and-leaf display: every observation of the sample on a row, the digits above the last one as the
        /// stem and the last digit as a leaf, so the shape of the distribution shows and every value can still be read back.
        /// <para>A value is cut at the leaf unit (a power of ten) without rounding, toward zero: 1.29 at a unit of 0.1 is a 2 on
        /// the stem 1. The rows run from the smallest stem to the largest, a stem nobody falls on being a row of no leaves; negative
        /// values have negative stems, and the negative values smaller than ten leaf units stand on the stem "-0", before "0".
        /// Within a stem the leaves follow the order of the values, and a repeated value is a repeated leaf. Without a
        /// <paramref name="leafUnit"/> the finest power of ten is taken whose rows do not outnumber twice the square root of the
        /// observations (at least five) while its stems have no more than two digits.</para>
        /// <para>Each leaf has a column of its own, so the leaves of every row stand under each other because the cells are
        /// the same width, in any font: the display is a <see cref="TableSeries"/> in the plot (<see cref="TableLocation.InPlot"/>, under every
        /// theme) with no borders and no fill, the stems aligned to
        /// the right against a bar and the leaves centred. A line at the lower left of the plot says how to read it, such as
        /// <c>Key: 1 | 2 = 1.2</c>. The verb sets no axis limits.</para>
        /// <para>The chart is its parts: written to JSON it is the table and the annotation, and read back it draws the same
        /// picture. It is not laid out again; change the sample and call the verb again.</para></summary>
        /// <param name="sample">The observations; a missing value (NaN) and an infinity are left out.</param>
        /// <param name="leafUnit">The value of the last digit, a power of ten from 1e-15 to 1e15 such as 0.1 or 10, or
        /// <see langword="null"/> to choose one from the sample.</param>
        /// <param name="configure">Optional styling of the parts, run once after the verb has built them, so what it sets wins:
        /// the font size, the row height, the widths of the columns, the text of the key.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="sample"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="sample"/> holds no finite number.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="leafUnit"/> is not a power of ten, or is too fine for the
        /// largest value.</exception>
        /// <example>
        /// <code>
        /// double[] ages = [12, 15, 21, 23, 23, 38, 41, 44, 47, 52];
        ///
        /// string svg = Plt.Create()
        ///     .WithTitle("Ages")
        ///     .WithSize(500, 350)
        ///     .AddSubPlot(1, 1, 1, ax => ax.StemAndLeaf(ages))
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder StemAndLeaf(double[] sample, double? leafUnit = null, Action<StemAndLeafParts>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);
            ArgumentNullException.ThrowIfNull(sample);
            var display = sample.StemAndLeaf(leafUnit);

            int leaves = display.LeafColumns;
            var table = new TableSeries(display.Cells())
            {
                // The display fills its plot under every theme; a table on its own stands below the axes under the matplotlib themes.
                Location = TableLocation.InPlot,
                ColumnAlignments = [TextAlignment.Right, TextAlignment.Center, .. Enumerable.Repeat(TextAlignment.Center, leaves)],
                ColumnWidths = [StemColumnWidth, BarColumnWidth, .. Enumerable.Repeat(1.0, leaves)],
                CellHeight = StemRowHeight,
                CellColor = Colors.Transparent,
                BorderColor = Colors.Transparent,
            };
            axes.AddSeries(table);

            Annotation? key = null;
            axes.Annotate(display.Key, 0, KeyInset, line =>
            {
                line.Coordinates = AnnotationCoordinates.AxesFraction;
                line.Alignment = TextAlignment.Left;
                line.VerticalAlignment = TextVerticalAlignment.Bottom;
                line.OffsetX = KeyOffset;
                key = line;
            });

            configure?.Invoke(new StemAndLeafParts(table, key!));
            return axes;
        }
    }

    /// <summary>The widths of the stem column and of the bar column of a stem-and-leaf display, relative to the 1 of a leaf column.</summary>
    private const double StemColumnWidth = 2, BarColumnWidth = 0.5;

    /// <summary>The height of a row of a stem-and-leaf display, in pixels.</summary>
    private const double StemRowHeight = 20;

    /// <summary>How far above the foot of the plot (a share of its height) and in from its left edge (pixels) the key stands.</summary>
    private const double KeyInset = 0.02, KeyOffset = 8;

    /// <summary>The title of a Kaplan-Meier chart's Y axis.</summary>
    private const string SurvivalProbabilityTitle = "Survival probability";

    /// <summary>The title of a risk table's Y axis.</summary>
    private const string NumberAtRiskTitle = "Number at risk";

    /// <summary>How far the log-rank line stands in from the right edge of the risk table, in pixels.</summary>
    private const double TestInset = 6;

    /// <summary>How far a count at time 0 stands in from the left edge of the risk table, in pixels.</summary>
    private const double StartCountInset = 3;

    /// <summary>How many observations a Q-Q plot is drawn from at the least: a reference line is fitted through two points
    /// or more.</summary>
    private const int LineObservations = 2;

    /// <summary>The opacity of the band of a Kaplan-Meier curve: its pointwise confidence band (Greenwood's formula).</summary>
    private const double GreenwoodAlpha = 0.2;

    /// <summary>The colour of a Q-Q plot's reference line: red, as scipy's probplot draws it (<c>'r-'</c>).</summary>
    private static readonly Color ReferenceColor = Colors.Red;

    /// <summary>The title of a Q-Q plot's Y axis.</summary>
    private const string SampleQuantilesTitle = "Sample quantiles";

    /// <summary>How many observations a density is estimated from at the least: with one there is no spread to estimate a
    /// bandwidth from, and a single kernel says nothing about the shape of the group.</summary>
    private const int DensityObservations = 2;

    /// <summary>The number of points of a ridgeline's shared grid: seaborn's grid for a density curve, fine enough that a
    /// ridge drawn through them strays from the true density by less than a pixel at the usual sizes.</summary>
    private const int RidgePoints = 200;

    /// <summary>The product-limit (Kaplan-Meier) estimate of one <see cref="SurvivalGroup"/>, as the points of a step curve read
    /// "after each point, the level holds until the next" (matplotlib's <c>steps-post</c>): <see cref="DistributionAxesExtensions.SurvivalCurve.Times"/> ascending, and
    /// <see cref="DistributionAxesExtensions.SurvivalCurve.Survival"/> the estimated probability of surviving past each.</summary>
    /// <param name="Times">The time of each point: 0, then every time at which an event happened, then the last follow-up time
    /// when it is later than the last event.</param>
    /// <param name="Survival">The estimate at each of <paramref name="Times"/>: 1 at 0, then the level the curve drops to at each
    /// event time, then that last level again at the last follow-up.</param>
    /// <param name="AtRisk">The number at risk at each of <paramref name="Times"/> (n_i): the subjects whose duration is that
    /// time or later, counted just before that time's events. Greenwood's variance is computed from this and
    /// <paramref name="Events"/>, not re-derived.</param>
    /// <param name="Events">The number of events at each of <paramref name="Times"/> (d_i): 0 at the seed point and at an
    /// extension to the last follow-up, which hold no event of their own.</param>
    internal readonly record struct SurvivalCurve(double[] Times, double[] Survival, int[] AtRisk, int[] Events)
    {
        /// <summary>The standard normal distribution's 97.5th percentile: a two-sided 95 % confidence band is the estimate plus and
        /// minus this many standard errors. Written as the literal, not computed from the normal quantile, so the band keeps its bits.</summary>
        private const double Z95 = 1.959963984540054;

        /// <summary>Greenwood's confidence band around this curve, PLAIN form (the Kaplan-Meier verb's own doc explains why not the
        /// log or log-log transform): the variance accumulates only at a point with an event (<see cref="DistributionAxesExtensions.SurvivalCurve.Events"/> above 0) and
        /// only while every subject at risk there did not all have the event (dividing by n_i - d_i = 0 is undefined; a level of
        /// exactly 0 already has a band of [0, 0], so nothing is lost by stopping the sum there).</summary>
        /// <returns>The lower and the upper bound, one pair per point of the curve, each clamped to [0, 1].</returns>
        internal SurvivalBand GreenwoodBand()
        {
            int n = Times.Length;
            var lower = new double[n];
            var upper = new double[n];
            double cumulative = 0;
            for (int i = 0; i < n; i++)
            {
                if (Events[i] > 0 && AtRisk[i] > Events[i])
                {
                    cumulative += (double)Events[i] / (AtRisk[i] * (AtRisk[i] - Events[i]));
                }

                double s = Survival[i];
                double se = Math.Sqrt(s * s * cumulative);
                lower[i] = (s - Z95 * se).Saturate();
                upper[i] = (s + Z95 * se).Saturate();
            }

            return new SurvivalBand(lower, upper);
        }

        /// <summary>The level the curve is drawn at AT <paramref name="time"/>: the survival of the last point at or before it, read
        /// "steps-post" exactly as the curve itself is drawn (a level holds from its point to the next).</summary>
        /// <param name="time">A time, in the unit of the durations.</param>
        /// <returns>The level there; before the first point, the first level.</returns>
        internal double LevelAt(double time)
        {
            double level = Survival[0];
            for (int i = 0; i < Times.Length && Times[i] <= time; i++)
            {
                level = Survival[i];
            }

            return level;
        }
    }

    /// <summary>The two edges of a survival curve's confidence band, one value for each point of the curve.</summary>
    /// <param name="Lower">The lower bound.</param>
    /// <param name="Upper">The upper bound.</param>
    internal readonly record struct SurvivalBand(double[] Lower, double[] Upper);
}
