// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet;

/// <summary>Charts whose data is an x array and values over it: every verb here composes series the library already
/// draws, one series for each drawn unit, takes its x positions as <c>double[]</c>, and its values as a <c>double[]</c> for
/// one unnamed series or as named <see cref="BarGroup"/>s, one value per x position. A missing value (NaN) leaves a gap,
/// never a bridge. The verbs set no axis limits unless their chart cannot be read without them, because a limit clips every
/// series on the axes and overwrites the caller's own; <see cref="BumpChart"/> says so where it sets them.</summary>
public static class XYAxesExtensions
{
    /// <summary>Draws a bump chart: one line for each group, showing how its rank changes from one x position to the next,
    /// with rank 1 on top and every line named at its right end.
    /// <para>Each line is ONE <see cref="LineSeries"/> with a marker at each rank, wearing one colour of the theme's cycle.
    /// The group's name stands at the end of its line, in the line's colour, and replaces the legend entry, so the chart
    /// needs no legend; a line whose last rank is missing is named at the last rank that is there.</para>
    /// <para>The verb sets the Y limits, because that is what makes a bump chart: rank 1 on top, running down to the lowest
    /// rank (the number of groups, or the lowest rank drawn when that is more), and a tick for every whole rank. Each end
    /// has half a rank of margin, because an axis limit clips at the frame and a marker standing on it would be cut in half.
    /// A limit of your own set after the call replaces these. The X axis is left to the data, its ticks too: for a handful of
    /// whole-number positions such as years, give it <c>SetXTickLocator(new MultipleLocator(1))</c>.</para>
    /// <para>A missing rank (NaN) leaves a gap in the line, not a bridge, and a point alone between two gaps is a marker
    /// with no line. Ranks may be fractional (the average rank of a tie, 2.5): the marker stands between the ticks. Groups
    /// may tie: their lines run through the same points. Ranking the data, and settling a tie the way the competition does,
    /// is the caller's job; the verb draws the ranks it is given.</para></summary>
    /// <param name="axes">The axes the chart is added to.</param>
    /// <param name="x">The position of each ranking along the X axis (a season, a year, a round), in the order they are
    /// drawn.</param>
    /// <param name="ranks">The groups to draw, one line each, in the order they take colours. A group's
    /// <see cref="BarGroup.Label"/> names its line, and its <see cref="BarGroup.Values"/> are its rank at each x position:
    /// one value per position, 1 or more, or NaN for a rank that is missing.</param>
    /// <param name="configure">Optional styling, run once for each line, in the order of the groups, after the verb has
    /// built it, so what it sets wins: the colour, the width, the marker, or putting the name back in the legend.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="axes"/>, <paramref name="x"/> or <paramref name="ranks"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">No groups are given; a group does not carry one value per x position (the message
    /// names the group); or a rank is below 1 or infinite.</exception>
    /// <example>
    /// <code>
    /// using MatPlotLibNet.Rendering.TickLocators;
    ///
    /// double[] seasons = [2021, 2022, 2023, 2024];
    ///
    /// string svg = Plt.Create()
    ///     .WithTitle("League table")
    ///     .AddSubPlot(1, 1, 1, ax => ax
    ///         .BumpChart(seasons,
    ///         [
    ///             new BarGroup("Hawks", [1, 2, 3, 3]),
    ///             new BarGroup("Owls", [2, 1, 1, 2]),
    ///             new BarGroup("Wrens", [3, 3, 2, 1]),
    ///         ])
    ///         .SetXTickLocator(new MultipleLocator(1))
    ///         .SetXLabel("Season")
    ///         .SetYLabel("Rank"))
    ///     .ToSvg();
    /// </code>
    /// </example>
    public static AxesBuilder BumpChart(this AxesBuilder axes, double[] x, IReadOnlyList<BarGroup> ranks,
        Action<LineSeries>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(axes);
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(ranks);
        if (ranks.Count == 0)
        {
            throw new ArgumentException("No groups were given; a bump chart draws one line per group.", nameof(ranks));
        }

        // Everything is checked before the first line is added, so a refused call leaves the axes as it was.
        foreach (var group in ranks)
        {
            group.ThrowIfMisaligned(x.Length, nameof(ranks), "x values");
        }

        ThrowIfNotRanks(x, ranks);

        foreach (var group in ranks)
        {
            // Each line holds its own x array, so editing one line's data cannot move another's.
            var line = new LineSeries([.. x], group.Values)
            {
                LineWidth = BumpLineWidth, Marker = MarkerStyle.Circle, MarkerSize = BumpMarkerSize,
                Label = group.Label, LabelPlacement = SeriesLabelPlacement.LineEnd,
            };
            axes.AddSeries(line, configure);
        }

        // Rank 1 on top: the limits run downward. The half rank at each end is the margin a marker standing on a rank needs,
        // because a limit clips at the frame. The axis reaches the lowest rank drawn when that is lower than the last
        // place of a table of this many groups (a few teams of a longer table, or a fractional rank past the last whole one).
        double lowest = Math.Max(ranks.Count, Math.Ceiling(ranks.SelectMany(g => g.Values).Where(double.IsFinite).DefaultIfEmpty(0).Max()));
        return axes.SetYLim(lowest + 0.5, 0.5).SetYTickLocator(new MultipleLocator(1));
    }

