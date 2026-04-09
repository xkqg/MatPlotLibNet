// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Skia;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Benchmarks;

[MemoryDiagnoser]
public class SkiaExportBenchmarks
{
    private Figure _simpleFigure = default!;
    private Figure _complexFigure = default!;

    [GlobalSetup]
    public void Setup()
    {
        var x = Enumerable.Range(0, 100).Select(i => (double)i).ToArray();
        var y = x.Select(v => Math.Sin(v * 0.1) * 50 + 50).ToArray();

        _simpleFigure = Plt.Create()
            .WithTitle("Simple")
            .Plot(x, y)
            .Build();

        _complexFigure = Plt.Create()
            .WithTitle("Complex")
            .WithTheme(Theme.Dark)
            .Plot(x, y, l => { l.Color = Colors.Blue; })
            .Scatter(x.Take(20).ToArray(), y.Take(20).ToArray())
            .Bar(["A", "B", "C"], [10.0, 25, 15])
            .Build();
    }

    [Benchmark]
    public byte[] ToPng_Simple() => _simpleFigure.ToPng();

    [Benchmark]
    public byte[] ToPng_Complex() => _complexFigure.ToPng();

    [Benchmark]
    public byte[] ToPdf_Simple() => _simpleFigure.ToPdf();

    [Benchmark]
    public byte[] ToPdf_Complex() => _complexFigure.ToPdf();
}
