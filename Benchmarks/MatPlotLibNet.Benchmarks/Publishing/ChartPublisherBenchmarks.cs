// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet.AspNetCore;
using MatPlotLibNet.Benchmarks.LiveWall;
using Wall = MatPlotLibNet.Benchmarks.LiveWall.LiveWall;
using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Benchmarks.Publishing;

/// <summary>What one publish of a live-wall figure costs the server: the document drawn (<c>RenderDocument</c>), compared with the
/// one last sent (the patch), and handed to a viewer that patches and one that takes the whole document, over a hub with no
/// connection behind it. The rows beside the full publish say how much of it is the draw.</summary>
[MemoryDiagnoser]
public class ChartPublisherBenchmarks
{
    private const string Trend = "trend";
    private const string Panels = "panels";
    private const string Surface = "surface";
    private readonly SvgTransform _svg = new();
    private Wall _wall = default!;
    private Figure _panelFigure = default!;
    private Figure _surfaceFigure = default!;
    private ChartPublisher _publisher = default!;

    [GlobalSetup]
    public void Setup()
    {
        _wall = new Wall(LiveWallShape.Operations, new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));
        _panelFigure = _wall.Panels();
        _surfaceFigure = _wall.Surface();

        var subscriptions = new ChartSubscriptions();
        foreach (var chart in new[] { Trend, Panels, Surface })
        {
            subscriptions.Subscribe(chart, "plain-viewer");
            subscriptions.SubscribeToParts(chart, "patching-viewer");
        }
        _publisher = new ChartPublisher(new StandInHub(new CountingClient()), ChartSerializerDefault.Current, _svg, subscriptions);

        // A first publish of each figure, so the rows below measure the steady state: a viewer already holds a picture.
        _publisher.PublishSvgAsync(Trend, _wall.Trend).GetAwaiter().GetResult();
        _publisher.PublishSvgAsync(Panels, _panelFigure).GetAwaiter().GetResult();
        _publisher.PublishSvgAsync(Surface, _surfaceFigure).GetAwaiter().GetResult();
    }

    /// <summary>The trend after one beat, drawn into a document and nothing else.</summary>
    [Benchmark(Baseline = true)]
    public object TrendBeatRenderDocument()
    {
        _wall.Advance();
        return _svg.RenderDocument(_wall.Trend);
    }

    /// <summary>The trend after one beat, published: drawn, compared with the last document, sent to both kinds of viewer.</summary>
    [Benchmark]
    public Task TrendBeatPublish()
    {
        _wall.Advance();
        return _publisher.PublishSvgAsync(Trend, _wall.Trend);
    }

    /// <summary>The seven line panels drawn into a document and nothing else.</summary>
    [Benchmark]
    public object PanelsRenderDocument() => _svg.RenderDocument(_panelFigure);

    /// <summary>The seven line panels published again with nothing changed: every subplot is compared and nothing is sent.</summary>
    [Benchmark]
    public Task PanelsPublishUnchanged() => _publisher.PublishSvgAsync(Panels, _panelFigure);

    /// <summary>The lit surface published again with nothing changed.</summary>
    [Benchmark]
    public Task SurfacePublishUnchanged() => _publisher.PublishSvgAsync(Surface, _surfaceFigure);

    /// <summary>The seven line panels built again, as a wall builds them on every draw, and published: the whole draw, then the
    /// comparison, which finds the same markup and sends nothing.</summary>
    [Benchmark]
    public Task PanelsRebuiltPublish() => _publisher.PublishSvgAsync(Panels, _wall.Panels());

    /// <summary>The lit surface built again and published.</summary>
    [Benchmark]
    public Task SurfaceRebuiltPublish() => _publisher.PublishSvgAsync(Surface, _wall.Surface());
}
