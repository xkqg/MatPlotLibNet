// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using MatPlotLibNet.Tests.Rendering.Svg.Interaction;

namespace MatPlotLibNet.Tests.Benchmarks;

/// <summary>Interaction-path runs: server-side figure build, script emission and the client-side reproject, run through the
/// Jint harness. Each test asserts that the work was done (the scene is there, the events reached it) and prints how long it
/// took. It does not judge the time: a verdict on a clock reads the load of the box as much as the code, and the same cold
/// start took 38 ms alone and 2,295 ms beside a rebuild. The speed of a draw is measured in the benchmark project, where the
/// process CPU time is read and compared; <see cref="MatPlotLibNet.Tests.Packaging.WallClockContractTests"/> holds that no test asserts on a clock.</summary>
public sealed class InteractionBenchmarks
{
    /// <summary>The Jint harness builds for a 3D Surface: server render, SVG parse, script extraction and execution with the
    /// full matplotlib projection port. This is the cold start every behavioural test pays; the time is printed.</summary>
    [Fact]
    public void Harness_3DSurface_ColdStart()
    {
        var (sx, sy, sz) = SampleSurface.Sinc();

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
            Assert.NotNull(h.Document.querySelector(".mpl-3d-scene"));
        }
        sw.Stop();
        double avgMs = sw.Elapsed.TotalMilliseconds / iterations;
        Console.WriteLine($"Harness 3D Surface cold-start: {avgMs:F1}ms/figure ({iterations} iterations)");
    }

    /// <summary>One pointerdown, pointermove, pointerup cycle through Jint with the full matplotlib projection and the
    /// tier-subgroup depth resort: each event reaches the scene, and the time per cycle is printed.</summary>
    [Fact]
    public void DragReproject_20x20Surface_ThroughputPerDrag()
    {
        var (sx, sy, sz) = SampleSurface.Sinc();

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
            Assert.Equal(1, h.Simulate(".mpl-3d-scene", "pointerdown", e => { e.clientX = 100; e.clientY = 100; }));
            Assert.Equal(1, h.Simulate(".mpl-3d-scene", "pointermove", e => { e.clientX = 110 + i; e.clientY = 102; }));
            Assert.Equal(1, h.Simulate(".mpl-3d-scene", "pointerup",   e => { e.clientX = 110 + i; e.clientY = 102; }));
        }
        sw.Stop();
        double avgMs = sw.Elapsed.TotalMilliseconds / iterations;
        Console.WriteLine($"3D drag reproject (20x20 surface, 400 quads + axis infra): {avgMs:F2}ms/drag-cycle ({iterations} iterations)");
    }

    /// <summary>2D wheel-zoom: pure viewBox math, a few microseconds an event. Each wheel event reaches the chart's svg.</summary>
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
            Assert.Equal(1, h.Simulate("svg", "wheel", e => { e.deltaY = i % 2 == 0 ? -100 : 100; e.clientX = 100; e.clientY = 80; }));
        sw.Stop();
        double avgUs = sw.Elapsed.TotalMicroseconds / iterations;
        Console.WriteLine($"2D wheel-zoom (1000-point line): {avgUs:F1}µs/wheel-event ({iterations} iterations)");
    }

    /// <summary>Sankey hover: <c>highlight(nodeId)</c> and <c>restore()</c> on a moderately connected graph, every node found.</summary>
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
            var node = h.Document.querySelector($"[data-sankey-node-id='{i % nodes.Length}']");
            Assert.NotNull(node);
            node!.Fire(new DomEvent("mouseenter") { target = node });
            node.Fire(new DomEvent("mouseleave") { target = node });
        }
        sw.Stop();
        double avgUs = sw.Elapsed.TotalMicroseconds / iterations;
        Console.WriteLine($"Sankey hover + restore (20 nodes, ~75 links): {avgUs:F1}µs/cycle ({iterations} iterations)");
    }
}
