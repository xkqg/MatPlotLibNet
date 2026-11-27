// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The verbs of the axes builder that add a series of the grid family (a heatmap, an image, contours, a mesh, a
/// spectrogram): each adds the series through the axes verb of the same name (<see cref="AxesGridVerbs"/>) and hands it to the
/// caller's callback.</summary>
public static class AxesBuilderGridVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Adds a heatmap series to the axes.</summary>
        public AxesBuilder Heatmap(double[,] data, Action<HeatmapSeries>? configure = null)
            => axes.Configured(axes.Axes.Heatmap(data), configure);

        /// <summary>Adds an image series to the axes.</summary>
        public AxesBuilder Image(double[,] data, Action<ImageSeries>? configure = null)
            => axes.Configured(axes.Axes.Image(data), configure);

        /// <summary>Adds a 2D histogram (density) series to the axes.</summary>
        public AxesBuilder Histogram2D(double[] x, double[] y, int bins = Histogram2DSeries.RestingBins, Action<Histogram2DSeries>? configure = null)
            => axes.Configured(axes.Axes.Histogram2D(x, y, bins), configure);

        /// <summary>Adds a hexagonal binning series to the axes.</summary>
        public AxesBuilder Hexbin(double[] x, double[] y, Action<HexbinSeries>? configure = null)
            => axes.Configured(axes.Axes.Hexbin(x, y), configure);

        /// <summary>Adds a contour series to the axes.</summary>
        public AxesBuilder Contour(double[] x, double[] y, double[,] z, Action<ContourSeries>? configure = null)
            => axes.Configured(axes.Axes.Contour(x, y, z), configure);

        /// <summary>Adds a filled contour series to the axes.</summary>
        public AxesBuilder Contourf(double[] x, double[] y, double[,] z, Action<ContourfSeries>? configure = null)
            => axes.Configured(axes.Axes.Contourf(x, y, z), configure);

        /// <summary>Adds a clustermap series to the axes (heatmap with optional row/column dendrograms).</summary>
        /// <param name="data">The 2D data matrix to render as the heatmap.</param>
        /// <param name="configure">Optional configuration callback for the series.</param>
        public AxesBuilder Clustermap(double[,] data, Action<ClustermapSeries>? configure = null)
            => axes.Configured(axes.Axes.Clustermap(data), configure);

        /// <summary>Adds a pair-grid series to the axes (N×N matrix of histograms + scatters).</summary>
        /// <param name="variables">The N input variables (one <c>double[]</c> per variable, all equal length).</param>
        /// <param name="configure">Optional configuration callback for the series.</param>
        /// <returns>This <see cref="AxesBuilder"/> for chaining.</returns>
        public AxesBuilder PairGrid(double[][] variables, Action<PairGridSeries>? configure = null)
            => axes.Configured(axes.Axes.PairGrid(variables), configure);

        /// <summary>Adds a pseudocolor mesh series to the axes.</summary>
        public AxesBuilder Pcolormesh(double[] x, double[] y, double[,] c, Action<PcolormeshSeries>? configure = null)
            => axes.Configured(axes.Axes.Pcolormesh(x, y, c), configure);

        /// <summary>Adds a spectrogram series to the axes.</summary>
        public AxesBuilder Spectrogram(double[] signal, int sampleRate = SpectrogramSeries.RestingSampleRate, Action<SpectrogramSeries>? configure = null)
            => axes.Configured(axes.Axes.Spectrogram(signal, sampleRate), configure);

        /// <summary>Adds a contour series on an unstructured triangular mesh to the axes.</summary>
        public AxesBuilder Tricontour(double[] x, double[] y, double[] z, Action<TricontourSeries>? configure = null)
            => axes.Configured(axes.Axes.Tricontour(x, y, z), configure);

        /// <summary>Adds a pseudocolor series on a triangular mesh to the axes.</summary>
        public AxesBuilder Tripcolor(double[] x, double[] y, double[] z, Action<TripcolorSeries>? configure = null)
            => axes.Configured(axes.Axes.Tripcolor(x, y, z), configure);
    }
}