    /// <summary>Draws a control chart: the individuals chart (the X chart of an I-MR pair, a Shewhart chart of single
    /// values), with the values in time order as a marked line, the centre line and the two control limits across the plot,
    /// and every value beyond a limit marked, so a reader sees at a glance where the process left its usual variation.
    /// <para>Unless <paramref name="limits"/> are given, they are estimated from the values the individuals chart's way:
    /// the centre is the mean of the values; sigma is the average moving range (the mean distance between each value and the
    /// one before it) divided by d2 = 1.128, the constant for ranges of two; and the limits stand three sigma below and above
    /// the centre. The moving range follows the process from one value to the next, so a single spike barely moves it and
    /// stands out beyond the limits, where a standard deviation would widen around it. Limits that are given are drawn as
    /// they are and nothing is estimated: the limits of an earlier period the process is held to, say.</para>
    /// <para>Only rule 1 of the Western Electric rules is marked: a value strictly beyond a limit is a violation; a value on
    /// a limit is not. The other rules (two of three beyond two sigma, eight in a row on one side, and the rest) are not
    /// checked.</para>
    /// <para>The values are ONE <see cref="LineSeries"/> with a circle on each value, in the theme's first colour; the
    /// violations are ONE <see cref="ScatterSeries"/> over those values, in the theme's second colour, so each stands out as
    /// a larger dot that covers the line's marker. The centre is a solid reference line, the limits dashed ones, named CL,
    /// LCL and UCL at their right ends and drawn in the colour a reference line has when none is set. The automatic range
    /// holds the limits, so they are drawn inside the plot even when every value lies between them; the verb sets no axis
    /// limits, and a limit of your own stays yours. Nothing is named in the legend until <paramref name="configure"/> names
    /// the values or the violations.</para>
    /// <para>A value that is missing (NaN) or infinite breaks the line, is left out of the estimate (the moving range is then
    /// taken between the values on either side of it) and is never a violation.</para>
    /// <para>The chart is its parts: written to JSON it is a line series, a scatter series and three reference lines, and
    /// read back it draws the same picture. It is not estimated again; change the values and call the verb again.</para></summary>
    /// <param name="axes">The axes the chart is added to.</param>
    /// <param name="x">The position of each value along the X axis (a sample number, a day), in the order the values were
    /// taken.</param>
    /// <param name="values">The values, one per x position, in the order they were taken: the order the moving range follows.
    /// Without <paramref name="limits"/>, at least two that can be drawn.</param>
    /// <param name="limits">The centre and the limits to draw; <see langword="null"/> (the default) to estimate them from
    /// <paramref name="values"/>.</param>
    /// <param name="configure">Optional styling of the five parts, run once after the verb has built and styled them, so what
    /// it sets wins: the line's and the violations' colour, marker and legend label; a reference line's colour, style, width
    /// or name. The reference lines also tell the limits the chart was drawn with.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="axes"/>, <paramref name="x"/> or <paramref name="values"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="values"/> does not carry one value per x position (the message
    /// names both counts); <paramref name="limits"/> are not three finite numbers with Lower &lt;= Center &lt;= Upper; or no
    /// limits are given and fewer than two values can be drawn.</exception>
    /// <example>
    /// <code>
    /// double[] day = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
    /// double[] fill = [50.2, 49.8, 50.1, 50.4, 49.9, 50.0, 53.9, 50.3, 49.7, 50.1];
    ///
    /// string svg = Plt.Create()
    ///     .WithTitle("Fill weight")
    ///     .AddSubPlot(1, 1, 1, ax => ax
    ///         .ControlChart(day, fill, configure: parts => parts.Violations.Label = "beyond a limit")
    ///         .SetXLabel("Day")
    ///         .SetYLabel("Weight (g)"))
    ///     .ToSvg();
    /// </code>
    /// </example>
    public static AxesBuilder ControlChart(this AxesBuilder axes, double[] x, double[] values, ControlLimits? limits = null,
        Action<ControlChartParts>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(axes);
        ArgumentNullException.ThrowIfNull(x);
        values.ThrowIfMisaligned(x.Length, nameof(values), "x positions", "x position");

        // Everything is checked (and estimated) before the first part is added, so a refused call leaves the axes as it was.
        var drawn = limits is { } given ? given.ThrowIfNotOrdered() : values.IndividualsLimits();

        // Rule 1: a value strictly beyond a limit. A missing or infinite value is never one: it cannot be placed.
        int[] beyond = [.. Enumerable.Range(0, values.Length).Where(i => double.IsFinite(values[i]) && (values[i] > drawn.Upper || values[i] < drawn.Lower))];

        // The line first (the theme's first colour), the violations after it (the second, drawn over the line's markers).
        var line = new LineSeries([.. x], [.. values]) { Marker = MarkerStyle.Circle };
        var violations = new ScatterSeries([.. beyond.Select(i => x[i])], [.. beyond.Select(i => values[i])]);
        ReferenceLine center = null!, lower = null!, upper = null!;
        axes.AddSeries(line)
            .AddSeries(violations)
            .AxHLine(drawn.Center, r => { center = r; r.LineStyle = LineStyle.Solid; r.Label = CenterLineName; })
            .AxHLine(drawn.Lower, r => { lower = r; r.Label = LowerLimitName; })
            .AxHLine(drawn.Upper, r => { upper = r; r.Label = UpperLimitName; });

        configure?.Invoke(new ControlChartParts(line, violations, center, lower, upper));
        return axes;
    }

