// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.Benchmarks;

/// <summary>What a streaming chart costs between two frames: a sample appended to each kind of streaming series, the question the
/// render clock asks at every tick (has any data changed), and the range a full stream is scaled to.</summary>
[MemoryDiagnoser]
public class StreamingTickBenchmarks
{
    private const int Samples = 1_000;

    private StreamingLineSeries _line = default!;
    private StreamingSignalSeries _signal = default!;
    private StreamingCandlestickSeries _candles = default!;
    private StreamingSignalSeries _fullSignal = default!;
    private StreamingLineSeries _fullLine = default!;
    private StreamingFigure _figure = default!;
    private Axes _axes = default!;

    [GlobalSetup]
    public void Setup()
    {
        _line = new StreamingLineSeries(Samples);
        _signal = new StreamingSignalSeries(Samples);
        _candles = new StreamingCandlestickSeries(Samples);

        _fullSignal = new StreamingSignalSeries(10_000);
        _fullLine = new StreamingLineSeries(10_000);
        for (int i = 0; i < 10_000; i++)
        {
            _fullSignal.AppendSample(Math.Sin(i * 0.01));
            _fullLine.AppendPoint(i, Math.Sin(i * 0.01));
        }

        // A dashboard of four panels, each with three streams and a static line on its primary Y axis: the seats every
        // version of the clock has counted.
        var figure = new Figure();
        for (int p = 0; p < 4; p++)
        {
            var axes = figure.AddSubPlot();
            axes.AddSeries(new StreamingLineSeries(Samples)).AppendPoint(0, 0);
            axes.AddSeries(new StreamingScatterSeries(Samples)).AppendPoint(0, 0);
            axes.AddSeries(new StreamingCandlestickSeries(Samples)).AppendBar(1, 2, 0, 1);
            axes.AddSeries(new LineSeries([0.0, 1.0], [0.0, 1.0]));
            _axes = axes;
        }

        _figure = new StreamingFigure(figure) { MinRenderInterval = TimeSpan.FromHours(1) };
    }

    [GlobalCleanup]
    public void Cleanup() => _figure.Dispose();

    [Benchmark]
    public long AppendPoint_1000()
    {
        for (int i = 0; i < Samples; i++) _line.AppendPoint(i, i);
        return _line.Version;
    }

    [Benchmark]
    public long AppendSample_1000()
    {
        for (int i = 0; i < Samples; i++) _signal.AppendSample(i);
        return _signal.Version;
    }

    [Benchmark]
    public long AppendBar_1000()
    {
        for (int i = 0; i < Samples; i++) _candles.AppendBar(i, i + 2, i - 1, i + 1);
        return _candles.Version;
    }

    /// <summary>The question the render clock asks at every tick.</summary>
    [Benchmark]
    public long DataVersion_4Panels() => _figure.DataVersion;

    [Benchmark]
    public double? SignalRange_10000() => _fullSignal.ComputeDataRange(new Rendering.AxesContextAdapter(_axes)).YMax;

    [Benchmark]
    public double? LineRange_10000() => _fullLine.ComputeDataRange(new Rendering.AxesContextAdapter(_axes)).YMax;
}
