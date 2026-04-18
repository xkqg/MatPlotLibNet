// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using MatPlotLibNet.Tests.Rendering.Svg.Interaction;

namespace MatPlotLibNet.Tests.Benchmarks;

/// <summary>Phase K of the v1.7.2 follow-on plan — interaction-path benchmarks
/// (user-requested). Sits alongside <see cref="V170Benchmarks"/> so the full
/// interaction stack (server-side figure build → script emission → client-side
/// reproject) has measured baselines going forward.
/// <para>Thresholds are CI-aware: dev-machine budgets catch real regressions
/// early, while shared GitHub-runners (2-core VMs, 2-3× slower than a dev
/// laptop) use a generous 5× headroom to absorb scheduling jitter. The
/// <c>GITHUB_ACTIONS</c> env var is set by every GitHub-Actions runner and
/// is the canonical CI detector.</para></summary>
public sealed class InteractionBenchmarks
{
    /// <summary>True when running on a shared CI runner (GitHub Actions).
    /// Perf assertions use a looser threshold here because a 2-core VM
    /// shared with other processes has unavoidable per-run jitter that
    /// would otherwise produce flaky failures on a genuinely healthy code
    /// path.</summary>
    private static readonly bool IsCi =
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"))
        || string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>How long it takes to fully build the Jint harness for a 3D Surface:
    /// includes server render, SVG parse, script extraction + execution with the
    /// full matplotlib projection port (Phase B.4). This is the cold-start latency
    /// every behavioural test pays.</summary>
    [Fact]
    public void Harness_3DSurface_ColdStart()
    {
        const int n = 20;
        var sx = Enumerable.Range(0, n).Select(i => -3.0 + 6.0 * i / (n - 1)).ToArray();
        var sy = Enumerable.Range(0, n).Select(i => -3.0 + 6.0 * i / (n - 1)).ToArray();
        var sz = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                double r = Math.Sqrt(sx[i] * sx[i] + sy[j] * sy[j]);
                sz[i, j] = r < 1e-10 ? 1.0 : Math.Sin(r) / r;
            }

