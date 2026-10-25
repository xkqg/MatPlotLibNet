// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet;

/// <summary>Charts whose data is keyed by names: one mark, or one group of marks, for each category. Every verb here
/// composes series the library already draws, takes the categories as <c>string[]</c> and puts each mark on the centre of
/// its category's slot, the place a bar and the category's name stand on too. The verbs set no axis limits, because a
/// limit clips every series on the axes and overwrites the caller's own; the one exception is <see cref="Pareto"/>, whose
/// two Y axes are set to one scale so the line can be read against the bars.</summary>
public static class CategoricalAxesExtensions
{
    extension(AxesBuilder axes)
    {
        /// <summary>Draws a lollipop chart: one stem for each category, standing on the centre of the category's slot and
        /// ending in a head at its value, with the categories named on the X axis.
        /// <para>The chart is ONE <see cref="StemSeries"/>, because a stem already draws its own head. So it wears one colour,
        /// has one legend entry, and a click on that entry hides the whole chart. A missing value (NaN) draws no stem and no
        /// head. The stems stand on the baseline at 0.</para>
        /// <para>The verb sets no axis limits: the automatic range leaves the first and last head their margin, so every head
        /// is a whole circle, and a limit of your own set before or after the call stays yours.</para></summary>
        /// <param name="categories">The categories' names, along the X axis in the order given.</param>
        /// <param name="values">The value of each category, one per category, in the same order.</param>
        /// <param name="configure">Optional styling of the one series: the colours of the stem and its head, the head's marker,
        /// the legend label.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="categories"/> or
        /// <paramref name="values"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="values"/> does not carry one value per category.</exception>
        /// <example>
        /// <code>
        /// string[] products = ["Alpha", "Beta", "Gamma", "Delta"];
        /// double[] scores = [82.5, 67.3, 91.1, 54.8];
        ///
        /// string svg = Plt.Create()
        ///     .AddSubPlot(1, 1, 1, ax => ax
        ///         .Lollipop(products, scores, s => s.StemColor = Color.FromHex("#5B9BD5"))
        ///         .SetYLabel("Score"))
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder Lollipop(string[] categories, double[] values,
            Action<StemSeries>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);
            ArgumentNullException.ThrowIfNull(categories);
            values.ThrowIfMisaligned(categories.Length, nameof(values));

            return axes
                .Stem(SlotCentres(categories.Length), values, configure)
                .SetXCategoryTicks(categories);
        }

        /// <summary>Draws a dumbbell chart: for each category two dots, where it was and where it is, joined by a connector, with
        /// the categories named on the X axis.
        /// <para>The dots are two scatter series, labelled with the groups' names and wearing the theme's first two colours. The
        /// connector is an error bar of no error that spans the lower of the two values to the higher one, WHICHEVER group is
        /// higher: a category that went down is joined as well as one that went up. It is a neutral grey, drawn behind the dots,
        /// added last so it takes no colour slot, and it has no legend entry.</para>
        /// <para>A click on a legend entry hides that group's dots only. The connector belongs to neither group, so it stays
        /// visible while a group is hidden. A missing value (NaN) in either group leaves that category's connector out, and its
        /// dot if it is the dot that is missing.</para>
        /// <para>The verb sets no axis limits; a limit of your own stays yours.</para></summary>
        /// <param name="categories">The categories' names, along the X axis in the order given.</param>
        /// <param name="from">The first dot of each category (where it was); its <see cref="BarGroup.Label"/> names the dots in
        /// the legend. One value per category.</param>
        /// <param name="to">The second dot of each category (where it is); its <see cref="BarGroup.Label"/> names the dots in the
        /// legend. One value per category.</param>
        /// <param name="configure">Optional styling of the three series, run once after the verb has styled them, so what it sets
        /// wins.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="categories"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">A group does not carry one value per category; the message names the group and
        /// the parameter.</exception>
        /// <example>
        /// <code>
        /// string[] products = ["A", "B", "C"];
        ///
        /// string svg = Plt.Create()
        ///     .AddSubPlot(1, 1, 1, ax => ax
        ///         .Dumbbell(products,
        ///             new BarGroup("Before", [62.0, 78.5, 55.0]),
        ///             new BarGroup("After", [74.0, 82.0, 48.0]),
        ///             parts => parts.Connector.LineWidth = 2)
        ///         .WithLegend())
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder Dumbbell(string[] categories, BarGroup from, BarGroup to,
            Action<DumbbellParts>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);
            ArgumentNullException.ThrowIfNull(categories);
            from.ThrowIfMisaligned(categories.Length, nameof(from));
            to.ThrowIfMisaligned(categories.Length, nameof(to));

            double[] x = SlotCentres(categories.Length);

            // The connector spans [min, max] of the two values of a category: its foot is the lower value and it runs up by the
            // distance between them, so it is right whichever group is higher. A missing value makes both NaN, and the error bar
            // skips a point that cannot be placed along with its bars.
            double[] low = [.. from.Values.Zip(to.Values, Math.Min)];
            double[] span = [.. from.Values.Zip(to.Values, (a, b) => Math.Abs(b - a))];