    /// <summary>The limits of the individuals chart estimated from <paramref name="values"/>: the centre is the mean of the
    /// values that can be drawn, in their order; sigma is the average moving range between neighbouring ones (a missing value
    /// is left out, so the range spans its gap) over <see cref="MovingRangeD2"/>; the limits stand
    /// <see cref="ControlSigmas"/> sigma either side of the centre.</summary>
    /// <exception cref="ArgumentException">Fewer than two values can be drawn: a moving range is the distance between two.</exception>
    private static ControlLimits IndividualsLimits(this double[] values)
    {
        double[] there = values.Observations(MovingRangeObservations, "The sample", "the limits of a control chart are estimated from", nameof(values));
        double sum = 0, ranges = 0;
        for (int i = 0; i < there.Length; i++)
        {
            sum += there[i];
            if (i > 0) ranges += Math.Abs(there[i] - there[i - 1]);
        }

        double center = sum / there.Length;
        double sigma = ranges / (there.Length - 1) / MovingRangeD2;
        return new ControlLimits(center, center - ControlSigmas * sigma, center + ControlSigmas * sigma);
    }

    /// <summary>Refuses limits that are not three finite numbers with Lower &lt;= Center &lt;= Upper: a centre outside its limits,
    /// or a lower limit above the upper, marks every value or none.</summary>
    /// <returns>The same limits, for chaining.</returns>
    /// <exception cref="ArgumentException">The limits are not finite or not in that order; the message names the three.</exception>
    private static ControlLimits ThrowIfNotOrdered(this ControlLimits limits)
    {
        var (center, lower, upper) = limits;
        if (!(double.IsFinite(center) && double.IsFinite(lower) && double.IsFinite(upper) && lower <= center && center <= upper))
        {
            throw new ArgumentException(
                $"Control limits are three finite numbers, Lower <= Center <= Upper; these are Lower {lower.ToString(CultureInfo.InvariantCulture)}, Center {center.ToString(CultureInfo.InvariantCulture)}, Upper {upper.ToString(CultureInfo.InvariantCulture)}.",
                nameof(limits));
        }

        return limits;
    }

