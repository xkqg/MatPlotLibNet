// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Benchmarks.LiveWall;

/// <summary>What ONE draw of each part of a live wall costs: the live trend after one beat, and the three figures a wall
/// builds again on every draw, each with and without its SVG. A build-only row next to its full row says how much of
/// the draw is the model and how much is the writer.</summary>
[MemoryDiagnoser]
public class LiveWallBenchmarks
{
    private LiveWall _wall = default!;

    [GlobalSetup]
    public void Setup() => _wall = new LiveWall(LiveWallShape.Operations, new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));

    [Benchmark(Baseline = true)]
    public string TrendBeat()
    {
        _wall.Advance();
        return _wall.Trend.ToSvg();
    }

    [Benchmark]
    public string Panels() => _wall.Panels().ToSvg();

    /// <summary>The same panels with the legend at <see cref="LegendPosition.Best"/>: the layout weighs every corner against the
    /// lines instead of taking the one it is told, which the wall's own top-centre legend never asks for.</summary>
    [Benchmark]
    public string PanelsBestLegend() => _wall.Panels(LegendPosition.Best).ToSvg();

    [Benchmark]
    public Figure PanelsBuildOnly() => _wall.Panels();

    [Benchmark]
    public string LogPanels() => _wall.LogPanels().ToSvg();

    [Benchmark]
    public string Tiles() => _wall.Tiles().ToSvg();

    [Benchmark]
    public Figure TilesBuildOnly() => _wall.Tiles();

    [Benchmark]
    public string Surface() => _wall.Surface().ToSvg();
}
