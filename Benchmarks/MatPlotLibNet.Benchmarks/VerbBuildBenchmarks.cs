// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using BenchmarkDotNet.Attributes;
using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Benchmarks;

/// <summary>What three verbs cost to build, without drawing: a Kagi chart of ten thousand closes, a matrix of a thousand rows
/// shown with its row numbers, and the canvas size of a 600-leaf treemap. Each is paid again every time an application builds
/// its figure for a frame.</summary>
[MemoryDiagnoser]
public class VerbBuildBenchmarks
{
    private double[] _closes = default!;
    private double[,] _matrix = default!;
    private TreeNode _tree = default!;
    private string[] _columns = default!;
    private BarGroup[] _segments = default!;
    private BarGroup[] _rows = default!;
    private string[] _dimensions = default!;
    private double[] _sample = default!;
    private double[] _x = default!;
    private double[] _y = default!;
    private double[,] _sparse = default!;
    private DateTime[] _eventDays = default!;
    private string[] _eventKinds = default!;
    private double[] _signal = default!;
    private double[] _meshX = default!;
    private double[] _meshY = default!;
    private int[] _meshTriangles = default!;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(7);
        _closes = new double[10_000];
        double price = 100;
        for (int i = 0; i < _closes.Length; i++)
        {
            price += random.NextDouble() - 0.5;
            _closes[i] = price;
        }

        _matrix = new double[1000, 4];
        for (int row = 0; row < 1000; row++)
        {
            for (int column = 0; column < 4; column++)
            {
                _matrix[row, column] = (row * 3 + column * 7) % 11;
            }
        }

        _columns = [.. Enumerable.Range(0, 40).Select(c => "C" + c.ToString(CultureInfo.InvariantCulture))];
        _segments = [.. Enumerable.Range(0, 8).Select(g => new BarGroup("S" + g.ToString(CultureInfo.InvariantCulture),
            [.. Enumerable.Range(0, 40).Select(c => (double)((g * 7 + c * 3) % 11 + 1))]))];
        _dimensions = [.. Enumerable.Range(0, 6).Select(d => "D" + d.ToString(CultureInfo.InvariantCulture))];
        _rows = [.. Enumerable.Range(0, 200).Select(r => new BarGroup("class " + (r % 3).ToString(CultureInfo.InvariantCulture),
            [.. Enumerable.Range(0, 6).Select(d => (double)((r * (d + 3)) % 97))]))];
        _sample = [.. Enumerable.Range(0, 10_000).Select(_ => random.NextDouble() * 100)];
        _x = [.. Enumerable.Range(0, 10_000).Select(i => (double)i)];
        _y = [.. _x.Select(v => 50 * Math.Sin(v / 300) + random.NextDouble())];

        _sparse = new double[400, 400];
        for (int row = 0; row < 400; row++)
        {
            for (int column = 0; column < 400; column++)
            {
                _sparse[row, column] = (row * 31 + column * 17) % 7 == 0 ? 1 : 0;
            }
        }

        var first = new DateTime(2025, 1, 6);
        _eventDays = [.. Enumerable.Range(0, 2000).Select(i => first.AddDays(random.Next(0, 365)))];
        _eventKinds = [.. Enumerable.Range(0, 2000).Select(i => "kind " + (i % 5).ToString(CultureInfo.InvariantCulture))];
        _signal = [.. Enumerable.Range(0, 16_384).Select(i => Math.Sin(2 * Math.PI * 50 * i / 1000.0) + 0.3 * random.NextDouble())];

        // A 70 by 70 grid of points, each cell cut into two triangles: 9,522 triangles, as a caller's own mesh.
        _meshX = new double[70 * 70];
        _meshY = new double[70 * 70];
        for (int i = 0; i < 70 * 70; i++)
        {
            _meshX[i] = i % 70;
            _meshY[i] = i / 70;
        }

        var triangles = new List<int>();
        for (int row = 0; row < 69; row++)
        {
            for (int column = 0; column < 69; column++)
            {
                int a = row * 70 + column, b = a + 1, c = a + 70, d = c + 1;
                triangles.AddRange([a, b, c, b, d, c]);
            }
        }

