// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet;
using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Benchmarks;

[MemoryDiagnoser]
public class SvgRenderingBenchmarks
{
    private Figure _simpleLine = default!;
    private Figure _complexChart = default!;
    private Figure _subplotGrid = default!;
    private Figure _treemap = default!;
    private Figure _sunburst = default!;
    private Figure _sankey = default!;
    private Figure _polar = default!;
    private Figure _surface3D = default!;
    private Figure _surface3DLit = default!;
    private Figure _geoMap = default!;
    private Figure _choropleth = default!;
    private Figure _legendChart = default!;
    private Figure _largeLine10K = default!;
    private Figure _largeLine100K = default!;

    [GlobalSetup]
    public void Setup()
    {
        var x = Enumerable.Range(0, 100).Select(i => (double)i).ToArray();
        var y = x.Select(v => Math.Sin(v * 0.1) * 50 + 50).ToArray();

        _simpleLine = Plt.Create()
            .WithTitle("Simple Line")
            .Plot(x, y)
            .Build();

        _complexChart = Plt.Create()
            .WithTitle("Complex")
            .WithTheme(Theme.Seaborn)
            .Plot(x, y, l => { l.Color = Colors.Blue; l.Label = "sin"; })
            .Scatter(x.Take(20).ToArray(), y.Take(20).ToArray(), s => { s.Color = Colors.Red; })
            .Bar(["A", "B", "C", "D"], [10.0, 25, 15, 30])
            .Build();

        _subplotGrid = Plt.Create()
            .WithTitle("3x3 Grid")
            .AddSubPlot(3, 3, 1, ax => ax.Plot(x, y))
            .AddSubPlot(3, 3, 2, ax => ax.Plot(x, y))
            .AddSubPlot(3, 3, 3, ax => ax.Plot(x, y))
            .AddSubPlot(3, 3, 4, ax => ax.Bar(["A", "B"], [10.0, 20]))
            .AddSubPlot(3, 3, 5, ax => ax.Bar(["C", "D"], [30.0, 40]))
            .AddSubPlot(3, 3, 6, ax => ax.Scatter(x.Take(10).ToArray(), y.Take(10).ToArray()))
            .AddSubPlot(3, 3, 7, ax => ax.Plot(x, y))
            .AddSubPlot(3, 3, 8, ax => ax.Plot(x, y))
            .AddSubPlot(3, 3, 9, ax => ax.Plot(x, y))
            .Build();

        var tree = new TreeNode
        {
            Label = "Root",
            Children = [
                new TreeNode { Label = "A", Value = 40, Children = [
                    new TreeNode { Label = "A1", Value = 20 },
                    new TreeNode { Label = "A2", Value = 20 }
                ]},
                new TreeNode { Label = "B", Value = 30 },
                new TreeNode { Label = "C", Value = 20 },
                new TreeNode { Label = "D", Value = 10 }
            ]
        };

        _treemap = Plt.Create().Treemap(tree).Build();
        _sunburst = Plt.Create().Sunburst(tree).Build();

        _sankey = Plt.Create().Sankey(
            [new SankeyNode("A"), new SankeyNode("B"), new SankeyNode("C"), new SankeyNode("D")],
            [new SankeyLink(0, 2, 30), new SankeyLink(0, 3, 20), new SankeyLink(1, 2, 10), new SankeyLink(1, 3, 15)]
        ).Build();

        var r = Enumerable.Range(0, 50).Select(i => (double)i / 5).ToArray();
        var theta = r.Select(v => v * 0.5).ToArray();
        _polar = Plt.Create().PolarPlot(r, theta).Build();

        var sx = Enumerable.Range(0, 10).Select(i => (double)i).ToArray();
        var sy = Enumerable.Range(0, 10).Select(i => (double)i).ToArray();
        var sz = new double[10, 10];
        for (int i = 0; i < 10; i++)
            for (int j = 0; j < 10; j++)
                sz[i, j] = Math.Sin(i * 0.5) * Math.Cos(j * 0.5);
        _surface3D = Plt.Create().Surface(sx, sy, sz).Build();

        _surface3DLit = Plt.Create()
            .Surface(sx, sy, sz)
            .WithLighting(0.5, 0.5, 1.0)
            .Build();

        // Minimal multi-feature GeoJSON: 4 polygons approximating world quadrants
        var geoJson = GeoJsonReader.FromJson("""
            {"type":"FeatureCollection","features":[
              {"type":"Feature","geometry":{"type":"Polygon","coordinates":[[[-180,0],[0,0],[0,90],[-180,90],[-180,0]]]},"properties":{}},
              {"type":"Feature","geometry":{"type":"Polygon","coordinates":[[[0,0],[180,0],[180,90],[0,90],[0,0]]]},"properties":{}},
              {"type":"Feature","geometry":{"type":"Polygon","coordinates":[[[-180,-90],[0,-90],[0,0],[-180,0],[-180,-90]]]},"properties":{}},
              {"type":"Feature","geometry":{"type":"Polygon","coordinates":[[[0,-90],[180,-90],[180,0],[0,0],[0,-90]]]},"properties":{}}
            ]}
            """);

        _geoMap = Plt.Create().Map(geoJson).Build();

        _choropleth = Plt.Create()
            .Choropleth(geoJson, [10.0, 40.0, 25.0, 70.0], c => c.ColorMap = ColorMaps.Viridis)
            .Build();

        _legendChart = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(x, y, s => s.Label = "Line 1")
                .Plot(x, y.Select(v => v * 0.8).ToArray(), s => s.Label = "Line 2")
                .Plot(x, y.Select(v => v * 0.6).ToArray(), s => s.Label = "Line 3")
                .WithLegend())
            .Build();

        var x10K = Enumerable.Range(0, 10_000).Select(i => (double)i).ToArray();
        var y10K = x10K.Select(v => Math.Sin(v * 0.01) * 50 + 50).ToArray();
        _largeLine10K = Plt.Create().WithTitle("Large Line 10K").Plot(x10K, y10K).Build();

        var x100K = Enumerable.Range(0, 100_000).Select(i => (double)i).ToArray();
        var y100K = x100K.Select(v => Math.Sin(v * 0.001) * 50 + 50).ToArray();
        _largeLine100K = Plt.Create().WithTitle("Large Line 100K")
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(x100K, y100K)
                .WithDownsampling(2000))
            .Build();
    }

    [Benchmark(Baseline = true)]
    public string SimpleLine() => _simpleLine.ToSvg();

    [Benchmark]
    public string ComplexChart() => _complexChart.ToSvg();

    [Benchmark]
    public string SubplotGrid3x3() => _subplotGrid.ToSvg();

    [Benchmark]
    public string Treemap() => _treemap.ToSvg();

    [Benchmark]
    public string Sunburst() => _sunburst.ToSvg();

    [Benchmark]
    public string Sankey() => _sankey.ToSvg();

    [Benchmark]
    public string PolarLine() => _polar.ToSvg();

    [Benchmark]
    public string Surface3D() => _surface3D.ToSvg();

    [Benchmark]
    public string Surface3D_WithLighting() => _surface3DLit.ToSvg();

    [Benchmark]
    public string GeoMap_Equirectangular() => _geoMap.ToSvg();

    [Benchmark]
    public string Choropleth_Viridis() => _choropleth.ToSvg();

    [Benchmark]
    public string WithLegend() => _legendChart.ToSvg();

    [Benchmark]
    public string LargeLineChart_10K() => _largeLine10K.ToSvg();

    [Benchmark]
    public string LargeLineChart_100K_LTTB() => _largeLine100K.ToSvg();
}