    /// <summary>d2 for ranges of two values: the expected range of two standard normal observations (2 / sqrt(pi), to the
    /// three decimals the control-chart tables give), by which the average moving range of an individuals chart is divided
    /// to estimate sigma.</summary>
    private const double MovingRangeD2 = 1.128;

    /// <summary>How many sigma the control limits stand from the centre: three, Shewhart's limits.</summary>
    private const double ControlSigmas = 3;

    /// <summary>How many values the limits are estimated from at the least: a moving range is the distance between two.</summary>
    private const int MovingRangeObservations = 2;

    /// <summary>The name of a control chart's centre line, at its right end.</summary>
    private const string CenterLineName = "CL";

    /// <summary>The name of a control chart's lower control limit, at its right end.</summary>
    private const string LowerLimitName = "LCL";

    /// <summary>The name of a control chart's upper control limit, at its right end.</summary>
    private const string UpperLimitName = "UCL";

    /// <summary>Refuses a rank below 1 or infinite: a rank is a place counted from 1, and a mark at 0 would be cut in half by
    /// the frame while an infinite one cannot be placed at all. NaN is a missing rank, not a bad one.</summary>
    /// <exception cref="ArgumentException">A rank is below 1 or infinite; the message names the group, the x position and
    /// the value.</exception>
    private static void ThrowIfNotRanks(double[] x, IReadOnlyList<BarGroup> ranks)
    {
        foreach (var group in ranks)
        {
            for (int i = 0; i < x.Length; i++)
            {
                double rank = group.Values[i];
                if (!double.IsNaN(rank) && !(rank >= 1 && double.IsFinite(rank)))
                {
                    throw new ArgumentException(
                        $"The rank of '{group.Label}' at x = {x[i].ToString(CultureInfo.InvariantCulture)} is {rank.ToString(CultureInfo.InvariantCulture)}; a rank counts from 1 and is finite (NaN marks a missing rank).",
                        nameof(ranks));
                }
            }
        }
    }

    /// <summary>The width of a bump chart's lines, in points: thick, because the chart is read as ribbons crossing.</summary>
    private const double BumpLineWidth = 3;

    /// <summary>The size of the markers a bump chart puts on each rank, in points.</summary>
    private const double BumpMarkerSize = 10;

