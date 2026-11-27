// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Benchmarks;

/// <summary>What building a figure costs, without drawing it: the fluent builders (one line, a 3 x 3 grid, the default axes),
/// two composed verbs (a Pareto chart, a ridgeline), and the factory that makes a figure from JSON (a full grid and a grid of two-point lines). A build is paid again
/// every time an application rebuilds a figure for a frame, so its share of a draw is the share a builder change can move.</summary>
[MemoryDiagnoser]
public class BuilderBenchmarks
{
    private double[] _x = default!;
    private double[] _y = default!;
    private string[] _kinds = default!;
    private double[] _counts = default!;
    private double[] _segmentX = default!;
    private double[] _segmentLow = default!;
    private double[] _segmentHigh = default!;
    private NamedSample[] _groups = default!;
    private string _json = default!;
    private string _jsonTiny = default!;
    private readonly ChartSerializer _serializer = new();

    [GlobalSetup]
    public void Setup()
    {
        _x = [.. Enumerable.Range(0, 200).Select(i => (double)i)];
        _y = [.. _x.Select(v => v % 37)];
        _kinds = [.. Enumerable.Range(0, 12).Select(i => $"kind {i}")];
        _counts = [.. Enumerable.Range(0, 12).Select(i => (double)((i * 37) % 50 + 1))];
        _segmentX = [.. Enumerable.Range(0, 10_000).Select(i => (double)i)];
        _segmentLow = [.. _segmentX.Select(v => v % 11)];
        _segmentHigh = [.. _segmentX.Select(v => v % 11 + 5)];
        _groups = [.. Enumerable.Range(0, 8).Select(g => new NamedSample($"g{g}", [.. Enumerable.Range(0, 300).Select(i => (double)((i * (g + 3)) % 101))]))];
        _json = _serializer.ToJson(Grid3x3Of(_x, _y));
        _jsonTiny = _serializer.ToJson(Grid3x3Of(_x[..2], _y[..2]));
    }

    /// <summary>One line on the default axes, with a title and labels.</summary>
    [Benchmark(Baseline = true)]
    public Figure OneLine() => Plt.Create().WithTitle("load").Plot(_x, _y, s => s.Label = "load").Build();

    /// <summary>Nine subplots of two lines each, labelled, with a legend.</summary>
    [Benchmark]
    public Figure Grid3x3() => Grid3x3Of(_x, _y);

    /// <summary>A Pareto chart: bars in falling order and the cumulative line on a second axis.</summary>
    [Benchmark]
    public Figure Pareto() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Pareto(_kinds, _counts)).Build();

    /// <summary>A line of 10,000 vertical segments (VLines): the points of every segment written as one line.</summary>
    [Benchmark]
    public Figure VLines10K() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.VLines(_segmentX, _segmentLow, _segmentHigh)).Build();

    /// <summary>A ridgeline of eight groups: a density estimate per group, stacked.</summary>
    [Benchmark]
    public Figure Ridgeline() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Ridgeline(_groups)).Build();

    /// <summary>The factory: the 3 x 3 grid made again from its JSON.</summary>
    [Benchmark]
    public Figure FromJson() => _serializer.FromJson(_json);

    /// <summary>The factory on the same 3 x 3 grid with two points per line: the document is nearly all structure, so what the
    /// row costs is the per-series and per-axes work of reading a figure, not the reading of numbers.</summary>
    [Benchmark]
    public Figure FromJsonTiny() => _serializer.FromJson(_jsonTiny);

    private static Figure Grid3x3Of(double[] x, double[] y)
    {
        var builder = Plt.Create().WithSize(900, 900);
        for (int i = 1; i <= 9; i++)
        {
            int panel = i;
            builder.AddSubPlot(3, 3, panel, ax => ax
                .Plot(x, y, s => s.Label = $"a{panel}")
                .Plot(x, x, s => s.Label = $"b{panel}")
                .WithTitle($"panel {panel}").SetXLabel("x").SetYLabel("y").WithLegend());
        }
        return builder.Build();
    }
}
