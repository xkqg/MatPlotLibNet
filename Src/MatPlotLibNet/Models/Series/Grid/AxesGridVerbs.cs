// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The grid-family verbs of <see cref="Axes"/> (<c>Heatmap</c>, <c>Contour</c>, <c>PairGrid</c>, …), each
/// constructing one series and handing it to <see cref="Axes.AddSeries{T}"/>.</summary>
public static class AxesGridVerbs
{
    extension(Axes axes)
    {
        /// <summary>Adds a heatmap series from the given two-dimensional data array.</summary>
        /// <param name="data">The 2D data matrix to render as a heatmap.</param>
        /// <returns>The newly created <see cref="HeatmapSeries"/> for further configuration.</returns>
        public HeatmapSeries Heatmap(double[,] data) => axes.AddSeries(new HeatmapSeries(data));

        /// <summary>Adds an image series from the given two-dimensional data array (imshow).</summary>
        /// <param name="data">The 2D data matrix to render as a colored image.</param>
        /// <returns>The newly created <see cref="ImageSeries"/> for further configuration.</returns>
        public ImageSeries Image(double[,] data) => axes.AddSeries(new ImageSeries(data));

        /// <summary>Adds a 2D histogram (density) series from X,Y scatter data.</summary>
        /// <param name="x">The X-axis data values.</param>
        /// <param name="y">The Y-axis data values.</param>
        /// <param name="bins">The number of bins along both axes.</param>
        /// <returns>The newly created <see cref="Histogram2DSeries"/> for further configuration.</returns>
        public Histogram2DSeries Histogram2D(double[] x, double[] y, int bins = 20)
        {
            x.Length.ThrowIfMisaligned(y.Length);
            return axes.AddSeries(new Histogram2DSeries(x, y, bins, bins));
        }

        /// <summary>Adds a hexagonal binning series from the given X and Y scatter data.</summary>
        /// <param name="x">The X data values.</param>
        /// <param name="y">The Y data values.</param>
        /// <returns>The newly created <see cref="HexbinSeries"/> for further configuration.</returns>
        public HexbinSeries Hexbin(double[] x, double[] y) => axes.AddSeries(new HexbinSeries(x, y));

        /// <summary>Adds a contour series from the given X, Y, and Z data.</summary>
        /// <param name="x">The X-axis grid coordinates.</param>
        /// <param name="y">The Y-axis grid coordinates.</param>
        /// <param name="z">The 2D matrix of Z values at each grid point.</param>
        /// <returns>The newly created <see cref="ContourSeries"/> for further configuration.</returns>
        public ContourSeries Contour(double[] x, double[] y, double[,] z) => axes.AddSeries(new ContourSeries(x, y, z));

        /// <summary>Adds a filled contour series from the given X, Y, and Z data.</summary>
        /// <param name="x">The X-axis grid coordinates.</param>
        /// <param name="y">The Y-axis grid coordinates.</param>
        /// <param name="z">The 2D matrix of Z values at each grid point.</param>
        /// <returns>The newly created <see cref="ContourfSeries"/> for further configuration.</returns>
        public ContourfSeries Contourf(double[] x, double[] y, double[,] z) => axes.AddSeries(new ContourfSeries(x, y, z));

        /// <summary>Adds a clustermap series to the axes (heatmap with optional row/column dendrograms).</summary>
        /// <param name="data">The 2D data matrix to render as the heatmap.</param>
        /// <param name="configure">Optional configuration callback for the series.</param>
        /// <returns>The newly added <see cref="ClustermapSeries"/> for further property configuration.</returns>
        public ClustermapSeries Clustermap(double[,] data, Action<ClustermapSeries>? configure = null)
        {
            var series = new ClustermapSeries(data);
            configure?.Invoke(series);
            return axes.AddSeries(series);
        }

        /// <summary>Adds a pair-grid series to the axes (N×N matrix of histograms + scatters).</summary>
        /// <param name="variables">The N input variables (one <c>double[]</c> per variable, all equal length).</param>
        /// <param name="configure">Optional configuration callback for the series.</param>
        /// <returns>The newly added <see cref="PairGridSeries"/> for further property configuration.</returns>
        public PairGridSeries PairGrid(double[][] variables, Action<PairGridSeries>? configure = null)
        {
            var series = new PairGridSeries(variables);
            configure?.Invoke(series);
            return axes.AddSeries(series);
        }

        /// <summary>Adds a pseudocolor mesh series on a non-uniform rectangular grid.</summary>
        /// <param name="x">The M+1 X-axis edge coordinates.</param>
        /// <param name="y">The N+1 Y-axis edge coordinates.</param>
        /// <param name="c">The N×M data matrix.</param>
        /// <returns>The newly created <see cref="PcolormeshSeries"/> for further configuration.</returns>
        public PcolormeshSeries Pcolormesh(Numerics.Vec x, Numerics.Vec y, double[,] c) => axes.AddSeries(new PcolormeshSeries(x, y, c));

        /// <summary>Adds a spectrogram series displaying the time-frequency content of a signal.</summary>
        /// <param name="signal">The input signal values.</param>
        /// <param name="sampleRate">The sample rate in Hz.</param>
        /// <returns>The newly created <see cref="SpectrogramSeries"/> for further configuration.</returns>
        public SpectrogramSeries Spectrogram(Numerics.Vec signal, int sampleRate = 1) => axes.AddSeries(new SpectrogramSeries(signal) { SampleRate = sampleRate });

        /// <summary>Adds a contour series on an unstructured triangular mesh.</summary>
        public TricontourSeries Tricontour(Numerics.Vec x, Numerics.Vec y, Numerics.Vec z) => axes.AddSeries(new TricontourSeries(x, y, z));

        /// <summary>Adds a pseudocolor series on a triangular mesh.</summary>
        public TripcolorSeries Tripcolor(Numerics.Vec x, Numerics.Vec y, Numerics.Vec z) => axes.AddSeries(new TripcolorSeries(x, y, z));
    }
}
