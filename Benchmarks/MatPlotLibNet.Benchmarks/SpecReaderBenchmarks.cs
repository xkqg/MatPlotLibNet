// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using BenchmarkDotNet.Attributes;
using MatPlotLibNet.Mcp;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Benchmarks;

/// <summary>What the tool server pays to read one model-authored chart document into a figure: parse, normalise (colour names,
/// enum spelling, bounds), the strict pass, the semantic pass and the library's own reader. It is paid once for each request,
/// not for each draw, so the row that matters is the allocation: the document is a JSON tree and a string of the whole
/// document on every pass that needs one.</summary>
[MemoryDiagnoser]
public class SpecReaderBenchmarks
{
    private ChartSpecReader _reader = default!;
    private ChartSpec _small;
    private ChartSpec _twoSubPlots;
    private ChartSchemaDescription _schema = default!;

    [GlobalSetup]
    public void Setup()
    {
        _reader = new ChartSpecReader(new ChartTypeCatalog(), RenderLimits.Default);
        _small = new ChartSpec("""{"width":400,"height":300,"subPlots":[{"series":[{"type":"line","xData":[1,2,3],"yData":[2,4,3],"color":"red"}]}]}""");
        _twoSubPlots = new ChartSpec(TwoSubPlots(2000));
        _schema = new ChartSchemaDescription(new ChartTypeCatalog());
    }

    [Benchmark]
    public Figure Read_Small() => _reader.Read(_small);

    [Benchmark]
    public Figure Read_TwoSubPlots_2000() => _reader.Read(_twoSubPlots);

    /// <summary>What describe_chart_schema answers without a chart type: the whole spec shape.</summary>
    [Benchmark]
    public string Describe_Overview() => _schema.Describe(null);

    private static string TwoSubPlots(int points)
    {
        var x = new StringBuilder();
        var y = new StringBuilder();
        for (int i = 0; i < points; i++)
        {
            string separator = i == 0 ? "" : ",";
            x.Append(separator).Append(i.ToString(CultureInfo.InvariantCulture));
            y.Append(separator).Append(((i * 37) % 101).ToString(CultureInfo.InvariantCulture));
        }

        string line = $$"""{"type":"line","xData":[{{x}}],"yData":[{{y}}],"color":"CornflowerBlue","label":"signal"}""";
        string bars = """{"type":"bar","categories":["Q1","Q2","Q3","Q4"],"values":[100,250,180,320],"color":"#f80"}""";
        return $$"""{"width":800,"height":600,"title":"Spec","backgroundColor":"white","subPlots":[{"title":"A","series":[{{line}}]},{"title":"B","series":[{{bars}}]}]}""";
    }
}
