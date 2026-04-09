// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Benchmarks;

[MemoryDiagnoser]
public class SerializationBenchmarks
{
    private readonly IChartSerializer _serializer = new ChartSerializer();
    private Figure _figure = default!;
    private string _json = default!;

    [GlobalSetup]
    public void Setup()
    {
        var x = Enumerable.Range(0, 100).Select(i => (double)i).ToArray();
        var y = x.Select(v => Math.Sin(v * 0.1) * 50 + 50).ToArray();

        _figure = Plt.Create()
            .WithTitle("Serialization Test")
            .WithTheme(Theme.Dark)
            .Plot(x, y, l => { l.Color = Colors.Blue; l.Label = "Data"; })
            .Bar(["Q1", "Q2", "Q3", "Q4"], [100.0, 250, 180, 320])
            .Build();

        _json = _serializer.ToJson(_figure);
    }

    [Benchmark]
    public string ToJson() => _serializer.ToJson(_figure);

    [Benchmark]
    public string ToJsonIndented() => _serializer.ToJson(_figure, indented: true);

    [Benchmark]
    public Figure FromJson() => _serializer.FromJson(_json);

    [Benchmark]
    public Figure RoundTrip()
    {
        var json = _serializer.ToJson(_figure);
        return _serializer.FromJson(json);
    }
}