    /// <summary>Draws a stream graph (a streamgraph): a stacked area chart whose stack floats around the middle of the plot
    /// instead of standing on zero, so the layers flow into one another like a river. A layer's value at an x position is
    /// the thickness of its stream there, and the whole stream swells and thins with the total.
    /// <para>Each layer is ONE <see cref="AreaSeries"/>, filled between its lower and its upper edge, wearing one colour of
    /// the theme's cycle and named in the legend by the layer's name, so the legend lists one entry per layer and a click on
    /// an entry hides that layer alone. The first layer is the lowest. The streams are opaque, so each wears exactly the
    /// colour of its legend entry. The edges are worked out by the very code
    /// <c>StackPlot</c> stacks its layers with, so a stream graph and a stacked area chart of the same data draw the same
    /// layers; only the baseline differs. <paramref name="baseline"/> chooses where the stack stands:
    /// <see cref="StackedBaseline.Wiggle"/> (the default) is matplotlib's <c>wiggle</c>, which keeps the middles of the layers
    /// as near to the middle line as the stack allows, so the lower layers weigh more;
    /// <see cref="StackedBaseline.WeightedWiggle"/> is matplotlib's <c>weighted_wiggle</c>, which it calls the streamgraph
    /// layout, and also follows how each layer changes, weighted by its size, so thin layers do not push the thick ones
    /// around; <see cref="StackedBaseline.Symmetric"/> puts the middle of the total on the middle line (ThemeRiver); and
    /// <see cref="StackedBaseline.Zero"/> stands the stack on zero, which is a plain stacked area chart.</para>
    /// <para>The vertical axis shows thickness, not a value counted from zero: where the stream lies up and down means
    /// nothing, and only the distance between a layer's edges reads as its value. So the verb gives the Y axis no ticks, no
    /// tick labels and no grid line (an empty fixed locator, which replaces a Y tick locator set before the call). To read
    /// thicknesses against a scale, put the ticks back with
    /// <c>SetYTickLocator(new AutoLocator())</c>; the numbers then name positions, and a thickness is the distance between two
    /// of them. The verb sets no axis limits, and a limit of your own stays yours. The X axis is left to the data, its ticks
    /// too: for a handful of whole-number positions such as years, give it <c>SetXTickLocator(new MultipleLocator(1))</c>.</para>
    /// <para>The stream fills the plot, so the legend, which stands in the upper right corner by default, covers part of it:
    /// put it beside the plot with <c>WithLegend(LegendPosition.OutsideRight)</c> under <c>TightLayout()</c>.</para>
    /// <para>A value is a thickness: it is finite and 0 or more. A missing value (NaN) leaves a gap, never a bridge. With the
    /// three floating baselines the middle of the stream cannot be placed without it, so every layer has a gap at that x
    /// position; with <see cref="StackedBaseline.Zero"/> only that layer and the layers stacked on it do.</para>
    /// <para>The chart is its parts: written to JSON it is one area series per layer, and read back it draws the same
    /// picture. It is not stacked again after a round trip; change the data and call the verb again.</para></summary>
    /// <param name="axes">The axes the chart is added to.</param>
    /// <param name="x">The position of each value along the X axis (a year, a week), in the order they are drawn.</param>
    /// <param name="layers">The layers, from the lowest up, one stream each. A layer's <see cref="BarGroup.Label"/> names its
    /// stream in the legend (a layer with no name has no legend entry) and its <see cref="BarGroup.Values"/> are its
    /// thickness at each x position: one value per position, 0 or more, or NaN for a value that is missing.</param>
    /// <param name="baseline">Where the stack stands; <see cref="StackedBaseline.Wiggle"/> by default.</param>
    /// <param name="configure">Optional styling, run once for each layer, in the order of the layers, after the verb has
    /// built it, so what it sets wins: the colour, the transparency, the outline.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="axes"/>, <paramref name="x"/> or <paramref name="layers"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="baseline"/> is not one of the
    /// <see cref="StackedBaseline"/> values.</exception>
    /// <exception cref="ArgumentException">No layers are given; a layer does not carry one value per x position (the message
    /// names the layer); or a value is negative or infinite (the message names the layer, the x position and the value).
    /// </exception>
    /// <example>
    /// <code>
    /// using MatPlotLibNet.Rendering.TickLocators;
    ///
    /// double[] year = [2019, 2020, 2021, 2022, 2023];
    ///
    /// string svg = Plt.Create()
    ///     .WithTitle("Listening by genre")
    ///     .TightLayout()
    ///     .AddSubPlot(1, 1, 1, ax => ax
    ///         .StreamGraph(year,
    ///         [
    ///             new BarGroup("Rock", [8, 7, 6, 5, 4]),
    ///             new BarGroup("Pop", [3, 5, 7, 8, 9]),
    ///             new BarGroup("Jazz", [2, 2, 3, 3, 4]),
    ///         ])
    ///         .SetXTickLocator(new MultipleLocator(1))
    ///         .SetXLabel("Year")
    ///         .WithLegend(LegendPosition.OutsideRight))
    ///     .ToSvg();
    /// </code>
    /// </example>
    public static AxesBuilder StreamGraph(this AxesBuilder axes, double[] x, IReadOnlyList<BarGroup> layers,
        StackedBaseline baseline = StackedBaseline.Wiggle, Action<AreaSeries>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(axes);
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(layers);
        if (layers.Count == 0)
        {
            throw new ArgumentException("No layers were given; a stream graph draws one stream per layer.", nameof(layers));
        }

        if (!Enum.IsDefined(baseline))
        {
            throw new ArgumentOutOfRangeException(nameof(baseline), baseline, "The baseline is not one of the StackedBaseline values.");
        }

        // Everything is checked before the first area is added, so a refused call leaves the axes as it was.
        foreach (var layer in layers)
        {
            layer.ThrowIfMisaligned(x.Length, nameof(layers), "x values");
        }

        ThrowIfNotThicknesses(x, layers);

        // The very edges StackPlot fills its layers between: each layer on the baseline the strategy gives it, up by its value.
        double[][] values = [.. layers.Select(layer => layer.Values)];
        double[][] lower = baseline.ComputeFor(values, x.Length);
        double[][] upper = lower.TopEdgesFor(values, x.Length);

        for (int k = 0; k < layers.Count; k++)
        {
            // Each area holds its own x array, so editing one layer's data cannot move another's. A layer with no name has no
            // legend entry: an entry with no text would name nothing.
            var stream = new AreaSeries([.. x], upper[k])
            {
                YData2 = lower[k], Alpha = StreamAlpha, Label = string.IsNullOrEmpty(layers[k].Label) ? null : layers[k].Label,
            };
            axes.AddSeries(stream, configure);
        }

        // Where the stream lies up and down means nothing, and only the distance between a layer's edges reads as its value:
        // an empty fixed locator draws no tick, no label and no grid line along the Y axis.
        return axes.SetYTickLocator(new FixedLocator([]));
    }

