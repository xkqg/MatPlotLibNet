// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The circular-family verbs of <see cref="Axes"/> (<c>Pie</c>, <c>Donut</c>, <c>Gauge</c>, <c>Radar</c>),
/// each constructing one series and handing it to <see cref="Axes.AddSeries{T}"/>.</summary>
public static class AxesCircularVerbs
{
    extension(Axes axes)
    {
        /// <summary>Adds a pie chart series from the given slice sizes.</summary>
        /// <param name="sizes">The numeric sizes of each pie slice.</param>
        /// <param name="labels">Optional labels for each pie slice.</param>
        /// <returns>The newly created <see cref="PieSeries"/> for further configuration.</returns>
        public PieSeries Pie(double[] sizes, string[]? labels = null) => axes.AddSeries(new PieSeries(sizes) { Labels = labels });

        /// <summary>Adds a donut chart series.</summary>
        public DonutSeries Donut(double[] sizes, string[]? labels = null) => axes.AddSeries(new DonutSeries(sizes) { Labels = labels });

        /// <summary>Adds a gauge (speedometer) chart displaying a single value within a range.</summary>
        public GaugeSeries Gauge(double value) => axes.AddSeries(new GaugeSeries(value));

        /// <summary>Adds a radar (spider) chart series from the given categories and values.</summary>
        public RadarSeries Radar(string[] categories, double[] values)
        {
            categories.Length.ThrowIfMisaligned(values.Length);
            return axes.AddSeries(new RadarSeries(categories, values));
        }
    }
}
