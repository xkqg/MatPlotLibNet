// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Benchmarks;

/// <summary>
/// Measures rendering throughput for <see cref="SignalSeries"/> and <see cref="SignalXYSeries"/>
/// at 100k, 1M, and 10M points with narrow (10% of span) and wide (full span) viewports.
/// Target: 10M narrow viewport &lt; 16 ms/op, zero gen-2 GC allocations.
/// </summary>
[MemoryDiagnoser]
public class SignalSeriesBenchmarks
{
    private Figure _signal100kNarrow = default!;
    private Figure _signal100kWide   = default!;
    private Figure _signal1MNarrow   = default!;
    private Figure _signal1MWide     = default!;
    private Figure _signal10MNarrow  = default!;
    private Figure _signalXY1MNarrow = default!;
    private Figure _signalXY1MWide   = default!;

    [GlobalSetup]
    public void Setup()
    {
        // ── 100k uniform signal ───────────────────────────────────────────────
        var y100k = Enumerable.Range(0, 100_000).Select(i => Math.Sin(i * 0.001)).ToArray();
        _signal100kNarrow = BuildSignalNarrow(y100k, sampleRate: 1000.0, xMin: 45.0, xMax: 55.0);
        _signal100kWide = Plt.Create()
            .Signal(y100k, sampleRate: 1000.0, configure: s => s.MaxDisplayPoints = 2000)
            .Build();

        // ── 1M uniform signal ─────────────────────────────────────────────────
        var y1M = Enumerable.Range(0, 1_000_000).Select(i => Math.Sin(i * 0.001)).ToArray();
        _signal1MNarrow = BuildSignalNarrow(y1M, sampleRate: 1000.0, xMin: 450.0, xMax: 550.0);
        _signal1MWide = Plt.Create()
            .Signal(y1M, sampleRate: 1000.0, configure: s => s.MaxDisplayPoints = 2000)
            .Build();

        // ── 10M uniform signal ────────────────────────────────────────────────
        var y10M = Enumerable.Range(0, 10_000_000).Select(i => Math.Sin(i * 0.001)).ToArray();
        _signal10MNarrow = BuildSignalNarrow(y10M, sampleRate: 1000.0, xMin: 4500.0, xMax: 5500.0);

        // ── 1M monotonic-XY signal ────────────────────────────────────────────
        var x1M = Enumerable.Range(0, 1_000_000).Select(i => i * 0.001).ToArray();
        var yXY1M = x1M.Select(Math.Sin).ToArray();
        _signalXY1MNarrow = BuildSignalXYNarrow(x1M, yXY1M, xMin: 450.0, xMax: 550.0);
        _signalXY1MWide = Plt.Create()
            .SignalXY(x1M, yXY1M, s => s.MaxDisplayPoints = 2000)
            .Build();
    }

    // ── Narrow viewport ───────────────────────────────────────────────────────

    [Benchmark]
    public string Signal_100k_Narrow() => _signal100kNarrow.ToSvg();

    [Benchmark]
    public string Signal_1M_Narrow() => _signal1MNarrow.ToSvg();

    [Benchmark]
    public string Signal_10M_Narrow() => _signal10MNarrow.ToSvg();

    [Benchmark]
    public string SignalXY_1M_Narrow() => _signalXY1MNarrow.ToSvg();

    // ── Wide viewport ─────────────────────────────────────────────────────────

    [Benchmark]
    public string Signal_100k_Wide() => _signal100kWide.ToSvg();

    [Benchmark]
    public string Signal_1M_Wide() => _signal1MWide.ToSvg();

    [Benchmark]
    public string SignalXY_1M_Wide() => _signalXY1MWide.ToSvg();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Figure BuildSignalNarrow(double[] y, double sampleRate, double xMin, double xMax)
    {
        var fig = Plt.Create()
            .Signal(y, sampleRate: sampleRate, configure: s => s.MaxDisplayPoints = 2000)
            .Build();
        fig.SubPlots[0].XAxis.Min = xMin;
        fig.SubPlots[0].XAxis.Max = xMax;
        return fig;
    }

    private static Figure BuildSignalXYNarrow(double[] x, double[] y, double xMin, double xMax)
    {
        var fig = Plt.Create()
            .SignalXY(x, y, s => s.MaxDisplayPoints = 2000)
            .Build();
        fig.SubPlots[0].XAxis.Min = xMin;
        fig.SubPlots[0].XAxis.Max = xMax;
        return fig;
    }
}
