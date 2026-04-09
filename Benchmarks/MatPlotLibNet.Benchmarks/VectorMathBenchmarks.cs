// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Benchmarks;

/// <summary>Benchmarks for SIMD-accelerated Vec public API and indicator proxies for internal VectorMath.</summary>
/// <remarks>Vec operators/reductions are SIMD-accelerated via TensorPrimitives.
/// Indicator benchmarks proxy the internal VectorMath domain algorithms (RollingMean, RollingStdDev, RollingMin/Max).</remarks>
[MemoryDiagnoser]
public class VectorMathBenchmarks
{
    private double[] _close = default!;
    private double[] _high = default!;
    private double[] _low = default!;
    private Vec _x = default!;
    private Vec _y = default!;

    [Params(1_000, 10_000, 100_000)]
    public int DataSize;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        var xArr = Enumerable.Range(0, DataSize).Select(_ => random.NextDouble() * 200 - 100).ToArray();
        var yArr = Enumerable.Range(0, DataSize).Select(_ => random.NextDouble() * 200 - 100).ToArray();
        _x = xArr;
        _y = yArr;

        double price = 100;
        _close = new double[DataSize];
        _high = new double[DataSize];
        _low = new double[DataSize];
        for (int i = 0; i < DataSize; i++)
        {
            var change = (random.NextDouble() - 0.48) * 2;
            _high[i] = price + Math.Abs(change) + random.NextDouble();
            _low[i] = price - Math.Abs(change) - random.NextDouble();
            price += change;
            _close[i] = price;
        }
    }

    // --- Vec SIMD operators (backed by TensorPrimitives) ---

    [Benchmark(Baseline = true)]
    public Vec Vec_Add_ElementWise() => _x + _y;

    [Benchmark]
    public Vec Vec_MultiplyScalar() => _x * 1.5;

    [Benchmark]
    public Vec Vec_Operators() => (_x + _y) * 1.5 - _y;

    [Benchmark]
    public double Vec_Sum() => _x.Sum();

    [Benchmark]
    public double Vec_Min() => _x.Min();

    [Benchmark]
    public double Vec_Max() => _x.Max();

    [Benchmark]
    public double Vec_Mean() => _x.Mean();

    [Benchmark]
    public double Vec_Std() => _x.Std();

    // --- Vec LINQ-style (scalar loops) ---

    [Benchmark]
    public Vec Vec_Select_Lambda() => _x.Select(v => v * 2 + 1);

    [Benchmark]
    public Vec Vec_Where_Lambda() => _x.Where(v => v > 0);

    // --- Domain algorithms via indicator proxies ---
    // These exercise VectorMath.RollingMean, RollingStdDev, RollingMin/Max

    [Benchmark]
    public double[] RollingMean_via_Sma() => new Sma(_close, 20).Compute();

    [Benchmark]
    public BandsResult RollingStdDev_via_BollingerBands() => new BollingerBands(_close, 20).Compute();

    [Benchmark]
    public StochasticResult RollingMinMax_via_Stochastic() => new Stochastic(_high, _low, _close, 14).Compute();
}
