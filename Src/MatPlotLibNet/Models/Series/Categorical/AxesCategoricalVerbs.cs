// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The categorical-family verbs of <see cref="Axes"/> (<c>Bar</c>, <c>Hist</c>, <c>Gantt</c>, …), each
/// constructing one series and handing it to <see cref="Axes.AddSeries{T}"/>.</summary>
public static class AxesCategoricalVerbs
{
    extension(Axes axes)
    {
        /// <summary>Adds a vertical bar series from the given categories and values.</summary>
        /// <param name="categories">The category labels for each bar.</param>
        /// <param name="values">The numeric values for each bar.</param>
        /// <returns>The newly created <see cref="BarSeries"/> for further configuration.</returns>
        public BarSeries Bar(string[] categories, double[] values)
        {
            categories.Length.ThrowIfMisaligned(values.Length);
            return axes.AddSeries(new BarSeries(categories, values));
        }

        /// <summary>Adds a horizontal bar series from the given categories and values.</summary>
        /// <param name="categories">The category labels for each bar.</param>
        /// <param name="values">The numeric values for each bar.</param>
        /// <returns>The newly created <see cref="BarSeries"/> with horizontal orientation.</returns>
        public BarSeries Barh(string[] categories, double[] values)
        {
            categories.Length.ThrowIfMisaligned(values.Length);
            return axes.AddSeries(new BarSeries(categories, values) { Orientation = BarOrientation.Horizontal });
        }

        /// <summary>Adds a histogram series from the given data array.</summary>
        /// <param name="data">The raw data values to bin into a histogram.</param>
        /// <param name="bins">The number of histogram bins.</param>
        /// <returns>The newly created <see cref="HistogramSeries"/> for further configuration.</returns>
        public HistogramSeries Hist(double[] data, int bins = 10) => axes.AddSeries(new HistogramSeries(data) { Bins = bins });

        /// <summary>Adds a waterfall chart showing cumulative positive and negative changes.</summary>
        public WaterfallSeries Waterfall(string[] categories, double[] values)
        {
            categories.Length.ThrowIfMisaligned(values.Length);
            return axes.AddSeries(new WaterfallSeries(categories, values));
        }

        /// <summary>Adds a funnel chart showing progressive reduction through stages.</summary>
        public FunnelSeries Funnel(string[] labels, double[] values)
        {
            labels.Length.ThrowIfMisaligned(values.Length);
            return axes.AddSeries(new FunnelSeries(labels, values));
        }

        /// <summary>Adds a progress bar showing a value as a fraction (0.0 to 1.0).</summary>
        public ProgressBarSeries ProgressBar(double value) => axes.AddSeries(new ProgressBarSeries(value));

        /// <summary>Adds a single-value stat tile (a big headline number + label) filling the plot area.</summary>
        public StatTileSeries StatTile(double value) => axes.AddSeries(new StatTileSeries(value));

        /// <summary>Adds a bullet graph — a measure, an optional target tick, and optional qualitative bands.</summary>
        /// <param name="value">The measure.</param>
        /// <returns>The created series.</returns>
        public BulletGraphSeries Bullet(double value) => axes.AddSeries(new BulletGraphSeries(value));

        /// <summary>Adds a single-row discrete-state timeline of coloured segments along the X axis.</summary>
        /// <param name="segments">Ordered state segments; each defines a coloured rectangle spanning
        /// <c>[Start, End]</c> in data units with a centred label.</param>
        public StateTimelineSeries StateTimeline(IReadOnlyList<StateSegment> segments) => axes.AddSeries(new StateTimelineSeries(segments));

        /// <summary>Adds a Gantt chart showing task durations on a timeline.</summary>
        public GanttSeries Gantt(string[] tasks, double[] starts, double[] ends)
        {
            tasks.Length.ThrowIfMisaligned(starts.Length);
            tasks.Length.ThrowIfMisaligned(ends.Length);
            return axes.AddSeries(new GanttSeries(tasks, starts, ends));
        }

        /// <summary>Adds an event plot series showing event positions as vertical tick lines per row.</summary>
        /// <param name="positions">An array of event position sets, one per row.</param>
        /// <returns>The newly created <see cref="EventplotSeries"/> for further configuration.</returns>
        public EventplotSeries Eventplot(double[][] positions) => axes.AddSeries(new EventplotSeries(positions));

        /// <summary>Adds a broken bar series showing horizontal bars with gaps per row.</summary>
        /// <param name="ranges">An array of <see cref="BarRange"/> sets, one per row.</param>
        /// <returns>The newly created <see cref="BrokenBarSeries"/> for further configuration.</returns>
        public BrokenBarSeries BrokenBarH(BarRange[][] ranges) => axes.AddSeries(new BrokenBarSeries(ranges));

        /// <summary>Adds a count plot series showing the frequency of raw categorical values as bars.</summary>
        /// <param name="values">The raw categorical values to count and display.</param>
        /// <returns>The newly created <see cref="CountSeries"/> for further configuration.</returns>
        public CountSeries Countplot(string[] values) => axes.AddSeries(new CountSeries(values));

        /// <summary>Adds a point plot series showing mean ± confidence interval per category.</summary>
        /// <param name="datasets">An array of datasets, one per category.</param>
        /// <returns>The newly created <see cref="PointplotSeries"/> for further configuration.</returns>
        public PointplotSeries Pointplot(double[][] datasets) => axes.AddSeries(new PointplotSeries(datasets));
    }
}