    /// <summary>Refuses a value that is not a thickness: negative or infinite. A stream of negative thickness is undefined, and
    /// an infinite one cannot be drawn. NaN is a missing value, not a bad one.</summary>
    /// <exception cref="ArgumentException">A value is negative or infinite; the message names the layer, the x position and
    /// the value.</exception>
    private static void ThrowIfNotThicknesses(double[] x, IReadOnlyList<BarGroup> layers)
    {
        foreach (var layer in layers)
        {
            for (int i = 0; i < x.Length; i++)
            {
                double thickness = layer.Values[i];
                if (!double.IsNaN(thickness) && !(thickness >= 0 && double.IsFinite(thickness)))
                {
                    throw new ArgumentException(
                        $"The value of '{layer.Label}' at x = {x[i].ToString(CultureInfo.InvariantCulture)} is {thickness.ToString(CultureInfo.InvariantCulture)}; a stream's thickness is finite and 0 or more (NaN marks a missing value).",
                        nameof(layers));
                }
            }
        }
    }

    /// <summary>The transparency of a stream: opaque, as matplotlib's stackplot draws its layers, so a layer wears exactly the
    /// colour of its legend entry and the grid does not show through it. (An area is faint by default, which suits a fill
    /// under a line and not a stacked stream; the outline an area strokes along its top covers the seam between two
    /// layers.)</summary>
    private const double StreamAlpha = 1;

