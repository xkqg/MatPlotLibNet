// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Benchmarks;

[MemoryDiagnoser]
public class SvgRenderingBenchmarks
{
    private Figure _simpleLine = default!;
    private Figure _complexChart = default!;
    private Figure _subplotGrid = default!;

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
            .Plot(x, y, l => { l.Color = Color.Blue; l.Label = "sin"; })
            .Scatter(x.Take(20).ToArray(), y.Take(20).ToArray(), s => { s.Color = Color.Red; })
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
    }

    [Benchmark(Baseline = true)]
    public string SimpleLine() => _simpleLine.ToSvg();

    [Benchmark]
    public string ComplexChart() => _complexChart.ToSvg();

    [Benchmark]
    public string SubplotGrid3x3() => _subplotGrid.ToSvg();
}
