// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Numerics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests;

/// <summary>Axes and series the tests draw, each written once: a 3-D axes with a three-by-three surface or a four-point triangulated
/// surface, the rotational flow field of the streamplot tests, the transform an FFT is checked against, and the sample series of every
/// registered kind the golden corpus pins (<c>AddSampleSeries</c>).</summary>
public static class SampleSeries
{
    /// <summary>A 3-D axes with a surface of three by three cells, which <paramref name="configure"/> may change.</summary>
    /// <param name="configure">Changes the surface, or <see langword="null"/>.</param>
    public static Axes SurfaceAxes(Action<SurfaceSeries>? configure = null)
    {
        var axes = new Axes { CoordinateSystem = CoordinateSystem.ThreeD };
        var surface = axes.Surface([0.0, 1, 2], [0.0, 1, 2], new double[,] { { 0, 1, 0 }, { 1, 2, 1 }, { 0, 1, 0 } });
        configure?.Invoke(surface);
        return axes;
    }

    /// <summary>A 3-D axes with a triangulated surface of four points, which <paramref name="configure"/> may change.</summary>
    /// <param name="configure">Changes the surface, or <see langword="null"/>.</param>
    public static Axes TrisurfAxes(Action<Trisurf3DSeries>? configure = null)
    {
        var axes = new Axes { CoordinateSystem = CoordinateSystem.ThreeD };
        var trisurf = axes.Trisurf([0.0, 1, 0, 1], [0.0, 0, 1, 1], [0.0, 1, 1, 2]);
        configure?.Invoke(trisurf);
        return axes;
    }

    /// <summary>A price series of <paramref name="count"/> values that starts at 100 and moves by up to half of <paramref name="step"/> of
    /// itself either way each bar, drawn from the seeded generator: the random walk the indicator tests run over.</summary>
    /// <param name="count">How many prices.</param>
    /// <param name="step">The width of the move of one bar, as a fraction of the price.</param>
    /// <param name="seed">The seed of the generator.</param>
    public static double[] RandomWalk(int count, double step, int seed = 42)
    {
        var rng = new Random(seed);
        var prices = new double[count];
        prices[0] = 100;
        for (int i = 1; i < count; i++)
        {
            prices[i] = prices[i - 1] * (1 + (rng.NextDouble() - 0.5) * step);
        }

        return prices;
    }

    /// <summary>The 40 values the streamplot field is given over, -3 to 3, the last one exactly 3.</summary>
    public static double[] StreamAxis()
    {
        var axis = new double[40];
        double step = 6.0 / 39;
        for (int i = 0; i < axis.Length; i++)
        {
            axis[i] = (i * step) + -3;
        }

        axis[^1] = 3;
        return axis;
    }

    /// <summary>The flow field the streamplot tests draw: <c>u = -1 - x^2 + y</c>, <c>v = 1 + x - y^2</c> over <see cref="StreamAxis"/>.</summary>
    /// <param name="density">How closely the streamlines are laid.</param>
    public static StreamplotSeries RotationalField(double density = 1)
    {
        var axis = StreamAxis();
        var u = new double[40, 40];
        var v = new double[40, 40];
        for (int row = 0; row < 40; row++)
        {
            for (int column = 0; column < 40; column++)
            {
                double x = axis[column], y = axis[row];
                u[row, column] = -1 - (x * x) + y;
                v[row, column] = 1 + x - (y * y);
            }
        }

        return new StreamplotSeries(axis, axis, u, v) { Density = density };
    }

    extension(IReadOnlyList<double> signal)
    {
        /// <summary>The discrete Fourier transform of the signal, summed straight from its definition; the phase index is reduced
        /// modulo the length so the angle stays small.</summary>
        public Complex[] DirectDft() => signal.Select(v => new Complex(v, 0)).ToArray().DirectDft();
    }

    extension(IReadOnlyList<Complex> signal)
    {
        /// <summary>The discrete Fourier transform of the signal, summed straight from its definition; the phase index is reduced
        /// modulo the length so the angle stays small.</summary>
        public Complex[] DirectDft()
        {
            int n = signal.Count;
            var result = new Complex[n];
            for (int k = 0; k < n; k++)
            {
                Complex sum = Complex.Zero;
                for (int j = 0; j < n; j++)
                {
                    double angle = -2 * Math.PI * ((long)k * j % n) / n;
                    sum += signal[j] * new Complex(Math.Cos(angle), Math.Sin(angle));
                }

                result[k] = sum;
            }

            return result;
        }
    }

