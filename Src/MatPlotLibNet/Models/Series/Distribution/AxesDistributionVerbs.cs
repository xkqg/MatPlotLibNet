// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The distribution-family verbs of <see cref="Axes"/> (<c>BoxPlot</c>, <c>Violin</c>, <c>Kde</c>, …), each
/// constructing one series and handing it to <see cref="Axes.AddSeries{T}"/>.</summary>
public static class AxesDistributionVerbs
{
    extension(Axes axes)
    {
        /// <summary>Adds a box plot series from the given array of datasets.</summary>
        /// <param name="datasets">An array of datasets, each containing the values for one box.</param>
        /// <returns>The newly created <see cref="BoxSeries"/> for further configuration.</returns>
        public BoxSeries BoxPlot(double[][] datasets) => axes.AddSeries(new BoxSeries(datasets));

        /// <summary>Adds a violin plot series from the given array of datasets.</summary>
        /// <param name="datasets">An array of datasets, each containing the values for one violin.</param>
        /// <returns>The newly created <see cref="ViolinSeries"/> for further configuration.</returns>
        public ViolinSeries Violin(double[][] datasets) => axes.AddSeries(new ViolinSeries(datasets));

        /// <summary>Adds a kernel density estimation (KDE) series for the given data sample.</summary>
        /// <param name="data">The data values used to estimate the density.</param>
        /// <returns>The newly created <see cref="KdeSeries"/> for further configuration.</returns>
        public KdeSeries Kde(double[] data) => axes.AddSeries(new KdeSeries(data));

        /// <summary>Adds a rug plot series showing individual data values as tick marks along the X axis.</summary>
        /// <param name="data">The data values to display as rug ticks.</param>
        /// <returns>The newly created <see cref="RugplotSeries"/> for further configuration.</returns>
        public RugplotSeries Rugplot(Numerics.Vec data) => axes.AddSeries(new RugplotSeries(data));

        /// <summary>Adds a strip plot series showing individual data points per category with random jitter.</summary>
        /// <param name="datasets">An array of datasets, each containing values for one category.</param>
        /// <returns>The newly created <see cref="StripplotSeries"/> for further configuration.</returns>
        public StripplotSeries Stripplot(double[][] datasets) => axes.AddSeries(new StripplotSeries(datasets));

        /// <summary>Adds a swarm plot series showing non-overlapping dots per category.</summary>
        /// <param name="datasets">An array of datasets, one per category.</param>
        /// <returns>The newly created <see cref="SwarmplotSeries"/> for further configuration.</returns>
        public SwarmplotSeries Swarmplot(double[][] datasets) => axes.AddSeries(new SwarmplotSeries(datasets));
    }
}