        _meshTriangles = [.. triangles];

        _tree = new TreeNode
        {
            Label = "Root",
            Children = [.. Enumerable.Range(0, 12).Select(g => new TreeNode
            {
                Label = "Group " + g.ToString(CultureInfo.InvariantCulture),
                Children = [.. Enumerable.Range(0, 10).Select(h => new TreeNode
                {
                    Label = "Sub " + h.ToString(CultureInfo.InvariantCulture),
                    Children = [.. Enumerable.Range(0, 5).Select(l => new TreeNode { Label = "Leaf-" + l.ToString(CultureInfo.InvariantCulture), Value = (g * 7 + h * 3 + l * 5) % 9 + 1 })],
                })],
            })],
        };
    }

    /// <summary>A Kagi chart of 10,000 closes with a reversal of 0.5, which turns a few thousand times.</summary>
    [Benchmark]
    public Figure Kagi_10k() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Kagi(_closes, 0.5)).Build();

    /// <summary>A matrix of 1,000 rows by 4 columns shown as a heatmap with whole-number ticks on both axes.</summary>
    [Benchmark]
    public Figure MatShow_1000() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.MatShow(_matrix)).Build();

    /// <summary>The canvas of a 600-leaf treemap sized to its labels.</summary>
    [Benchmark]
    public Figure TreemapAutoSize() => Plt.Create().WithAutoSize(_tree).Build();

    /// <summary>A Marimekko chart of 40 columns by 8 segments: the totals, the edges and the cells of every segment.</summary>
    [Benchmark]
    public Figure Marimekko_40x8() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Marimekko(_columns, _segments)).Build();

    /// <summary>Parallel coordinates of 200 rows over 6 dimensions: every column scaled, three classes' lines, the ticks.</summary>
    [Benchmark]
    public Figure ParallelCoordinates_200x6() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.ParallelCoordinates(_dimensions, _rows)).Build();

    /// <summary>A probability plot of 10,000 observations against the exponential distribution: the sorted sample, the
    /// quantile of every plotting position and the least-squares line.</summary>
    [Benchmark]
    public Figure ProbabilityPlot_10k() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.ProbabilityPlot(_sample, QQReference.Exponential)).Build();

    /// <summary>A stem-and-leaf display of 10,000 observations: the rows, and the table of their cells.</summary>
    [Benchmark]
    public Figure StemAndLeaf_10k() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.StemAndLeaf(_sample)).Build();

    /// <summary>A horizon chart of 10,000 points folded into three bands on each side of zero.</summary>
    [Benchmark]
    public Figure Horizon_10k() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Horizon(_x, _y)).Build();

    /// <summary>A Renko chart of 10,000 closes with bricks of 0.5: the bricks of both directions as floating bars.</summary>
    [Benchmark]
    public Figure Renko_10k() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Renko(_closes, 0.5)).Build();

    /// <summary>A calendar of 2,000 events of five kinds over a year: the grid of days and the stripe of every event.</summary>
    [Benchmark]
    public Figure CalendarEvents_2000() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.CalendarEvents(_eventDays, _eventKinds)).Build();

    /// <summary>A spy plot of a 400 by 400 matrix, one cell in seven marked.</summary>
    [Benchmark]
    public Figure Spy_400x400() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Spy(_sparse)).Build();

    /// <summary>The magnitude spectrum of 16,384 samples in decibels.</summary>
    [Benchmark]
    public Figure MagnitudeDecibels_16k() =>
        Plt.Create().AddSubPlot(1, 1, 1, ax => ax.MagnitudeSpectrum(_signal, new SpectrumOptions { SampleRate = 1000 }, MagnitudeScale.Decibel)).Build();

    /// <summary>The triangle plot of a mesh of 9,522 triangles given by the caller: each edge once.</summary>
    [Benchmark]
    public Figure Triplot_9522() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Triplot(_meshX, _meshY, _meshTriangles)).Build();
}
