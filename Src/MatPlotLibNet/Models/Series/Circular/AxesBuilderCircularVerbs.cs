// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The verbs of the axes builder that add a series of the circular family (a pie, a donut, a waffle, a gauge, a
/// radar): each adds the series through the axes verb of the same name (<see cref="AxesCircularVerbs"/>) and hands it to the
/// caller's callback.</summary>
public static class AxesBuilderCircularVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Adds a waffle chart to the axes: a grid of square cells (<see cref="WaffleSeries.RestingRows"/> by
        /// <see cref="WaffleSeries.RestingColumns"/> unless <c>configure</c> says otherwise) filled by category in proportion to its share
        /// of the total, so "37 of every 100" is thirty-seven squares a reader can count.</summary>
        /// <param name="categories">The categories' names.</param>
        /// <param name="values">The size of each category, one per category; only the proportions matter.</param>
        /// <param name="configure">Optional configuration callback.</param>
        /// <returns>This builder.</returns>
        public AxesBuilder Waffle(string[] categories, double[] values, Action<WaffleSeries>? configure = null)
            => axes.Configured(axes.Axes.Waffle(categories, values), configure);

        /// <summary>Adds a pie series to the axes.</summary>
        public AxesBuilder Pie(double[] sizes, string[]? labels = null, Action<PieSeries>? configure = null)
            => axes.Configured(axes.Axes.Pie(sizes, labels), configure);

        /// <summary>Adds a gauge (speedometer) chart to the axes.</summary>
        public AxesBuilder Gauge(double value, Action<GaugeSeries>? configure = null)
            => axes.Configured(axes.Axes.Gauge(value), configure);

        /// <summary>Adds a donut chart series to the axes.</summary>
        public AxesBuilder Donut(double[] sizes, string[]? labels = null, Action<DonutSeries>? configure = null)
            => axes.Configured(axes.Axes.Donut(sizes, labels), configure);

        /// <summary>Adds a radar (spider) chart series to the axes.</summary>
        public AxesBuilder Radar(string[] categories, double[] values, Action<RadarSeries>? configure = null)
            => axes.Configured(axes.Axes.Radar(categories, values), configure);
    }
}
