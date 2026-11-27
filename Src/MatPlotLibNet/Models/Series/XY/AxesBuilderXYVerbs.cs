// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The verbs of the axes builder that add a series of the XY family (a line, a scatter, an area, a step, a signal):
/// each adds the series through the axes verb of the same name (<see cref="AxesXYVerbs"/>) and hands it to the caller's
/// callback.</summary>
public static class AxesBuilderXYVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Adds a line series with DateTime X values; automatically activates the date X-axis.</summary>
        /// <param name="dates">X values as <see cref="DateTime"/> instances (converted to OLE Automation dates).</param>
        /// <param name="y">Y values corresponding to each date.</param>
        /// <param name="configure">Optional delegate to further configure the series.</param>
        public AxesBuilder Plot(DateTime[] dates, double[] y, Action<Models.Series.LineSeries>? configure = null)
            => axes.SetXDateAxis().Plot(dates.Select(d => d.ToOADate()).ToArray(), y, configure);

        /// <summary>Adds a scatter series with DateTime X values; automatically activates the date X-axis.</summary>
        /// <param name="dates">X values as <see cref="DateTime"/> instances (converted to OLE Automation dates).</param>
        /// <param name="y">Y values corresponding to each date.</param>
        /// <param name="configure">Optional delegate to further configure the series.</param>
        public AxesBuilder Scatter(DateTime[] dates, double[] y, Action<Models.Series.ScatterSeries>? configure = null)
            => axes.SetXDateAxis().Scatter(dates.Select(d => d.ToOADate()).ToArray(), y, configure);

        /// <summary>Adds a line series to the axes.</summary>
        public AxesBuilder Plot(double[] x, double[] y, Action<LineSeries>? configure = null)
            => axes.Configured(axes.Axes.Plot(x, y), configure);

        /// <summary>Adds a scatter series to the axes.</summary>
        public AxesBuilder Scatter(double[] x, double[] y, Action<ScatterSeries>? configure = null)
            => axes.Configured(axes.Axes.Scatter(x, y), configure);

        /// <summary>Adds a scatter whose markers are coloured by how crowded their neighbourhood is.
        /// <para>A scatter of ten thousand points is a black blob: the markers overlap, and where they overlap most
        /// is exactly what the reader wants to know and cannot see. This counts how many points share each point's
        /// cell of a grid laid over the cloud, puts that count on the series as its colour value, and gives it a
        /// perceptually uniform colour map to read it with.</para>
        /// <para>Counted, not estimated: a two-dimensional kernel estimate walks every other point for every point,
        /// which is unaffordable at exactly the sample sizes that make this chart worth drawing.
        /// <paramref name="configure"/> runs afterwards, so a caller with a better number than crowding - an age, a
        /// score, an error - can assign their own and keep everything else.</para></summary>
        /// <param name="x">The X coordinates.</param>
        /// <param name="y">The Y coordinates, one per X.</param>
        /// <param name="bins">Cells to a side of the counting grid; 0 chooses one from the point count, aiming at
        /// roughly ten points per cell.</param>
        /// <param name="configure">Optional configuration, applied after the density is assigned.</param>
        /// <returns>This builder for chaining.</returns>
        /// <exception cref="ArgumentException">The two axes carry different numbers of points.</exception>
        public AxesBuilder DensityScatter(double[] x, double[] y, Action<ScatterSeries>? configure = null, int bins = 0)
        {
            ArgumentNullException.ThrowIfNull(x);
            ArgumentNullException.ThrowIfNull(y);

            double[] density = Numerics.PointDensity.PerPoint(
                x, y, bins > 0 ? bins : Numerics.PointDensity.BinsFor(x.Length));

            return axes.Configured(axes.Axes.Scatter(x, y), series =>
            {
                series.C = density;
                // Named, not left to the theme: the verb promises a perceptually uniform map, which jet (the classic theme's) is not.
                series.ColorMap = Styling.ColorMaps.ColorMaps.Viridis;
                configure?.Invoke(series);
            });
        }

        /// <summary>Adds an ECDF series to the axes.</summary>
        public AxesBuilder Ecdf(double[] data, Action<EcdfSeries>? configure = null)
            => axes.Configured(axes.Axes.Ecdf(data), configure);

        /// <summary>Adds a stacked area (stackplot) series to the axes.</summary>
        public AxesBuilder StackPlot(double[] x, double[][] ySets, Action<StackedAreaSeries>? configure = null)
            => axes.Configured(axes.Axes.StackPlot(x, ySets), configure);

        /// <summary>Adds a polynomial regression series to the axes.</summary>
        public AxesBuilder Regression(double[] x, double[] y, Action<RegressionSeries>? configure = null)
            => axes.Configured(axes.Axes.Regression(x, y), configure);

        /// <summary>Adds a sparkline to the axes.</summary>
        public AxesBuilder Sparkline(double[] values, Action<SparklineSeries>? configure = null)
            => axes.Configured(axes.Axes.Sparkline(values), configure);

        /// <summary>Adds a bubble chart series to the axes: a scatter whose marker area is a third variable.</summary>
        /// <param name="x">X coordinates.</param>
        /// <param name="y">Y coordinates.</param>
        /// <param name="sizes">The area of each bubble in points squared, as a scatter's size is: a size of 100 is a bubble 10 points across.</param>
        /// <param name="configure">Sets the <see cref="BubbleSeries"/> (colour, opacity, label).</param>
        /// <returns>This builder, for chaining.</returns>
        public AxesBuilder Bubble(double[] x, double[] y, double[] sizes, Action<BubbleSeries>? configure = null)
            => axes.Configured(axes.Axes.Bubble(x, y, sizes), configure);

        /// <summary>Adds a Relative Rotation Graph series to the axes.</summary>
        /// <param name="assetCloses">Per-asset close price series.</param>
        /// <param name="benchmarkCloses">Benchmark close price series (e.g. BTC).</param>
        /// <param name="assetLabels">Display labels — one per asset.</param>
        /// <param name="configure">Optional configuration callback.</param>
        /// <returns>This <see cref="AxesBuilder"/> for chaining.</returns>
        public AxesBuilder RelativeRotation(
            double[][] assetCloses,
            double[] benchmarkCloses,
            string[] assetLabels,
            Action<RelativeRotationSeries>? configure = null)
            => axes.Configured(axes.Axes.RelativeRotation(assetCloses, benchmarkCloses, assetLabels), configure);

        /// <summary>Adds an error bar series to the axes.</summary>
        public AxesBuilder ErrorBar(double[] x, double[] y, double[] yErrorLow, double[] yErrorHigh, Action<ErrorBarSeries>? configure = null)
            => axes.Configured(axes.Axes.ErrorBar(x, y, yErrorLow, yErrorHigh), configure);

        /// <summary>Adds a step-function series to the axes.</summary>
        public AxesBuilder Step(double[] x, double[] y, Action<StepSeries>? configure = null)
            => axes.Configured(axes.Axes.Step(x, y), configure);

        /// <summary>Adds a filled area (fill-between) series to the axes.</summary>
        public AxesBuilder FillBetween(double[] x, double[] y, double[]? y2 = null, Action<AreaSeries>? configure = null)
            => axes.Configured(axes.Axes.FillBetween(x, y, y2), configure);

        /// <summary>Adds an area that lies down (matplotlib's <c>fill_betweenx</c>): at each position along Y, the fill runs between
        /// two curves on X, or from X = 0 to <paramref name="x1"/> when there is no second curve.</summary>
        /// <param name="y">The positions along the Y axis.</param>
        /// <param name="x1">The first curve on the X axis, one value per position.</param>
        /// <param name="x2">The second curve on the X axis, or <see langword="null"/> to fill from X = 0.</param>
        /// <param name="configure">Optional configuration callback.</param>
        /// <returns>This builder.</returns>
        public AxesBuilder FillBetweenX(double[] y, double[] x1, double[]? x2 = null, Action<AreaSeries>? configure = null)
            => axes.Configured(axes.Axes.FillBetweenX(y, x1, x2), configure);

        /// <summary>Adds a residual plot series to the axes.</summary>
        public AxesBuilder Residplot(double[] x, double[] y, Action<ResidualSeries>? configure = null)
            => axes.Configured(axes.Axes.Residplot(x, y), configure);

        /// <summary>Adds a <see cref="SignalSeries"/> with uniform sample rate to the axes.</summary>
        public AxesBuilder Signal(double[] y, double sampleRate = SignalSeries.RestingSampleRate, double xStart = SignalSeries.RestingXStart, Action<SignalSeries>? configure = null)
            => axes.Configured(axes.Axes.Signal(y, sampleRate, xStart), configure);

        /// <summary>Adds a <see cref="SignalXYSeries"/> with monotonically ascending X values to the axes.</summary>
        public AxesBuilder SignalXY(double[] x, double[] y, Action<SignalXYSeries>? configure = null)
            => axes.Configured(axes.Axes.SignalXY(x, y), configure);
    }
}
