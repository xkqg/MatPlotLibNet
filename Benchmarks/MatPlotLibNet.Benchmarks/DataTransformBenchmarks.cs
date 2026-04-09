// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Benchmarks;

/// <summary>Benchmarks comparing per-point DataToPixel vs SIMD batch TransformBatch.</summary>
[MemoryDiagnoser]
public class DataTransformBenchmarks
{
    private double[] _xData = default!;
    private double[] _yData = default!;
    private DataTransform _transform = default!;

    [Params(1_000, 10_000, 100_000)]
    public int DataSize;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        _xData = Enumerable.Range(0, DataSize).Select(i => (double)i).ToArray();
        _yData = Enumerable.Range(0, DataSize).Select(_ => random.NextDouble() * 100).ToArray();

        // Simulate a typical 800×400 plot area with data from 0..DataSize-1 on X and 0..100 on Y
        _transform = new DataTransform(
            dataXMin: 0, dataXMax: DataSize - 1,
            dataYMin: 0, dataYMax: 100,
            plotBounds: new Rect(50, 20, 700, 360));
    }

    [Benchmark(Baseline = true)]
    public Point[] PerPoint_Loop()
    {
        var result = new Point[_xData.Length];
        for (int i = 0; i < _xData.Length; i++)
            result[i] = _transform.DataToPixel(_xData[i], _yData[i]);
        return result;
    }

    [Benchmark]
    public Point[] TransformBatch() => _transform.TransformBatch(_xData, _yData);
}