            var fromDots = new ScatterSeries(x, from.Values) { Marker = MarkerStyle.Circle, MarkerSize = DumbbellDotSize, Label = from.Label };
            var toDots = new ScatterSeries(x, to.Values) { Marker = MarkerStyle.Circle, MarkerSize = DumbbellDotSize, Label = to.Label };

            // Added last: the dots take colour slots 0 and 1, and the connector's explicit grey takes none. ZOrder -1 draws it
            // first, behind the dots. The grey is the library's neutral (every reference line, level and trendline wears it):
            // a colour fixed here is read against whatever theme is set later, and this one stands at 3 : 1 or more against the
            // plot of 25 of the 30 shipped themes, where a lighter #AAAAAA reached it on 12.
            var connector = new ErrorBarSeries(x, low, new double[x.Length], span)
            {
                Color = Colors.Gray, LineWidth = 3, CapSize = 0, ZOrder = -1,
            };

            axes.AddSeries(fromDots).AddSeries(toDots).AddSeries(connector).SetXCategoryTicks(categories);
            configure?.Invoke(new DumbbellParts(connector, fromDots, toDots));
            return axes;
        }

        /// <summary>Draws a Pareto chart: the counts of the categories as bars, largest first, and the running share of the total
        /// as a marked line on a second Y axis, one point on the centre of each bar, so the few categories that make up most of
        /// the total stand out on the left.
        /// <para>The bars are sorted by count, largest first; equal counts keep the order they were given in, so a tie never
        /// reshuffles between runs or platforms. The line's point on a bar is the share of the total that bar and every bar left
        /// of it hold, in percent. The total is the last running sum, so the last point is exactly 100.</para>
        /// <para>The two Y axes are one scale: the left runs from 0 to 1.05 times the total, the right from 0 to 105 %, so a count
        /// on the left reads its share of the total on the right, the top of the first bar is the first point of the line, and
        /// the 100 % point is drawn whole below the top of the plot. Set a limit of your own after the call to change either;
        /// the X axis is left to the bars.</para>
        /// <para>A colour-coded dual axis: the bars are blue and the line orange (the default theme's first two colours, a pair
        /// that stays apart under the common forms of colour-vision deficiency), and each Y axis wears its series' colour and is
        /// titled with its series' name ("Count" and "Cumulative %"), as the series stand after <paramref name="configure"/>:
        /// recolour or rename a part there and its axis follows. A part left without a colour of its own takes the theme's colour
        /// and leaves its axis uncoloured; a part left without a name leaves its axis untitled. The X axis belongs to both series
        /// and takes neither colour. The legend stands right of centre, between the short bars of the tail and the line, which
        /// runs near 100 % there.</para>
        /// <para>A missing count (NaN) cannot be ranked: its category goes last and keeps its name on the axis, but has no bar and
        /// no point, and it adds nothing to the total.</para>
        /// <para>The chart is its parts: written to JSON it is a bar series and a line series, and read back it draws the same
        /// picture. It is not sorted or accumulated again; change the counts and call the verb again.</para></summary>
        /// <param name="categories">The categories' names, in any order; they are drawn in the order of their counts.</param>
        /// <param name="counts">How often each category occurs, one count per category, in the same order: a finite number, 0 or
        /// more, or NaN for a count that is missing.</param>
        /// <param name="configure">Optional styling of the two series, run once after the verb has styled them, so what it sets
        /// wins; each Y axis then takes the colour and the name its series has.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="categories"/> or
        /// <paramref name="counts"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="counts"/> does not carry one count per category, a count is
        /// negative or infinite, or the counts do not add up to a finite number above 0 (no counts, only zeros, only missing
        /// counts, or a sum too large for a <see cref="double"/>).</exception>
        /// <example>
        /// <code>
        /// string[] defects = ["Dents", "Scratches", "Stains", "Cracks", "Warp"];
        /// double[] counts = [98, 142, 45, 67, 9];
        ///
        /// string svg = Plt.Create()
        ///     .WithTitle("Defects by kind")
        ///     .AddSubPlot(1, 1, 1, ax => ax
        ///         .Pareto(defects, counts, parts => parts.Bars.Label = "Defects")
        ///         .SetXLabel("Kind of defect"))
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder Pareto(string[] categories, double[] counts,
            Action<ParetoParts>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);
            ArgumentNullException.ThrowIfNull(categories);
            counts.ThrowIfMisaligned(categories.Length, nameof(counts));
            ThrowIfNotCounts(categories, counts);

            int[] order = counts.DescendingOrder();
            double[] sorted = [.. order.Select(i => counts[i])];
            double[] running = RunningSums(sorted);
            // The missing counts come last and add nothing, so the total is the last running sum that is not NaN (0 when there
            // is none); a sum too large for a double is infinite and refused with it.
            double total = running.LastOrDefault(r => !double.IsNaN(r), 0);
            if (!(total > 0) || double.IsInfinity(total))
            {
                throw new ArgumentException(
                    $"The counts add up to {total.ToString(CultureInfo.InvariantCulture)}; a Pareto chart draws each count as a share of the total, which must be a finite number above 0.",
                    nameof(counts));
            }

            // The total IS the last running sum, so the last point is that sum over itself: exactly 100. A missing count keeps
            // NaN, and a NaN point is not drawn.
            double[] cumulative = [.. running.Select(r => r / total * 100)];

            var bars = new BarSeries([.. order.Select(i => categories[i])], sorted)
            {
                Color = ParetoBarColor, Label = ParetoBarLabel,
            };
            LineSeries line = null!;
            axes.AddSeries(bars)
                .SetYLim(0, total * ParetoHeadroom)
                .WithSecondaryYAxis(y2 => y2
                    .Plot(SlotCentres(categories.Length), cumulative, s =>
                    {
                        line = s;
                        s.Color = ParetoLineColor; s.LineWidth = 2; s.Marker = MarkerStyle.Circle; s.MarkerSize = 7;
                        s.Label = ParetoLineLabel;
                    })
                    .SetYLim(0, 100 * ParetoHeadroom))
                .WithLegend(LegendPosition.Right);

            configure?.Invoke(new ParetoParts(bars, line));

            // Each Y axis wears its series' colour and name as the callback left them.
            if (bars.Color is { } barColor) axes.SetYAxisColor(barColor);
            if (bars.Label is { } barLabel) axes.SetYLabel(barLabel);
            axes.WithSecondaryYAxis(y2 =>
            {
                if (line.Color is { } lineColor) y2.SetYAxisColor(lineColor);
                if (line.Label is { } lineLabel) y2.SetYLabel(lineLabel);
            });
            return axes;
        }

        /// <summary>Draws a slope graph: for each category a line between two moments, where it stood and where it stands, so the
        /// direction and the steepness of every change show at a glance, with the two moments named on the X axis.
        /// <para>Each category is ONE <see cref="LineSeries"/> with a marker at each end, wearing one colour of the theme's cycle.
        /// Its name stands at both ends: right of the end point, in the line's colour, as an end label that replaces the legend
        /// entry (so the chart needs no legend), and left of the start point, in the theme's text colour, as an annotation.</para>
        /// <para>A missing value (NaN) leaves a gap, never a bridge, and takes the name of the point that is missing with it:
        /// a category without a start has no start name, and a category without an end has no end name at the line end. The
        /// point that is there is still drawn, as a marker with no line, and keeps its own name.</para>
        /// <para>The chart draws no legend, because its names stand at the ends of the lines. A <c>WithLegend</c> after the call
        /// switches it on again, for the names a callback moved to the legend and for a category without an end, whose name has
        /// no end label to stand at.</para>
        /// <para>The verb sets no axis limits; a limit of your own, set before or after the call, stays yours. It does set the X
        /// margin to a quarter of the distance between the two columns, because the start names stand in the plot to the left of
        /// the first column and the usual margin is too narrow to hold them clear of the Y axis' tick labels. A margin of your own
        /// set after the call replaces it; a long name needs a wider one.</para></summary>
        /// <param name="categories">The categories' names, one line each, in the order they take colours.</param>
        /// <param name="from">The first moment: its <see cref="BarGroup.Label"/> names the left column on the X axis, and its
        /// <see cref="BarGroup.Values"/> are the start of each category's line, one value per category.</param>
        /// <param name="to">The second moment: its <see cref="BarGroup.Label"/> names the right column on the X axis, and its
        /// <see cref="BarGroup.Values"/> are the end of each category's line, one value per category.</param>
        /// <param name="configure">Optional styling, run once for each line, in the order of the categories, after the verb has
        /// built it, so what it sets wins: the colour, the width, the marker, or putting the name back in the legend.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="categories"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">A moment does not carry one value per category; the message names the moment and
        /// the parameter.</exception>
        /// <example>
        /// <code>
        /// string[] regions = ["North", "South", "East", "West"];
        ///
        /// string svg = Plt.Create()
        ///     .WithTitle("Sales by region")
        ///     .AddSubPlot(1, 1, 1, ax => ax
        ///         .SlopeGraph(regions,
        ///             new BarGroup("2015", [42.0, 55.0, 38.0, 61.0]),
        ///             new BarGroup("2025", [51.0, 49.0, 44.0, 66.0]))
        ///         .SetYLabel("Sales"))
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder SlopeGraph(string[] categories, BarGroup from, BarGroup to,
            Action<LineSeries>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);
            ArgumentNullException.ThrowIfNull(categories);
            from.ThrowIfMisaligned(categories.Length, nameof(from));
            to.ThrowIfMisaligned(categories.Length, nameof(to));

            // The two moments are two categories: each stands on the centre of its slot, where the tick that names it stands.
            double[] columns = SlotCentres(2);
            for (int k = 0; k < categories.Length; k++)
            {
                double start = from.Values[k], end = to.Values[k];
                var line = new LineSeries([.. columns], [start, end])
                {
                    LineWidth = SlopeLineWidth, Marker = MarkerStyle.Circle, MarkerSize = SlopeMarkerSize, Label = categories[k],
                    // The end label names the point the line ends at. A line whose end is missing would be named at its start's
                    // height, beside nothing, so it keeps its name for the legend instead.
                    LabelPlacement = double.IsFinite(end) ? SeriesLabelPlacement.LineEnd : SeriesLabelPlacement.Legend,
                };
                axes.AddSeries(line, configure);

                // The start name is an annotation with no colour of its own, so the theme's text colour decides it; an annotation
                // at a start that is missing is not drawn, and is not added.
                if (double.IsFinite(start))
                {
                    axes.Annotate(categories[k], columns[0] - SlopeNameGap, start, a => a.Alignment = TextAlignment.Right);
                }
            }

            // The names stand at the ends of the lines, so the chart needs no legend; one would otherwise appear for a category
            // whose end is missing, which has no end name, and cover the plot for a name its start already gives.
            return axes
                .SetXMargin(SlopeSideMargin)
                .SetXCategoryTicks([from.Label ?? string.Empty, to.Label ?? string.Empty])
                .WithLegend(visible: false);
        }

        /// <summary>Draws a dot plot (a Cleveland dot plot): the categories down the Y axis, ordered by the first group with the
        /// largest on top, and for each group a dot on every category's row at the group's value, so the values compare along a
        /// shared scale and the groups compare within a row.
        /// <para>Each group is ONE <see cref="ScatterSeries"/> of circles, labelled with the group's name and wearing one colour of
        /// the theme's cycle. The categories are sorted by the first group's values, largest first; equal values keep the order
        /// they were given in, so a tie never reshuffles between runs or platforms, and a category whose first value is missing
        /// (NaN) goes last, at the bottom. The other groups follow that order. The categories' names are the Y axis' ticks.</para>
        /// <para>The guide lines that lead the eye from a name to its dots are the grid, drawn along the Y axis (one line
        /// through each category), not lines of their own, so they follow the theme and can be restyled with
        /// <c>WithGrid</c> after the call. A missing value (NaN) leaves that dot out and nothing else.</para>
        /// <para>The verb sets no axis limits; a limit of your own, set before or after the call, stays yours. The legend names
        /// the groups and stands in the lower right corner: the order of the rows puts the largest values at the top right and
        /// the smallest at the bottom left, so the lower right is the corner the dots leave empty, where the default corner (upper
        /// right) would cover the largest dots. A <c>WithLegend</c> after the call moves it or switches it off.</para></summary>
        /// <param name="categories">The categories' names, in any order; they are drawn in the order of the first group's
        /// values.</param>
        /// <param name="groups">The groups to draw, one set of dots each, in the order they take colours. A group's
        /// <see cref="BarGroup.Label"/> names its dots in the legend, and its <see cref="BarGroup.Values"/> are its value in each
        /// category: one value per category, in the order of <paramref name="categories"/>. The first group decides the order of
        /// the rows.</param>
        /// <param name="configure">Optional styling, run once for each group, in the order of the groups, after the verb has
        /// built it, so what it sets wins: the colour, the marker, its size.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="categories"/> or
        /// <paramref name="groups"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">No groups are given, or a group does not carry one value per category; the
        /// message names the group.</exception>
        /// <example>
        /// <code>
        /// string[] languages = ["Ruby", "Go", "Rust", "Java", "Python"];
        ///
        /// string svg = Plt.Create()
        ///     .WithTitle("Programming languages")
        ///     .AddSubPlot(1, 1, 1, ax => ax
        ///         .DotPlot(languages,
        ///         [
        ///             new BarGroup("2015", [31.0, 48.0, 62.0, 55.0, 80.0]),
        ///             new BarGroup("2025", [35.0, 52.0, 60.0, 58.0, 84.0]),
        ///         ])
        ///         .SetXLabel("Share of developers (%)"))
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder DotPlot(string[] categories, IReadOnlyList<BarGroup> groups,
            Action<ScatterSeries>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);
            ArgumentNullException.ThrowIfNull(categories);
            ArgumentNullException.ThrowIfNull(groups);
            if (groups.Count == 0)
            {
                throw new ArgumentException("No groups were given; a dot plot draws one set of dots for each group.", nameof(groups));
            }

            // Everything is checked before the first series is added, so a refused call leaves the axes as it was.
            foreach (var group in groups)
            {
                group.ThrowIfMisaligned(categories.Length, nameof(groups));
            }

            // The rows, largest first by the first group (a stable order, a missing value last). Row d stands in the slot
            // counted from the top, so the largest is on top of an axis that counts upward.
            int[] order = groups[0].Values.DescendingOrder();
            double[] rows = [.. Enumerable.Range(0, order.Length).Select(d => CategorySlot.Of(order.Length - 1 - d).Centre)];

            foreach (var group in groups)
            {
                var dots = new ScatterSeries([.. order.Select(i => group.Values[i])], [.. rows])
                {
                    Marker = MarkerStyle.Circle, MarkerSize = DotPlotDotSize, Label = group.Label,
                };
                axes.AddSeries(dots, configure);
            }

            // The guide lines are the theme's grid along the Y axis: one line through each category, drawn behind the dots. The
            // legend stands where the order leaves the plot empty: the default corner, upper right, is where the largest dots are.
            return axes
                .SetYCategoryTicks([.. order.Select(i => categories[i])], topDown: true)
                .WithGrid(g => g with { Visible = true, Axis = GridAxis.Y })
                .WithLegend(LegendPosition.LowerRight);
        }

        /// <summary>Draws parallel coordinates: one vertical axis for each dimension and one line for each row, crossing every
        /// axis at the row's value there, so rows that behave alike run together and a class that stands apart in some dimension
        /// shows where.
        /// <para>Rows that share a name are one class. A class is ONE <see cref="LineSeries"/> in one colour of the theme's cycle
        /// (the classes take the colours in the order their names first appear), named in the legend: its rows are stretches of
        /// that series with one missing point between two rows, so no line runs from the end of one row to the start of the next,
        /// and a click on the class's legend entry hides every row of it.</para>
        /// <para>Each column is scaled to 0..1 by its own smallest and largest value over ALL the rows, so dimensions in different
        /// units share one axis; a column whose values are all the same is drawn at the middle. A missing value (NaN) or an
        /// infinity leaves a gap in its row's line, never a bridge, and is no part of its column's range. The Y axis carries no
        /// ticks, because each column is scaled to 0..1 on its own and a number on it would read none of them; each dimension's
        /// own axis is the vertical line at its name, a solid line in the reference line's default grey. Axes that show each
        /// column's own units are not drawn.</para>
        /// <para>The dimension names are the X ticks, on the centres of the slots, as every verb that names categories puts them.
        /// The legend stands where the theme puts it, in the upper right corner unless told otherwise, over whatever lines run
        /// there: the lines of a parallel-coordinates chart fill the plot, so no corner is empty. <c>WithLegend</c> after the call
        /// moves it, for example <c>WithLegend(LegendPosition.OutsideRight)</c> under <c>TightLayout()</c> puts it beside the plot.
        /// The verb sets no axis limits; a limit of your own, set before or after the call, stays yours. The chart is its parts:
        /// written to JSON it is one line series for each class and one reference line for each dimension, and read back it draws
        /// the same picture; it is not scaled again, so change the data and call the verb again.</para></summary>
        /// <param name="dimensions">The dimensions' names, along the X axis in the order given, one axis for each.</param>
        /// <param name="rows">The rows to draw. A row's <see cref="BarGroup.Label"/> is its class, shared by every row of that class
        /// (a row with no name belongs to a class with no name, which has no legend entry), and its
        /// <see cref="BarGroup.Values"/> are its value in each dimension: one value per dimension, in the order of
        /// <paramref name="dimensions"/>, or NaN for a value that is missing.</param>
        /// <param name="configure">Optional styling, run once for each class, in the order the classes first appear, after the verb
        /// has built it, so what it sets wins: the colour, the line width, the line style.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dimensions"/> or
        /// <paramref name="rows"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">No dimensions or no rows are given; a row does not carry one value per dimension
        /// (the message names the row); or the values of a column span more than a <see cref="double"/> can hold, so they cannot be
        /// scaled to 0..1 (the message names the dimension).</exception>
        /// <example>
        /// <code>
        /// string[] measures = ["Sepal length", "Sepal width", "Petal length", "Petal width"];
        ///
        /// string svg = Plt.Create()
        ///     .WithTitle("Three species of iris")
        ///     .AddSubPlot(1, 1, 1, ax => ax
        ///         .ParallelCoordinates(measures,
        ///         [
        ///             new BarGroup("setosa", [5.1, 3.5, 1.4, 0.2]),
        ///             new BarGroup("setosa", [4.9, 3.0, 1.4, 0.2]),
        ///             new BarGroup("versicolor", [7.0, 3.2, 4.7, 1.4]),
        ///             new BarGroup("virginica", [6.3, 3.3, 6.0, 2.5]),
        ///         ]))
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder ParallelCoordinates(string[] dimensions, IReadOnlyList<BarGroup> rows,
            Action<LineSeries>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);
            ArgumentNullException.ThrowIfNull(dimensions);
            ArgumentNullException.ThrowIfNull(rows);
            if (dimensions.Length == 0)
            {
                throw new ArgumentException(
                    "No dimensions were given; parallel coordinates draw one axis for each dimension.", nameof(dimensions));
            }

            if (rows.Count == 0)
            {
                throw new ArgumentException("No rows were given; parallel coordinates draw one line for each row.", nameof(rows));
            }

            // Everything is checked before the first series is added, so a refused call leaves the axes as it was.
            foreach (var row in rows)
            {
                row.ThrowIfMisaligned(dimensions.Length, nameof(rows), "dimensions");
            }

            double[][] scaled = ScaleColumns(dimensions, rows);
            double[] columns = SlotCentres(dimensions.Length);

            // The solid axis lines take no colour of their own: the reference line's default grey is a fixed mid grey that stands
            // at 3 : 1 or more on a light and on a dark theme, which a grey fixed here would not do on both.
            foreach (double column in columns)
            {
                axes.AxVLine(column, line => line.LineStyle = LineStyle.Solid);
            }

            // One series per class, in the order the names first appear: every row of a class is a stretch of the one series, and
            // a missing point between two stretches breaks the line there, so one series draws all its polylines in one colour.
            foreach (var classRows in Enumerable.Range(0, rows.Count).GroupBy(i => rows[i].Label ?? string.Empty))
            {
                var x = new List<double>();
                var y = new List<double>();
                foreach (int row in classRows)
                {
                    if (x.Count > 0)
                    {
                        x.Add(double.NaN);
                        y.Add(double.NaN);
                    }

                    x.AddRange(columns);
                    y.AddRange(scaled[row]);
                }

                var line = new LineSeries([.. x], [.. y])
                {
                    LineWidth = ParallelLineWidth,
                    Label = classRows.Key.Length > 0 ? classRows.Key : null,
                };
                axes.AddSeries(line, configure);
            }

            // The columns are scaled to 0..1 on their own, so the Y axis has no scale to read: an empty fixed locator draws no tick,
            // no label and no grid line along it.
            return axes
                .SetXCategoryTicks([.. dimensions.Select(name => name ?? string.Empty)])
                .SetYTickLocator(new FixedLocator([]));
        }

        /// <summary>Draws a Marimekko chart (a market map): one column for each name, as wide as its share of the grand total,
        /// divided from the bottom up into one cell for each segment, as tall as the segment's share of that column. The columns
        /// fill the plot, so the area of every cell is its share of the whole: how big each column is and how it is made up show
        /// in one picture.
        /// <para>Each segment is ONE <see cref="AreaSeries"/> in one colour of the theme's cycle, named in the legend by its
        /// segment, and drawn as one rectangle in every column: two points per column, with a missing point between two columns,
        /// so no column is joined to the next. A click on a legend entry hides that segment in every column. The cells are opaque,
        /// as bars are, and each outlines its top in its own colour.</para>
        /// <para>The X axis runs from 0 to 100 percent of the grand total and names each column under its centre; the Y axis runs
        /// from 0 to 100 percent of each column's total and reads in percent. The verb sets the Y margin to 0, so the columns
        /// reach the top and the bottom of the plot as they reach its sides (a margin set after the call replaces it), and it sets
        /// no axis limits. The legend stands where the theme puts it, over the cells there, because the chart fills the plot and
        /// no corner is empty: <c>WithLegend(LegendPosition.OutsideRight)</c> under <c>TightLayout()</c> puts it beside the
        /// plot.</para>
        /// <para>A missing size (NaN) is an empty cell: it adds nothing to its column, so the column's width and its other cells'
        /// shares are taken over the sizes that are there, and the cell above it stands on the one below it. A size of 0 has no
        /// cell either. A column whose sizes are all 0 or missing has no share: it is 0 wide, draws no cell and carries no
        /// name.</para>
        /// <para>The chart is its parts: written to JSON it is one area series per segment with the named ticks and the percent
        /// axis, and read back it draws the same picture. It is not laid out again; change the sizes and call the verb
        /// again.</para></summary>
        /// <param name="columns">The columns' names, along the X axis in the order given, one column each.</param>
        /// <param name="segments">The segments, stacked in every column in the order given, the first at the bottom. A segment's
        /// <see cref="BarGroup.Label"/> names it in the legend (a segment with no name is drawn and has no entry), and its
        /// <see cref="BarGroup.Values"/> are its size in each column: one size per column, in the order of
        /// <paramref name="columns"/>, a finite number, 0 or more, or NaN for a size that is missing.</param>
        /// <param name="configure">Optional styling, run once for each segment, in the order of the segments, after the verb has
        /// built it, so what it sets wins: the colour, the outline, the transparency.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="columns"/> or
        /// <paramref name="segments"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">No columns or no segments are given; a segment does not carry one size per column
        /// (the message names the segment); a size is negative or infinite (the message names the segment and the column); or the
        /// sizes do not add up to a finite number above 0 (only zeros, only missing sizes, or a sum too large for a
        /// <see cref="double"/>).</exception>
        /// <example>
        /// <code>
        /// string[] channels = ["Retail", "Online", "Wholesale"];
        ///
        /// string svg = Plt.Create()
        ///     .WithTitle("Market by channel and brand")
        ///     .TightLayout()
        ///     .AddSubPlot(1, 1, 1, ax => ax
        ///         .Marimekko(channels,
        ///         [
        ///             new BarGroup("Brand A", [30, 15, 5]),
        ///             new BarGroup("Brand B", [20, 25, 10]),
        ///             new BarGroup("Brand C", [10, 20, 15]),
        ///         ])
        ///         .SetXLabel("Channel (width: its share of the market)")
        ///         .WithLegend(LegendPosition.OutsideRight))
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder Marimekko(string[] columns, IReadOnlyList<BarGroup> segments,
            Action<AreaSeries>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);
            ArgumentNullException.ThrowIfNull(columns);
            ArgumentNullException.ThrowIfNull(segments);
            if (columns.Length == 0)
            {
                throw new ArgumentException("No columns were given; a Marimekko chart divides its width among its columns.", nameof(columns));
            }

            if (segments.Count == 0)
            {
                throw new ArgumentException("No segments were given; a Marimekko chart draws one area for each segment.", nameof(segments));
            }

            // Everything is checked before the first series is added, so a refused call leaves the axes as it was.
            foreach (var segment in segments)
            {
                segment.ThrowIfMisaligned(columns.Length, nameof(segments), "columns");
            }

            ThrowIfNotSizes(columns, segments);

            // A column's total is the running sum of its sizes in the order of the segments (a missing size adds nothing), so the
            // top of its last cell is that sum over itself: exactly 100. The edges are the running sums of the column totals over
            // the grand total, which is the last of them, so the last edge is exactly 100 too.
            int n = columns.Length;
            double[] totals = new double[n];
            for (int c = 0; c < n; c++)
            {
                foreach (var segment in segments)
                {
                    if (!double.IsNaN(segment.Values[c])) totals[c] += segment.Values[c];
                }
            }

            double[] edges = new double[n + 1];
            for (int c = 0; c < n; c++) edges[c + 1] = edges[c] + totals[c];
            double grand = edges[n];
            if (!(grand > 0) || double.IsInfinity(grand))
            {
                throw new ArgumentException(
                    $"The sizes add up to {grand.ToString(CultureInfo.InvariantCulture)}; a Marimekko chart draws each column as its share of the total, which must be a finite number above 0.",
                    nameof(segments));
            }

            for (int c = 0; c <= n; c++) edges[c] = edges[c] / grand * WholeInPercent;

            // One area per segment: two points per column with a missing point between two columns, so each cell is one
            // rectangle and no column is joined to the next. A cell with no height (a size of 0 or a missing one) is missing too:
            // no fill, and no outline across its column.
            double[] floors = new double[n];
            foreach (var segment in segments)
            {
                double[] x = new double[3 * n - 1], top = new double[3 * n - 1], bottom = new double[3 * n - 1];
                for (int c = 0; c < n; c++)
                {
                    int at = 3 * c;
                    if (c > 0)
                    {
                        x[at - 1] = top[at - 1] = bottom[at - 1] = double.NaN;
                    }

                    double size = segment.Values[c];
                    bool drawn = size > 0;
                    double low = drawn ? floors[c] / totals[c] * WholeInPercent : double.NaN;
                    if (drawn) floors[c] += size;
                    double high = drawn ? floors[c] / totals[c] * WholeInPercent : double.NaN;
                    x[at] = edges[c];
                    x[at + 1] = edges[c + 1];
                    top[at] = top[at + 1] = high;
                    bottom[at] = bottom[at + 1] = low;
                }

                var cells = new AreaSeries(x, top)
                {
                    YData2 = bottom, Alpha = MarimekkoAlpha, Label = string.IsNullOrEmpty(segment.Label) ? null : segment.Label,
                };
                axes.AddSeries(cells, configure);
            }

            // A column with no share has no width to stand under, so it carries no name; the others are named under their
            // centres. The cells fill 0..100 % on both axes: the area's X edges are sticky, and no Y margin keeps the Y axis on
            // the cells too.
            int[] named = [.. Enumerable.Range(0, n).Where(c => totals[c] > 0)];
            return axes
                .SetXCategoryTicks([.. named.Select(c => columns[c] ?? string.Empty)], [.. named.Select(c => (edges[c] + edges[c + 1]) / 2)])
                .SetYTickFormatter(new PercentFormatter(WholeInPercent))
                .SetYMargin(0);
        }
    }

    /// <summary>Scales every column of <paramref name="rows"/> to 0..1 by its smallest and largest drawable value over ALL the
    /// rows. A value that cannot be drawn (NaN, an infinity) stays missing and is no part of the range; a column whose drawable
    /// values are all the same has no range and sits at the middle.</summary>
    /// <returns>The scaled values, one array per row with one entry for each dimension.</returns>
    /// <exception cref="ArgumentException">The values of a column span more than a <see cref="double"/> can hold.</exception>
    private static double[][] ScaleColumns(string[] dimensions, IReadOnlyList<BarGroup> rows)
    {
        double[][] scaled = [.. rows.Select(_ => new double[dimensions.Length])];
        for (int d = 0; d < dimensions.Length; d++)
        {
            double[] column = [.. rows.Select(row => row.Values[d])];
            // A column with nothing to draw has no range; every value of it is then missing and the span is never read.
            var range = column.DrawableRange() ?? new Range1D(0, 0);
            double span = range.Width;
            if (double.IsInfinity(span))
            {
                throw new ArgumentException(
                    $"The values of '{dimensions[d]}' span more than a double can hold; a column is scaled to 0..1 by its range.",
                    nameof(rows));
            }

            for (int i = 0; i < column.Length; i++)
            {
                scaled[i][d] = !column[i].IsDrawable() ? double.NaN
                    : span > 0 ? (column[i] - range.Lo) / span
                    : MiddleOfAColumn;
            }
        }

        return scaled;
    }

    /// <summary>Refuses a count that is negative or infinite: a Pareto chart counts occurrences, and a negative count has no
    /// share of a total while an infinite one leaves every other count a share of 0. NaN is a missing count, not a bad one.</summary>
    /// <exception cref="ArgumentException">A count is negative or infinite; the message names its category.</exception>
    private static void ThrowIfNotCounts(string[] categories, double[] counts)
    {
        for (int i = 0; i < counts.Length; i++)
        {
            double count = counts[i];
            if (count < 0 || double.IsPositiveInfinity(count))
            {
                throw new ArgumentException(
                    $"The count of '{categories[i]}' is {count.ToString(CultureInfo.InvariantCulture)}; a count is a finite number of occurrences, 0 or more (NaN marks a missing count).",
                    nameof(counts));
            }
        }
    }

    /// <summary>Refuses a size that is negative or infinite: a Marimekko chart draws every size as a share of its column and
    /// of the whole, and a negative size has no share while an infinite one leaves every other size a share of 0. NaN is a
    /// missing size, not a bad one.</summary>
    /// <exception cref="ArgumentException">A size is negative or infinite; the message names its segment and its
    /// column.</exception>
    private static void ThrowIfNotSizes(string[] columns, IReadOnlyList<BarGroup> segments)
    {
        foreach (var segment in segments)
        {
            for (int c = 0; c < columns.Length; c++)
            {
                double size = segment.Values[c];
                if (size < 0 || double.IsPositiveInfinity(size))
                {
                    throw new ArgumentException(
                        $"The size of '{segment.Label}' in '{columns[c]}' is {size.ToString(CultureInfo.InvariantCulture)}; a size is finite and 0 or more (NaN marks a missing size).",
                        nameof(segments));
                }
            }
        }
    }

    /// <summary>The running sum of <paramref name="counts"/>: entry <c>k</c> is the sum of the counts up to and including
    /// <c>k</c>. A missing count (NaN) adds nothing and keeps NaN as its own entry.</summary>
    private static double[] RunningSums(double[] counts)
    {
        double sum = 0;
        return [.. counts.Select(c => double.IsNaN(c) ? double.NaN : sum += c)];
    }

    /// <summary>How far past the total the left axis of a Pareto chart reaches, and past 100 % the right one: 5 %, so the two
    /// axes are one scale and the 100 % point is drawn whole below the top of the plot (a set limit clips at the frame).</summary>
    private const double ParetoHeadroom = 1.05;

    /// <summary>The name of a Pareto chart's bars: their legend entry and the title of the left axis.</summary>
    private const string ParetoBarLabel = "Count";

    /// <summary>The name of a Pareto chart's line: its legend entry and the title of the right-hand axis.</summary>
    private const string ParetoLineLabel = "Cumulative %";

    /// <summary>The bars' colour, and the left axis': the default theme's first colour.</summary>
    private static readonly Color ParetoBarColor = Colors.Tab10Blue;

    /// <summary>The line's colour, and the right-hand axis': the default theme's second colour.</summary>
    private static readonly Color ParetoLineColor = Colors.Tab10Orange;

    /// <summary>The size of the dots of a dot plot, in points squared (a scatter's marker area): about 11 pixels across, big
    /// enough to compare by position and small enough that the two dots of a close pair stay apart.</summary>
    private const double DotPlotDotSize = 50;

    /// <summary>The width of a slope graph's lines, in points.</summary>
    private const double SlopeLineWidth = 2;

    /// <summary>The size of the markers a slope graph puts at both ends of its lines, in points.</summary>
    private const double SlopeMarkerSize = 8;

    /// <summary>How far left of its start point a slope graph's start name ends, as a fraction of the distance between the two
    /// columns: far enough to clear the marker (a radius of 4 pixels) at the sizes a figure has.</summary>
    private const double SlopeNameGap = 0.04;

    /// <summary>The X margin of a slope graph, as a fraction of the distance between the two columns, on each side. The start
    /// names stand in the plot area left of the first column, and the theme's usual 5 % leaves them a few pixels, so the
    /// names would run across the Y axis' tick labels; a quarter holds a name of about a dozen characters.</summary>
    private const double SlopeSideMargin = 0.25;

    /// <summary>The size of the dots of a dumbbell chart, in points squared (a scatter's marker area).</summary>
    private const double DumbbellDotSize = 14;

    /// <summary>The width of a parallel-coordinates line, in points: thin, so that many rows stay readable where they run
    /// together.</summary>
    private const double ParallelLineWidth = 1.2;

    /// <summary>Where a column without a range is drawn, on the scale of 0..1: the middle.</summary>
    private const double MiddleOfAColumn = 0.5;

    /// <summary>The whole, in percent: a Marimekko chart's widths are percent of the grand total and its heights percent of
    /// a column's total.</summary>
    private const double WholeInPercent = 100;

    /// <summary>The transparency of a Marimekko chart's cells: opaque, as bars are, so a cell wears exactly the colour of its
    /// legend entry (an area is faint, 0.3, by default).</summary>
    private const double MarimekkoAlpha = 1;

    /// <summary>The centre of each category's slot along the axis: where the marks of a category stand.</summary>
    private static double[] SlotCentres(int categoryCount) =>
        [.. Enumerable.Range(0, categoryCount).Select(k => CategorySlot.Of(k).Centre)];
}
