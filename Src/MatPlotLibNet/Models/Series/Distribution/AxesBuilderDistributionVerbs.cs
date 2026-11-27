// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The verbs of the axes builder that add a series of the distribution family (a box, a violin, a density, the points
/// of a sample): each adds the series through the axes verb of the same name (<see cref="AxesDistributionVerbs"/>) and hands it
/// to the caller's callback.</summary>
public static class AxesBuilderDistributionVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Adds a box plot series to the axes.</summary>
        public AxesBuilder BoxPlot(double[][] datasets, Action<BoxSeries>? configure = null)
            => axes.Configured(axes.Axes.BoxPlot(datasets), configure);

        /// <summary>Adds a violin series to the axes.</summary>
        public AxesBuilder Violin(double[][] datasets, Action<ViolinSeries>? configure = null)
            => axes.Configured(axes.Axes.Violin(datasets), configure);

        /// <summary>Adds a kernel density estimation (KDE) series to the axes.</summary>
        public AxesBuilder Kde(double[] data, Action<KdeSeries>? configure = null)
            => axes.Configured(axes.Axes.Kde(data), configure);

        /// <summary>Adds a rug plot series to the axes.</summary>
        public AxesBuilder Rugplot(double[] data, Action<RugplotSeries>? configure = null)
            => axes.Configured(axes.Axes.Rugplot(data), configure);

        /// <summary>Adds a strip plot series to the axes.</summary>
        public AxesBuilder Stripplot(double[][] datasets, Action<StripplotSeries>? configure = null)
            => axes.Configured(axes.Axes.Stripplot(datasets), configure);

        /// <summary>Adds a swarm plot series to the axes.</summary>
        public AxesBuilder Swarmplot(double[][] datasets, Action<SwarmplotSeries>? configure = null)
            => axes.Configured(axes.Axes.Swarmplot(datasets), configure);
    }
}
