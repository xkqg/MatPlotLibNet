// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The verbs of the axes builder that add a series of the categorical family (bars, a histogram, a waterfall, a Gantt
/// chart, a count): each adds the series through the axes verb of the same name (<see cref="AxesCategoricalVerbs"/>) and hands
/// it to the caller's callback; and how the bars of several series share a category.</summary>
public static class AxesBuilderCategoricalVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Sets the bar mode (grouped or stacked) for multiple bar series.</summary>
        public AxesBuilder SetBarMode(BarMode mode) { axes.Axes.BarMode = mode; return axes; }

        /// <summary>Adds a bar series to the axes.</summary>
        public AxesBuilder Bar(string[] categories, double[] values, Action<BarSeries>? configure = null)
            => axes.Configured(axes.Axes.Bar(categories, values), configure);

        /// <summary>Adds several bars per category in one call: one series per group, each labelled with the
        /// group's name, drawn side by side inside every category.
        /// <para>The groups are an ordered list, not a map. The order is the picture — it decides which bar sits
        /// where in each category and which colour it takes from the theme's cycle — and a map has no order to give.
        /// <paramref name="configure"/> runs on every one of them, so set the orientation, the alpha or the hatch
        /// once rather than per group.</para></summary>
        /// <param name="categories">The categories along the axis, shared by every group.</param>
        /// <param name="groups">The groups, in the order they should be drawn. At least one, and each must carry
        /// exactly one value per category.</param>
        /// <param name="configure">Optional configuration applied to every series.</param>
        /// <returns>This builder for chaining.</returns>
        /// <exception cref="ArgumentException">No groups, or a group whose value count does not match the
        /// categories — named, so the caller knows which one.</exception>
        public AxesBuilder GroupedBar(string[] categories, IReadOnlyList<BarGroup> groups,
            Action<BarSeries>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(categories);
            ArgumentNullException.ThrowIfNull(groups);
            groups.ThrowIfEmpty("A grouped bar chart needs at least one group.");

            groups.ThrowIfNotOneEach(categories.Length, nameof(groups));

            foreach (var group in groups)
            {
                axes.Bar(categories, group.Values, series =>
                {
                    series.Label = group.Label;
                    configure?.Invoke(series);
                });
            }

            return axes;
        }

        /// <summary>Adds a histogram series to the axes.</summary>
        public AxesBuilder Hist(double[] data, int bins = HistogramSeries.RestingBins, Action<HistogramSeries>? configure = null)
            => axes.Configured(axes.Axes.Hist(data, bins), configure);

        /// <summary>Adds a progress bar to the axes.</summary>
        public AxesBuilder ProgressBar(double value, Action<ProgressBarSeries>? configure = null)
            => axes.Configured(axes.Axes.ProgressBar(value), configure);

        /// <summary>Adds a single-value stat tile (a big headline number + label) to the axes.</summary>
        public AxesBuilder StatTile(double value, Action<StatTileSeries>? configure = null)
            => axes.Configured(axes.Axes.StatTile(value), configure);

        /// <summary>Adds a bullet graph to the axes — Few's replacement for the radial dial: a measure, an
        /// optional target tick, and optional qualitative bands in one thin strip.</summary>
        /// <param name="value">The measure.</param>
        /// <param name="configure">Optional configuration of the created series.</param>
        /// <returns>This builder for chaining.</returns>
        public AxesBuilder Bullet(double value, Action<BulletGraphSeries>? configure = null)
            => axes.Configured(axes.Axes.Bullet(value), configure);

        /// <summary>Adds a single-row discrete-state timeline of coloured segments along the X axis.</summary>
        /// <param name="segments">Ordered state segments; each defines a coloured rectangle spanning
        /// <c>[Start, End]</c> in data units with a centred label.</param>
        /// <param name="configure">Optional post-construction configurator.</param>
        public AxesBuilder StateTimeline(IReadOnlyList<StateSegment> segments, Action<StateTimelineSeries>? configure = null)
            => axes.Configured(axes.Axes.StateTimeline(segments), configure);

        /// <summary>Adds a waterfall chart series to the axes: each value is a change on the running total, and a step marked
        /// in <see cref="WaterfallSeries.TotalIndices"/> (set in <paramref name="configure"/>) is a total, a level that stands
        /// on zero in <see cref="WaterfallSeries.TotalColor"/>.</summary>
        public AxesBuilder Waterfall(string[] categories, double[] values, Action<WaterfallSeries>? configure = null)
            => axes.Configured(axes.Axes.Waterfall(categories, values), configure);

        /// <summary>Adds a funnel chart series to the axes.</summary>
        public AxesBuilder Funnel(string[] labels, double[] values, Action<FunnelSeries>? configure = null)
            => axes.Configured(axes.Axes.Funnel(labels, values), configure);

        /// <summary>Adds a Gantt chart series to the axes.</summary>
        public AxesBuilder Gantt(string[] tasks, double[] starts, double[] ends, Action<GanttSeries>? configure = null)
            => axes.Configured(axes.Axes.Gantt(tasks, starts, ends), configure);

        /// <summary>Adds an event plot series to the axes.</summary>
        public AxesBuilder Eventplot(double[][] positions, Action<EventplotSeries>? configure = null)
            => axes.Configured(axes.Axes.Eventplot(positions), configure);

        /// <summary>Adds a broken-bar series to the axes (matplotlib <c>broken_barh</c>).</summary>
        /// <param name="ranges">One <see cref="BarRange"/> array per row; each inner entry defines a
        /// horizontal segment by <c>(Start, Width)</c> in data units.</param>
        /// <param name="configure">Optional post-construction configurator (colour, labels, bar height).</param>
        public AxesBuilder BrokenBarH(BarRange[][] ranges, Action<BrokenBarSeries>? configure = null)
            => axes.Configured(axes.Axes.BrokenBarH(ranges), configure);

        /// <summary>Adds a count plot series to the axes.</summary>
        public AxesBuilder Countplot(string[] values, Action<CountSeries>? configure = null)
            => axes.Configured(axes.Axes.Countplot(values), configure);

        /// <summary>Adds a point plot series to the axes.</summary>
        public AxesBuilder Pointplot(double[][] datasets, Action<PointplotSeries>? configure = null)
            => axes.Configured(axes.Axes.Pointplot(datasets), configure);
    }
}
