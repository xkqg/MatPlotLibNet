// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Benchmarks;

/// <summary>v1.10 — quantifies the Scatter-vs-Hexbin trade-off for the off-diagonal
/// cells of <see cref="PairGridSeries"/>. Hexbin trades per-point <c>&lt;circle&gt;</c>
/// emission for one <c>&lt;polygon&gt;</c> per occupied hex bucket — the cliff-point
/// where Hexbin wins is roughly when <c>samples > gridSize²</c> (each hex absorbs
/// many points).</summary>
[MemoryDiagnoser]
public class PairGridBenchmarks
{
    private double[][] _vars3x10K  = default!;
    private double[][] _vars5x10K  = default!;
    private double[][] _vars5x100K = default!;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        _vars3x10K  = MakeVars(rng, n: 3, samples: 10_000);
        _vars5x10K  = MakeVars(rng, n: 5, samples: 10_000);
        _vars5x100K = MakeVars(rng, n: 5, samples: 100_000);
    }

    private static double[][] MakeVars(Random rng, int n, int samples)
    {
        var vars = new double[n][];
        for (int i = 0; i < n; i++)
        {
            vars[i] = new double[samples];
            for (int k = 0; k < samples; k++) vars[i][k] = rng.NextDouble() * 10.0;
        }
        return vars;
    }

    // ── 3 vars × 10K samples (6 off-diagonal cells × 10K points each) ─────────

    [Benchmark]
    public string PairGrid_3x10K_Scatter() =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PairGrid(_vars3x10K, s =>
                s.OffDiagonalKind = PairGridOffDiagonalKind.Scatter))
            .ToSvg();

    [Benchmark]
    public string PairGrid_3x10K_Hexbin() =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PairGrid(_vars3x10K, s =>
                s.OffDiagonalKind = PairGridOffDiagonalKind.Hexbin))
            .ToSvg();

    // ── 5 vars × 10K samples (20 off-diagonal cells × 10K each) ───────────────

    [Benchmark]
    public string PairGrid_5x10K_Scatter() =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PairGrid(_vars5x10K, s =>
                s.OffDiagonalKind = PairGridOffDiagonalKind.Scatter))
            .ToSvg();

    [Benchmark]
    public string PairGrid_5x10K_Hexbin() =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PairGrid(_vars5x10K, s =>
                s.OffDiagonalKind = PairGridOffDiagonalKind.Hexbin))
            .ToSvg();

    // ── 5 vars × 100K samples (where Hexbin should genuinely shine) ───────────

    [Benchmark]
    public string PairGrid_5x100K_Scatter() =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PairGrid(_vars5x100K, s =>
                s.OffDiagonalKind = PairGridOffDiagonalKind.Scatter))
            .ToSvg();

    [Benchmark]
    public string PairGrid_5x100K_Hexbin() =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PairGrid(_vars5x100K, s =>
                s.OffDiagonalKind = PairGridOffDiagonalKind.Hexbin))
            .ToSvg();

    // ── HexbinGridSize sensitivity (5×10K dataset) ─────────────────────────────

    [Benchmark]
    public string PairGrid_5x10K_Hexbin_GridSize10() =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PairGrid(_vars5x10K, s =>
            {
                s.OffDiagonalKind = PairGridOffDiagonalKind.Hexbin;
                s.HexbinGridSize  = 10;
            }))
            .ToSvg();

    [Benchmark]
    public string PairGrid_5x10K_Hexbin_GridSize30() =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PairGrid(_vars5x10K, s =>
            {
                s.OffDiagonalKind = PairGridOffDiagonalKind.Hexbin;
                s.HexbinGridSize  = 30;
            }))
            .ToSvg();
}
