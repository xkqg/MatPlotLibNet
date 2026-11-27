// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Benchmarks;

/// <summary>What an axis with breaks costs per point: every value goes through the break map, so it takes the scalar path the
/// plain linear transform replaces with SIMD. The baseline is the same data on the same plot area with no break.</summary>
[MemoryDiagnoser]
public class DataTransformBreaksBenchmarks
{
    private double[] _x = default!;
    private double[] _y = default!;
    private DataTransform _linear = default!;
    private DataTransform _broken = default!;

    [Params(1_000, 10_000)]
    public int DataSize;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        _x = Enumerable.Range(0, DataSize).Select(i => (double)i).ToArray();
        _y = Enumerable.Range(0, DataSize).Select(_ => random.NextDouble() * 100).ToArray();
        var plot = new Rect(50, 20, 700, 360);
        double last = DataSize - 1;

        _linear = new DataTransform(0, last, 0, 100, plot);
        // Two breaks on X and one on Y, the shape of a chart that skips a quiet stretch; the transform is built, as the renderer builds
        // it, on the range that remains once the breaks are taken out.
        var xBreaks = new[] { new AxisBreak(last * 0.30, last * 0.40), new AxisBreak(last * 0.70, last * 0.75) };
        var yBreaks = new[] { new AxisBreak(40, 60) };
        _broken = new DataTransform(
            0, last - (last * 0.15), 0, 80, plot,
            xBreaks, yBreaks,
            0, last, 0, 100);
    }

    [Benchmark(Baseline = true)]
    public Point[] LinearTransformBatch() => _linear.TransformBatch(_x, _y);

    [Benchmark]
    public Point[] BreaksTransformBatch() => _broken.TransformBatch(_x, _y);

    [Benchmark]
    public double[] BreaksTransformX() => _broken.TransformX(_x);

    [Benchmark]
    public double[] BreaksTransformY() => _broken.TransformY(_y);
}
