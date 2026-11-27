// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The verbs of the axes builder that add a series of the polar family: each adds the series through the axes verb of
/// the same name (<see cref="AxesPolarVerbs"/>) and hands it to the caller's callback; and the radius a polar axes draws at its
/// centre.</summary>
public static class AxesBuilderPolarVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Sets the radius a polar axes draws at its centre, as matplotlib's <c>set_rorigin</c>: an origin below the low end
        /// of the radial axis leaves a hole at the centre (<see cref="Axes.ROrigin"/>).</summary>
        /// <param name="rOrigin">The radius at the centre.</param>
        /// <returns>This builder, for chaining.</returns>
        public AxesBuilder SetROrigin(double rOrigin) { axes.Axes.ROrigin = rOrigin; return axes; }

        /// <summary>Adds a polar line series to the axes.</summary>
        public AxesBuilder PolarPlot(double[] r, double[] theta, Action<PolarLineSeries>? configure = null)
            => axes.Configured(axes.Axes.PolarPlot(r, theta), configure);

        /// <summary>Adds a polar scatter series to the axes.</summary>
        public AxesBuilder PolarScatter(double[] r, double[] theta, Action<PolarScatterSeries>? configure = null)
            => axes.Configured(axes.Axes.PolarScatter(r, theta), configure);

        /// <summary>Adds a polar bar series to the axes.</summary>
        public AxesBuilder PolarBar(double[] r, double[] theta, Action<PolarBarSeries>? configure = null)
            => axes.Configured(axes.Axes.PolarBar(r, theta), configure);

        /// <summary>Adds a polar heatmap series to the axes.</summary>
        public AxesBuilder PolarHeatmap(double[,] data, int thetaBins, int rBins, Action<PolarHeatmapSeries>? configure = null)
            => axes.Configured(axes.Axes.PolarHeatmap(data, thetaBins, rBins), configure);
    }
}
