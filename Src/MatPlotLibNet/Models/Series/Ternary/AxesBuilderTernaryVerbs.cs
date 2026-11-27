// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The verbs of the axes builder that add a series of the ternary family: each adds the series through the axes verb
/// of the same name (<see cref="AxesTernaryVerbs"/>) and hands it to the caller's callback.</summary>
public static class AxesBuilderTernaryVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Adds a line through compositions of three parts to the axes, which becomes a ternary diagram.</summary>
        public AxesBuilder TernaryPlot(double[] top, double[] left, double[] right, Action<TernaryLineSeries>? configure = null)
            => axes.Configured(axes.Axes.TernaryPlot(top, left, right), configure);

        /// <summary>Adds a marker on each composition of three parts to the axes, which becomes a ternary diagram.</summary>
        public AxesBuilder TernaryScatter(double[] top, double[] left, double[] right, Action<TernaryScatterSeries>? configure = null)
            => axes.Configured(axes.Axes.TernaryScatter(top, left, right), configure);

        /// <summary>Adds a Piper diagram of the given waters (in mg/L) to the axes: the cations and the anions each on a triangle, both on a diamond.</summary>
        public AxesBuilder Piper(IReadOnlyList<WaterSample> samples, Action<PiperSeries>? configure = null)
            => axes.Configured(axes.Axes.Piper(samples), configure);
    }
}