    /// <summary>A grid of two by two cells the sample series draw.</summary>
    private static readonly double[,] Grid2X2 = { { 1.0, 2.0 }, { 3.0, 4.0 } };

    /// <summary>Another grid of two by two cells, a field whose two corners point the other way.</summary>
    private static readonly double[,] Grid2X2Alt = { { 0.0, 1.0 }, { 1.0, 0.0 } };

    extension(AxesBuilder ax)
    {
        /// <summary>Adds the sample series of the kind named by its registry discriminator: the data each kind is drawn with in the
        /// golden corpus (<c>Serialization/Golden/&lt;discriminator&gt;.json</c>), which holds every row byte for byte, so a row's data
        /// is never changed here.</summary>
        /// <param name="discriminator">The registry's name of the kind, as <see cref="MatPlotLibNet.Serialization.SeriesRegistry.Discriminators"/> lists it.</param>
        /// <returns>The same builder.</returns>
        /// <exception cref="ArgumentOutOfRangeException">No row draws a kind of that name.</exception>
        public AxesBuilder AddSampleSeries(string discriminator)
        {
            switch (discriminator)
            {
                case "line": ax.Plot([1.0, 2, 3], [4.0, 5, 6]); break;
                case "scatter": ax.Scatter([1.0, 2, 3], [4.0, 5, 6]); break;
                case "bar": ax.Bar(["A", "B"], [1.0, 2.0]); break;
                case "histogram": ax.Hist([1.0, 2, 3, 4, 5]); break;
                case "pie": ax.Pie([30.0, 70.0]); break;
                case "box": ax.BoxPlot([[1.0, 2, 3]]); break;
                case "violin": ax.Violin([[1.0, 2, 3]]); break;
                case "hexbin": ax.Hexbin([1.0, 2, 3], [1.0, 2, 3]); break;
                case "regression": ax.Regression([1.0, 2, 3], [1.0, 2, 3]); break;
                case "kde": ax.Kde([1.0, 2, 3, 4, 5]); break;
                case "heatmap": ax.Heatmap(Grid2X2); break;
                case "image": ax.Image(Grid2X2); break;
                case "histogram2d": ax.Histogram2D([1.0, 2, 3], [1.0, 2, 3]); break;
                case "stem": ax.Stem([1.0, 2], [3.0, 4]); break;
                case "contour": ax.Contour([1.0, 2], [1.0, 2], Grid2X2); break;
                case "contourf": ax.Contourf([1.0, 2], [1.0, 2], Grid2X2); break;
                case "area": ax.FillBetween([1.0, 2], [3.0, 4]); break;
                case "step": ax.Step([1.0, 2], [3.0, 4]); break;
                case "ecdf": ax.Ecdf([1.0, 2, 3]); break;
                case "stackedarea": ax.StackPlot([1.0, 2], [[1.0, 2], [3.0, 4]]); break;
                case "errorbar": ax.ErrorBar([1.0, 2], [3.0, 4], [0.1, 0.2], [0.1, 0.2]); break;
                case "candlestick": ax.Candlestick([10.0, 11], [12.0, 13], [9.0, 10], [11.0, 12]); break;
                case "quiver": ax.Quiver([0.0, 1], [0.0, 1], [1.0, 1], [1.0, 1]); break;
                case "streamplot": ax.Streamplot([0.0, 1.0], [0.0, 1.0], Grid2X2Alt, Grid2X2Alt); break;
                case "radar": ax.Radar(["A", "B", "C"], [1.0, 2.0, 3.0]); break;
                case "donut": ax.Donut([30.0, 70.0]); break;
                case "bubble": ax.Bubble([1.0, 2], [3.0, 4], [10.0, 20]); break;
                case "ohlcbar": ax.OhlcBar([10.0, 12], [15.0, 17], [8.0, 10], [13.0, 15]); break;
                case "waterfall": ax.Waterfall(["A", "B"], [10.0, -5.0]); break;
                case "funnel": ax.Funnel(["A", "B"], [100.0, 50.0]); break;
                case "gantt": ax.Gantt(["Task A", "Task B"], [0.0, 2.0], [3.0, 5.0]); break;
                case "gauge": ax.Gauge(0.7); break;
                case "progressbar": ax.ProgressBar(0.5); break;
                case "stattile": ax.StatTile(42.0); break;
                case "statetimeline":
                    ax.StateTimeline([new StateSegment(0, 1, "A", Colors.Red), new StateSegment(1, 2, "B", Colors.Blue)]);
                    break;
                case "bulletgraph":
                    ax.Bullet(2412, b =>
                    {
                        b.Target = 2500;
                        b.Bands = [new(1800, Colors.Red), new(2800, Colors.Blue)];
                    });
                    break;
                case "sparkline": ax.Sparkline([1.0, 2, 3, 4, 5]); break;
                case "treemap": ax.Treemap(new TreeNode { Label = "Root", Children = [new TreeNode { Label = "A", Value = 10 }] }); break;
                case "sunburst": ax.Sunburst(new TreeNode { Label = "Root", Children = [new TreeNode { Label = "A", Value = 10 }] }); break;
                case "icicle": ax.Icicle(new TreeNode { Label = "Root", Children = [new TreeNode { Label = "A", Value = 10 }, new TreeNode { Label = "B", Value = 30 }] }); break;
                case "dendrogram":
                    ax.Dendrogram(new TreeNode
                    {
                        Label = "Root",
                        Value = 1,
                        Children = [new TreeNode { Label = "A", Value = 0 }, new TreeNode { Label = "B", Value = 0 }],
                    });
                    break;
                case "clustermap": ax.Clustermap(Grid2X2); break;
                case "pairgrid": ax.PairGrid([[1.0, 2, 3], [4.0, 5, 6]]); break;
                case "networkgraph":
                    ax.NetworkGraph(
                        [new GraphNode("a", X: 1, Y: 2, ColorScalar: 0.3, SizeScalar: 1.5, Label: "Alpha"),
                         new GraphNode("b", X: 3, Y: 4, ColorScalar: 0.7, SizeScalar: 2.0)],
                        [new GraphEdge("a", "b", Weight: 1.5, IsDirected: true)]);
                    break;
                case "relativerotation":
                    ax.RelativeRotation(
                        [[100.0, 101.0, 102.0], [100.0, 99.0, 98.0]],
                        [100.0, 100.0, 100.0],
                        ["ETH", "BNB"]);
                    break;
                case "sankey": ax.Sankey([new SankeyNode("A"), new SankeyNode("B")], [new SankeyLink(0, 1, 10)]); break;
                case "polarline": ax.PolarPlot([1.0, 2], [0.0, 1.5]); break;
                case "polarscatter": ax.PolarScatter([1.0, 2], [0.0, 1.5]); break;
                case "polarbar": ax.PolarBar([1.0, 2], [0.0, 1.5]); break;
                case "ternaryscatter": ax.TernaryScatter([0.2, 0.5], [0.5, 0.3], [0.3, 0.2]); break;
                case "wordcloud": ax.WordCloud(["data", "chart", "plot"], [10.0, 4.0, 1.0]); break;
                case "chord": ax.Chord(["A", "B", "C"], new double[,] { { 0, 5, 2 }, { 3, 0, 0 }, { 0, 4, 6 } }); break;
                case "piper": ax.Piper([new WaterSample { Ca = 60, Mg = 10, Na = 8, K = 2, HCO3 = 200, SO4 = 20, Cl = 10 }]); break;
                case "ternaryline": ax.TernaryPlot([0.2, 0.5], [0.5, 0.3], [0.3, 0.2]); break;
                case "surface": ax.Surface([0.0, 1.0], [0.0, 1.0], Grid2X2Alt); break;
                case "wireframe": ax.Wireframe([0.0, 1.0], [0.0, 1.0], Grid2X2Alt); break;
                case "scatter3d": ax.Scatter3D([1.0, 2], [3.0, 4], [5.0, 6]); break;
                case "rugplot": ax.Rugplot([1.0, 2, 3]); break;
                case "stripplot": ax.Stripplot([[1.0, 2, 3], [4.0, 5, 6]]); break;
                case "eventplot": ax.Eventplot([[1.0, 2, 3]]); break;
                case "brokenbar": ax.BrokenBarH([[new BarRange(1, 2), new BarRange(4, 1)]]); break;
                case "count": ax.Countplot(["A", "A", "B"]); break;
                case "pcolormesh": ax.Pcolormesh([0.0, 1, 2], [0.0, 1, 2], Grid2X2); break;
                case "residual": ax.Residplot([1.0, 2, 3, 4], [1.5, 2.1, 2.8, 4.2]); break;
                case "pointplot": ax.Pointplot([[1.0, 2, 3], [4.0, 5, 6]]); break;
                case "swarmplot": ax.Swarmplot([[1.0, 2, 3], [4.0, 5, 6]]); break;
                case "spectrogram":
                    // Golden inputs must be platform-deterministic: libm transcendentals (Math.Sin et al.)
                    // differ by 1 ulp between Windows CRT and glibc, which flips the shortest round-trip
                    // representation and breaks the byte-for-byte golden compare on Linux CI. Pure
                    // integer arithmetic is bit-identical everywhere.
                    ax.Spectrogram(Enumerable.Range(0, 64).Select(i => ((i * 37 % 100) - 50) / 50.0).ToArray(), 1000);
                    break;
                case "table": ax.Table([["a", "b"], ["c", "d"]]); break;
                case "tricontour": ax.Tricontour([0.0, 1, 0.5], [0.0, 0, 1], [1.0, 2, 3]); break;
                case "tripcolor": ax.Tripcolor([0.0, 1, 0.5], [0.0, 0, 1], [1.0, 2, 3]); break;
                case "quiverkey": ax.QuiverKey(0.5, 0.9, 1.0, "key"); break;
                case "barbs": ax.Barbs([0.0], [0.0], [10.0], [45.0]); break;
                case "stem3d": ax.Stem3D([1.0, 2], [3.0, 4], [5.0, 6]); break;
                case "bar3d": ax.Bar3D([0.0, 1.0], [0.0, 1.0], [1.0, 2.0]); break;
                case "signal-xy": ax.SignalXY([0.0, 1.0, 2.0], [1.0, 2.0, 3.0]); break;
                case "signal": ax.Signal([1.0, 2.0, 3.0], 1.0, 0.0); break;
                case "polarheatmap": ax.PolarHeatmap(Grid2X2, 4, 2); break;
                case "line3d": ax.Plot3D([1.0, 2], [3.0, 4], [5.0, 6]); break;
                case "trisurf": ax.Trisurf([0.0, 1, 2], [0.0, 1, 2], [0.0, 1, 4]); break;
                case "contour3d": ax.Contour3D([0.0, 1.0], [0.0, 1.0], Grid2X2Alt); break;
                case "quiver3d": ax.Quiver3D([0.0], [0.0], [0.0], [1.0], [1.0], [1.0]); break;
                case "voxels":
                    var filled = new bool[2, 2, 2];
                    filled[0, 0, 0] = true;
                    filled[1, 1, 1] = true;
                    ax.Voxels(filled);
                    break;
                case "text3d": ax.Text3D(1, 2, 3, "hello"); break;
                case "planarbar3d": ax.PlanarBar3D([0.0, 1.0], [0.0, 1.0], [1.0, 2.0]); break;
                case "streaming_line": ax.StreamingPlot(8, s => { s.AppendPoint(1, 2); s.AppendPoint(2, 3); }); break;
                case "streaming_scatter": ax.StreamingScatter(8, s => { s.AppendPoint(1, 2); s.AppendPoint(2, 3); }); break;
                case "streaming_candlestick": ax.StreamingCandlestick(8, s => s.AppendBar(10, 12, 9, 11)); break;
                case "streaming_signal": ax.StreamingSignal(8, 1.0, s => { s.AppendSample(1); s.AppendSample(2); }); break;
                case "waffle": ax.Waffle(["A", "B", "C"], [37.0, 33.0, 30.0]); break;
                case "pointandfigure": ax.PointAndFigure([20.4, 20.9, 22.3, 23.0, 21.0, 24.4, 21.0, 20.2, 18.6, 21.9, 22.0, 25.8, 22.0, 22.9, 15.1], 1); break;
                case "treegrid": ax.TreeGrid([new TreeGridRow("A", ["1", "2"]), new TreeGridRow("B", ["3", "4"])]); break;
                default: throw new ArgumentOutOfRangeException(nameof(discriminator), discriminator, "no sample series of that kind");
            }

            return ax;
        }
    }
}