        var sw = Stopwatch.StartNew();
        const int iterations = 20;
        for (int i = 0; i < iterations; i++)
        {
            using var h = InteractionScriptHarness.FromBuilder(b => b
                .WithSize(600, 500)
                .WithBrowserInteraction()
                .AddSubPlot(1, 1, 1, ax => ax
                    .WithCamera(elevation: 35, azimuth: -50)
                    .Surface(sx, sy, sz)));
        }
        sw.Stop();
        double avgMs = sw.Elapsed.TotalMilliseconds / iterations;
        Console.WriteLine($"Harness 3D Surface cold-start: {avgMs:F1}ms/figure ({iterations} iterations)");
        double coldStartBudget = IsCi ? 7500 : 1500;
        Assert.True(avgMs < coldStartBudget,
            $"harness cold-start should stay under {coldStartBudget}ms, got {avgMs:F1}ms");
    }

    /// <summary>Per-drag latency: after cold-start, how long does one
    /// pointerdown→pointermove→pointerup cycle take through Jint with the full
    /// matplotlib projection + Phase F tier-subgroup depth resort?</summary>
    [Fact]
    public void DragReproject_20x20Surface_ThroughputPerDrag()
    {
        const int n = 20;
        var sx = Enumerable.Range(0, n).Select(i => -3.0 + 6.0 * i / (n - 1)).ToArray();
        var sy = Enumerable.Range(0, n).Select(i => -3.0 + 6.0 * i / (n - 1)).ToArray();
        var sz = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                double r = Math.Sqrt(sx[i] * sx[i] + sy[j] * sy[j]);
                sz[i, j] = r < 1e-10 ? 1.0 : Math.Sin(r) / r;
            }

        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithBrowserInteraction()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 35, azimuth: -50, distance: 8)
                .Surface(sx, sy, sz)));

        var sw = Stopwatch.StartNew();
        const int iterations = 50;
        for (int i = 0; i < iterations; i++)
        {
            h.Simulate(".mpl-3d-scene", "pointerdown", e => { e.clientX = 100; e.clientY = 100; });
            h.Simulate(".mpl-3d-scene", "pointermove", e => { e.clientX = 110 + i; e.clientY = 102; });
            h.Simulate(".mpl-3d-scene", "pointerup",   e => { e.clientX = 110 + i; e.clientY = 102; });
        }
        sw.Stop();
        double avgMs = sw.Elapsed.TotalMilliseconds / iterations;
        Console.WriteLine($"3D drag reproject (20x20 surface, 400 quads + axis infra): {avgMs:F2}ms/drag-cycle ({iterations} iterations)");
        // Typical runtime: ~24ms on a warm dev laptop, but observed 70–82ms under
        // Windows scheduler jitter + other processes. The benchmark is meant to
        // catch O(n²) / catastrophic regressions (5×+ slowdown), not regress on
        // normal OS noise. Dev budget 150ms (6× typical) absorbs jitter while
        // still tripping on a real 300 ms+ regression. CI uses 250 ms because
        // shared 2-core VMs are inherently slower AND the coverage job
        // collaborates with dotnet-coverage which adds overhead.
        double dragBudget = IsCi ? 250 : 150;
        Assert.True(avgMs < dragBudget,
            $"drag reproject should stay under {dragBudget}ms, got {avgMs:F2}ms");
    }

    /// <summary>2D wheel-zoom throughput — pure viewBox math, should be microseconds.</summary>
    [Fact]
    public void WheelZoom_2D_Throughput()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithZoomPan()
            .Plot(Enumerable.Range(0, 1000).Select(i => (double)i).ToArray(),
                  Enumerable.Range(0, 1000).Select(i => Math.Sin(i * 0.01)).ToArray()));

        var sw = Stopwatch.StartNew();
        const int iterations = 200;
        for (int i = 0; i < iterations; i++)
            h.Simulate("svg", "wheel", e => { e.deltaY = i % 2 == 0 ? -100 : 100; e.clientX = 100; e.clientY = 80; });
        sw.Stop();
        double avgUs = sw.Elapsed.TotalMicroseconds / iterations;
        Console.WriteLine($"2D wheel-zoom (1000-point line): {avgUs:F1}µs/wheel-event ({iterations} iterations)");
        double wheelBudgetUs = IsCi ? 25000 : 5000;
        Assert.True(avgUs < wheelBudgetUs,
            $"wheel-zoom should stay under {wheelBudgetUs}µs/event, got {avgUs:F1}µs");
    }

    /// <summary>Sankey hover BFS traversal — how fast does <c>highlight(nodeId)</c>
    /// + <c>restore()</c> run on a moderately-connected graph?</summary>
    [Fact]
    public void SankeyHover_Throughput()
    {
        var nodes = Enumerable.Range(0, 20)
            .Select(i => new MatPlotLibNet.Models.SankeyNode($"N{i}"))
            .ToArray();
        var links = new List<MatPlotLibNet.Models.SankeyLink>();
        for (int i = 0; i < 15; i++)
            for (int j = i + 1; j < Math.Min(i + 5, 20); j++)
                links.Add(new MatPlotLibNet.Models.SankeyLink(i, j, 1.0 + i * 0.1));

        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithSize(800, 600)
            .WithSankeyHover()
            .AddSubPlot(1, 1, 1, ax => ax.Sankey(nodes, links.ToArray()).HideAllAxes()));

        var sw = Stopwatch.StartNew();
        const int iterations = 100;
        for (int i = 0; i < iterations; i++)
        {
            var node = h.Document.querySelector($"[data-sankey-node-id='{i % nodes.Length}']")!;
            node.Fire(new DomEvent("mouseenter") { target = node });
            node.Fire(new DomEvent("mouseleave") { target = node });
        }
        sw.Stop();
        double avgUs = sw.Elapsed.TotalMicroseconds / iterations;
        Console.WriteLine($"Sankey hover + restore (20 nodes, ~75 links): {avgUs:F1}µs/cycle ({iterations} iterations)");
        double sankeyBudgetUs = IsCi ? 50000 : 10000;
        Assert.True(avgUs < sankeyBudgetUs,
            $"Sankey hover should stay under {sankeyBudgetUs}µs/cycle, got {avgUs:F1}µs");
    }
}