    /// <summary>Draws a horizon chart: one series folded into <paramref name="bands"/> bands of the same hue and
    /// increasing depth, so a tall value reads as a short, deeply-coloured band instead of a line that runs off the
    /// top of a short plot — a way to show a series with a wide range of values in a strip a few dozen pixels tall.
    /// <para>The fold height h is the largest absolute value <paramref name="y"/> reaches, divided by
    /// <paramref name="bands"/>. Band k (0 first, the lightest) is clamp(y - k x h, 0, h): 0 below the band, h above
    /// it, the value itself between — NOT y mod h, which would fold a value sitting exactly on a band edge to 0 in
    /// every band above it. Every band is an <see cref="AreaSeries"/> filled from 0, with
    /// <see cref="AreaSeries.Alpha"/> (k + 1) / <paramref name="bands"/>, so the last band is fully opaque and the
    /// first the faintest — stacked on top of each other (not beside), the overlap is what reads as depth.</para>
    /// <para>When <paramref name="y"/> never goes negative, that is the whole chart: <paramref name="bands"/> bands, one
    /// colour group, one hue. When it does, the negative part folds the SAME way below zero (clamp(-y - k x h, 0, h),
    /// negated) into its OWN <paramref name="bands"/> bands and its OWN colour group, a second hue — a positive and a
    /// negative value never share a band, because a tall peak and a deep trough are different things worth telling
    /// apart in a strip too short to show either as a line.</para>
    /// <para>No band carries a label: a fold is a view of one series, not a unit of its own to name in a legend. A
    /// missing value (NaN) leaves a gap in every band at that point, never a zero. The verb sets no axis limits.</para></summary>
    /// <param name="axes">The axes the chart is added to.</param>
    /// <param name="x">The x position of each value.</param>
    /// <param name="y">The value at each x position; at least one must be finite, to measure the fold height from.</param>
    /// <param name="bands">How many bands fold each side of zero into, at least 1. Default 3.</param>
    /// <param name="configure">Optional styling of every band together, run once after the verb has built them, so what
    /// it sets wins.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="axes"/>, <paramref name="x"/> or <paramref name="y"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="y"/> differ in length;
    /// <paramref name="y"/> has no finite value; or <paramref name="bands"/> is below 1.</exception>
    /// <example>
    /// <code>
    /// double[] day = [.. Enumerable.Range(0, 60).Select(i => (double)i)];
    /// double[] value = [.. day.Select(d => 40 * Math.Sin(d / 5.0))];
    ///
    /// string svg = Plt.Create()
    ///     .WithSize(700, 120)
    ///     .AddSubPlot(1, 1, 1, ax => ax.Horizon(day, value, bands: 4))
    ///     .ToSvg();
    /// </code>
    /// </example>
    public static AxesBuilder Horizon(this AxesBuilder axes, double[] x, double[] y, int bands = 3, Action<HorizonParts>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(axes);
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);
        if (x.Length != y.Length)
        {
            throw new ArgumentException($"x has {x.Length} values and y has {y.Length}; there must be one y per x.", nameof(y));
        }

        if (bands < 1)
        {
            throw new ArgumentException($"{bands} bands were asked for; a horizon chart needs at least 1.", nameof(bands));
        }

        double maxAbs = 0;
        bool anyFinite = false, anyNegative = false;
        foreach (double v in y)
        {
            if (!double.IsFinite(v)) continue;
            anyFinite = true;
            if (v < 0) anyNegative = true;
            maxAbs = Math.Max(maxAbs, Math.Abs(v));
        }

        if (!anyFinite)
        {
            throw new ArgumentException("y has no finite value; a horizon chart measures its fold height from the largest one.", nameof(y));
        }

        double bandHeight = maxAbs == 0 ? 1 : maxAbs / bands;

        List<AreaSeries> Fold(bool negative)
        {
            int colorGroup = axes.NextColorGroup();
            var folds = new List<AreaSeries>(bands);
            for (int k = 0; k < bands; k++)
            {
                double offset = k * bandHeight;
                double[] folded = [.. y.Select(v => !double.IsFinite(v) ? double.NaN
                    : negative ? -Math.Clamp(-v - offset, 0, bandHeight)
                    : Math.Clamp(v - offset, 0, bandHeight))];
                var band = new AreaSeries(x, folded) { Alpha = (k + 1.0) / bands };
                band.ColorGroup = colorGroup;
                axes.AddSeries(band);
                folds.Add(band);
            }

            return folds;
        }

        var positive = Fold(negative: false);
        var negativeFolds = anyNegative ? Fold(negative: true) : [];
        configure?.Invoke(new HorizonParts(positive, negativeFolds));
        return axes;
    }
}
