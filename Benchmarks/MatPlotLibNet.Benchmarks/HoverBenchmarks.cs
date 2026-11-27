// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Benchmarks;

/// <summary>What a hover costs the server: the nearest point of a figure with three lines of 360 points, found from the layout
/// of a draw, for a cursor on a line, a cursor far from every line, and a sweep of the cursor across the plot.</summary>
[MemoryDiagnoser]
public class HoverBenchmarks
{
    private const int Points = 360;
    private const int SweepSteps = 20;
    private Figure _figure = default!;
    private ChartLayout _layout = default!;

    [GlobalSetup]
    public void Setup()
    {
        var t = Enumerable.Range(0, Points).Select(i => (double)i).ToArray();
        _figure = Plt.Create().WithSize(1000, 400)
            .AddSubPlot(1, 1, 1, ax =>
            {
                for (var line = 0; line < 3; line++)
                {
                    var shift = line;
                    ax.Plot(t, t.Select(v => Math.Sin((v / 20) + shift)).ToArray(), s => s.Label = $"line {shift + 1}");
                }
            })
            .Build();
        _layout = ChartLayout.Create(_figure, ChartRendererDefault.Current.ComputeLayout(_figure, new SvgRenderContext()));
    }

    [Benchmark(Baseline = true)]
    public NearestPointResult? OnALine() => NearestPointFinder.Find(_figure, 0, 100, Math.Sin(100.0 / 20), _layout);

    [Benchmark]
    public NearestPointResult? FarFromEveryLine() => NearestPointFinder.Find(_figure, 0, 100, 5, _layout);

    [Benchmark]
    public int Sweep()
    {
        var found = 0;
        for (var step = 0; step < SweepSteps; step++)
        {
            var x = (double)step * (Points - 1) / (SweepSteps - 1);
            if (NearestPointFinder.Find(_figure, 0, x, Math.Sin(x / 20), _layout) is not null) found++;
        }
        return found;
    }
}
