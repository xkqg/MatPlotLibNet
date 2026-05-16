// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Benchmarks;

/// <summary>v1.11 — quantifies the cost of <see cref="RelativeRotationSeries"/>
/// compute pipeline (DualEma / ZScore / LogReturn) and full SVG render across
/// asset-count and bar-count axes. Typical use case: 5–20 assets, 52–104 weekly bars.
/// The benchmark matrix surfaces where each formula's O(N·A) cost lives
/// and informs the "DO NOT EXCEED A≈50 assets at B=500 bars" cookbook rule.</summary>
[MemoryDiagnoser]
public class RelativeRotationBenchmarks
{
    private double[][] _assets2B50   = default!;
    private double[][] _assets5B100  = default!;
    private double[][] _assets10B200 = default!;
    private double[][] _assets20B500 = default!;
    private double[]   _bench50      = default!;
    private double[]   _bench100     = default!;
    private double[]   _bench200     = default!;
    private double[]   _bench500     = default!;
    private string[]   _labels2      = default!;
    private string[]   _labels5      = default!;
    private string[]   _labels10     = default!;
    private string[]   _labels20     = default!;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        _assets2B50   = MakeAssets(2,  50,  rng);
        _assets5B100  = MakeAssets(5,  100, rng);
        _assets10B200 = MakeAssets(10, 200, rng);
        _assets20B500 = MakeAssets(20, 500, rng);
        _bench50  = MakeBench(50,  rng);
        _bench100 = MakeBench(100, rng);
        _bench200 = MakeBench(200, rng);
        _bench500 = MakeBench(500, rng);
        _labels2  = Labels(2);
        _labels5  = Labels(5);
        _labels10 = Labels(10);
        _labels20 = Labels(20);
    }

    private static double[][] MakeAssets(int assets, int bars, Random rng)
    {
        var result = new double[assets][];
        for (int a = 0; a < assets; a++)
        {
            result[a] = new double[bars];
            result[a][0] = 100.0;
            for (int i = 1; i < bars; i++)
                result[a][i] = Math.Max(1.0, result[a][i - 1] * (1.0 + (rng.NextDouble() - 0.49) * 0.04));
        }
        return result;
    }

    private static double[] MakeBench(int bars, Random rng)
    {
        var b = new double[bars];
        b[0] = 100.0;
        for (int i = 1; i < bars; i++)
            b[i] = Math.Max(1.0, b[i - 1] * (1.0 + (rng.NextDouble() - 0.49) * 0.03));
        return b;
    }

    private static string[] Labels(int n) =>
        Enumerable.Range(0, n).Select(i => $"A{i}").ToArray();

    private static string Render(double[][] assets, double[] bench, string[] labels,
        RrgFormula formula = RrgFormula.DualEma) =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
                ax.RelativeRotation(assets, bench, labels, s => s.Formula = formula))
            .ToSvg();

    // ── Compute-only benchmarks (no SVG overhead) ──────────────────────────────

    [Benchmark] public object DualEma_A2_B50()
        => new RelativeRotationSeries(_assets2B50, _bench50, _labels2).ComputeRsData();

    [Benchmark] public object DualEma_A5_B100()
        => new RelativeRotationSeries(_assets5B100, _bench100, _labels5).ComputeRsData();

    [Benchmark] public object DualEma_A10_B200()
        => new RelativeRotationSeries(_assets10B200, _bench200, _labels10).ComputeRsData();

    [Benchmark] public object DualEma_A20_B500()
        => new RelativeRotationSeries(_assets20B500, _bench500, _labels20).ComputeRsData();

    [Benchmark] public object ZScore_A5_B100()
        => new RelativeRotationSeries(_assets5B100, _bench100, _labels5) { Formula = RrgFormula.ZScore }.ComputeRsData();

    [Benchmark] public object LogReturn_A5_B100()
        => new RelativeRotationSeries(_assets5B100, _bench100, _labels5) { Formula = RrgFormula.LogReturn }.ComputeRsData();

    // ── Full render benchmarks (compute + SVG) ─────────────────────────────────

    [Benchmark] public string Render_DualEma_A2_B50()   => Render(_assets2B50,   _bench50,   _labels2);
    [Benchmark] public string Render_DualEma_A5_B100()  => Render(_assets5B100,  _bench100,  _labels5);
    [Benchmark] public string Render_DualEma_A10_B200() => Render(_assets10B200, _bench200,  _labels10);
    [Benchmark] public string Render_ZScore_A5_B100()   => Render(_assets5B100,  _bench100,  _labels5, RrgFormula.ZScore);
    [Benchmark] public string Render_LogReturn_A5_B100()=> Render(_assets5B100,  _bench100,  _labels5, RrgFormula.LogReturn);
}
