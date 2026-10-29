// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The XY-family verbs of <see cref="Axes"/> (<c>Plot</c>, <c>Scatter</c>, …), each constructing one series
/// and handing it to <see cref="Axes.AddSeries{T}"/>. Extension members so <c>Axes</c> itself holds only state and
/// its series collections, never an open-ended verb surface — a new XY series type is a new member here, not a
/// change to <c>Axes</c>.</summary>
public static class AxesXYVerbs
{
    extension(Axes axes)
    {
        /// <summary>Adds a line series from the given X and Y data arrays.</summary>
        /// <param name="x">The X-axis data values.</param>
        /// <param name="y">The Y-axis data values.</param>
        /// <returns>The newly created <see cref="LineSeries"/> for further configuration.</returns>
        public LineSeries Plot(double[] x, double[] y)
        {
            x.Length.ThrowIfMisaligned(y.Length);
            return axes.AddSeries(new LineSeries(x, y));
        }

        /// <summary>Adds a scatter series from the given X and Y data arrays.</summary>
        /// <param name="x">The X-axis data values.</param>
        /// <param name="y">The Y-axis data values.</param>
        /// <returns>The newly created <see cref="ScatterSeries"/> for further configuration.</returns>
        public ScatterSeries Scatter(double[] x, double[] y)
        {
            x.Length.ThrowIfMisaligned(y.Length);
            return axes.AddSeries(new ScatterSeries(x, y));
        }

        /// <summary>Adds a stacked area (stackplot) series from the given shared X values and multiple Y datasets.</summary>
        /// <param name="x">The shared X-axis data values.</param>
        /// <param name="ySets">An array of Y arrays, one per stacked layer.</param>
        /// <returns>The newly created <see cref="StackedAreaSeries"/> for further configuration.</returns>
        public StackedAreaSeries StackPlot(double[] x, double[][] ySets)
        {
            foreach (var layer in ySets) x.Length.ThrowIfMisaligned(layer.Length);
            return axes.AddSeries(new StackedAreaSeries(x, ySets));
        }

        /// <summary>Adds a step-function series from the given X and Y data arrays.</summary>
        /// <param name="x">The X-axis data values.</param>
        /// <param name="y">The Y-axis data values.</param>
        /// <returns>The newly created <see cref="StepSeries"/> for further configuration.</returns>
        public StepSeries Step(double[] x, double[] y)
        {
            x.Length.ThrowIfMisaligned(y.Length);
            return axes.AddSeries(new StepSeries(x, y));
        }

        /// <summary>Adds a filled area series from the given X and Y data arrays.</summary>
        /// <param name="x">The X-axis data values.</param>
        /// <param name="y">The Y-axis data values (top line of the fill region).</param>
        /// <param name="y2">Optional secondary Y data. When provided, fills between the two curves; when null, fills down to y=0.</param>
        /// <returns>The newly created <see cref="AreaSeries"/> for further configuration.</returns>
        public AreaSeries FillBetween(double[] x, double[] y, double[]? y2 = null)
        {
            x.Length.ThrowIfMisaligned(y.Length);
            if (y2 is not null) x.Length.ThrowIfMisaligned(y2.Length);
            return axes.AddSeries(new AreaSeries(x, y) { YData2 = y2 });
        }

        /// <summary>Adds a bubble chart series from X, Y, and size arrays.</summary>
        public BubbleSeries Bubble(double[] x, double[] y, double[] sizes)
        {
            x.Length.ThrowIfMisaligned(y.Length);
            x.Length.ThrowIfMisaligned(sizes.Length);
            return axes.AddSeries(new BubbleSeries(x, y, sizes));
        }

        /// <summary>Adds an error bar series from the given X, Y data and error magnitudes.</summary>
        public ErrorBarSeries ErrorBar(double[] x, double[] y, double[] yErrorLow, double[] yErrorHigh)
        {
            x.Length.ThrowIfMisaligned(y.Length);
            x.Length.ThrowIfMisaligned(yErrorLow.Length);
            x.Length.ThrowIfMisaligned(yErrorHigh.Length);
            return axes.AddSeries(new ErrorBarSeries(x, y, yErrorLow, yErrorHigh));
        }

        /// <summary>Adds an ECDF (empirical cumulative distribution function) series from the given data.</summary>
        public EcdfSeries Ecdf(double[] data) => axes.AddSeries(new EcdfSeries(data));

        /// <summary>Adds a polynomial regression series fitted to the given X and Y data.</summary>
        /// <param name="x">The X data values.</param>
        /// <param name="y">The Y data values.</param>
        /// <returns>The newly created <see cref="RegressionSeries"/> for further configuration.</returns>
        public RegressionSeries Regression(double[] x, double[] y) => axes.AddSeries(new RegressionSeries(x, y));

        /// <summary>Adds a residual plot series showing regression residuals as a scatter plot.</summary>
        /// <param name="x">The X data values.</param>
        /// <param name="y">The Y data values.</param>
        /// <returns>The newly created <see cref="ResidualSeries"/> for further configuration.</returns>
        public ResidualSeries Residplot(Numerics.Vec x, Numerics.Vec y) => axes.AddSeries(new ResidualSeries(x, y));

        /// <summary>Adds a Relative Rotation Graph series to the axes.</summary>
        /// <param name="assetCloses">Per-asset close price series (jagged; each element must equal <paramref name="benchmarkCloses"/> length).</param>
        /// <param name="benchmarkCloses">Benchmark close price series (e.g. BTC).</param>
        /// <param name="assetLabels">Display labels — one per asset (e.g. "ETH", "SOL").</param>
        /// <param name="configure">Optional configuration callback.</param>
        /// <returns>The newly added <see cref="RelativeRotationSeries"/> for further property configuration.</returns>
        public RelativeRotationSeries RelativeRotation(
            double[][] assetCloses,
            double[] benchmarkCloses,
            string[] assetLabels,
            Action<RelativeRotationSeries>? configure = null)
        {
            var series = new RelativeRotationSeries(assetCloses, benchmarkCloses, assetLabels);
            configure?.Invoke(series);
            return axes.AddSeries(series);
        }

        /// <summary>Adds a <see cref="SignalXYSeries"/> with monotonically ascending X values.</summary>
        /// <param name="x">Monotonically ascending X values. Must match the length of <paramref name="y"/>.</param>
        /// <param name="y">Y values parallel to <paramref name="x"/>.</param>
        public SignalXYSeries SignalXY(double[] x, double[] y)
        {
            x.Length.ThrowIfMisaligned(y.Length);
            return axes.AddSeries(new SignalXYSeries(x, y));
        }

        /// <summary>Adds a <see cref="SignalSeries"/> with uniform sample rate.</summary>
        /// <param name="y">Y values sampled at <paramref name="sampleRate"/> Hz starting at <paramref name="xStart"/>.</param>
        /// <param name="sampleRate">Samples per X-unit (e.g., Hz for time-domain data). Defaults to 1.</param>
        /// <param name="xStart">X value of the first sample. Defaults to 0.</param>
        public SignalSeries Signal(double[] y, double sampleRate = 1.0, double xStart = 0.0) => axes.AddSeries(new SignalSeries(y, sampleRate, xStart));

        /// <summary>Adds a sparkline — a tiny inline line chart with no axes or labels.</summary>
        public SparklineSeries Sparkline(double[] values) => axes.AddSeries(new SparklineSeries(values));

        /// <summary>Adds a line series plotted against the first right-hand Y axis.</summary>
        /// <remarks>Obsolete: use <c>WithAddRightYAxis(right => right.Plot(x, y))</c> on the builder, or
        /// <c>Axes.YAxes.Right.FirstOrAdd().AddSeries(new LineSeries(x, y))</c> when you need the series back. Creates that
        /// axis when there is none, as <c>YAxes.Right.FirstOrAdd()</c> does; it never stacks another.</remarks>
        [Obsolete("Use WithAddRightYAxis(right => right.Plot(x, y)), or Axes.YAxes.Right.FirstOrAdd().AddSeries(new LineSeries(x, y)) when you need the series back.")]
        public LineSeries PlotSecondary(double[] x, double[] y)
        {
            x.Length.ThrowIfMisaligned(y.Length);
            return axes.YAxes.Right.FirstOrAdd().AddSeries(new LineSeries(x, y));
        }

        /// <summary>Adds a scatter series plotted against the first right-hand Y axis.</summary>
        /// <remarks>Obsolete: use <c>WithAddRightYAxis(right => right.Scatter(x, y))</c> on the builder, or
        /// <c>Axes.YAxes.Right.FirstOrAdd().AddSeries(new ScatterSeries(x, y))</c> when you need the series back. Creates
        /// that axis when there is none, as <c>YAxes.Right.FirstOrAdd()</c> does; it never stacks another.</remarks>
        [Obsolete("Use WithAddRightYAxis(right => right.Scatter(x, y)), or Axes.YAxes.Right.FirstOrAdd().AddSeries(new ScatterSeries(x, y)) when you need the series back.")]
        public ScatterSeries ScatterSecondary(double[] x, double[] y)
        {
            x.Length.ThrowIfMisaligned(y.Length);
            return axes.YAxes.Right.FirstOrAdd().AddSeries(new ScatterSeries(x, y));
        }

        /// <summary>Adds a line series plotted against the secondary X-axis.</summary>
        /// <remarks>Implicitly calls <see cref="Axes.TwinY"/> to ensure the secondary axis exists.</remarks>
        public LineSeries PlotXSecondary(double[] x, double[] y)
        {
            x.Length.ThrowIfMisaligned(y.Length);
            axes.TwinY();
            return axes.AddXSecondarySeries(new LineSeries(x, y));
        }

        /// <summary>Adds a scatter series plotted against the secondary X-axis.</summary>
        /// <remarks>Implicitly calls <see cref="Axes.TwinY"/> to ensure the secondary axis exists.</remarks>
        public ScatterSeries ScatterXSecondary(double[] x, double[] y)
        {
            x.Length.ThrowIfMisaligned(y.Length);
            axes.TwinY();
            return axes.AddXSecondarySeries(new ScatterSeries(x, y));
        }
    }
}
