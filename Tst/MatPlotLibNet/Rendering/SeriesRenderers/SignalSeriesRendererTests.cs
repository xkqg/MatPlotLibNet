// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Verifies that SignalXYSeries and SignalSeries render to SVG and round-trip through JSON.</summary>
public class SignalSeriesRendererTests
{
    // ── SignalXYSeries SVG ────────────────────────────────────────────────────

    [Fact]
    public void SignalXY_Render_ProducesSvgPolyline()
    {
        var x = Enumerable.Range(0, 10_000).Select(i => (double)i).ToArray();
        var y = x.Select(v => Math.Sin(v * 0.001)).ToArray();
        var figure = Plt.Create().SignalXY(x, y).Build();
        var svg = figure.ToSvg();
        Assert.Contains("<polyline", svg);
    }

    [Fact]
    public void SignalXY_NarrowViewport_StillProducesPolyline()
    {
        var x = Enumerable.Range(0, 10_000).Select(i => (double)i).ToArray();
        var y = x.Select(v => Math.Sin(v * 0.001)).ToArray();
        var figure = Plt.Create().SignalXY(x, y, s =>
        {
            s.MaxDisplayPoints = 500;
        }).Build();
        var svg = figure.ToSvg();
        Assert.Contains("<polyline", svg);
    }

    // ── SignalXYSeries JSON round-trip ────────────────────────────────────────

    [Fact]
    public void SignalXY_JsonRoundTrip_RestoresType()
    {
        var x = new double[] { 1.0, 2.0, 3.0 };
        var y = new double[] { 10.0, 20.0, 30.0 };
        var original = Plt.Create().SignalXY(x, y).Build();
        var json = original.ToJson();
        var restored = new ChartSerializer().FromJson(json);
        Assert.Single(restored.SubPlots[0].Series);
        Assert.IsType<SignalXYSeries>(restored.SubPlots[0].Series[0]);
    }

    [Fact]
    public void SignalXY_JsonRoundTrip_RestoresXYData()
    {
        var x = new double[] { 1.0, 2.0, 3.0 };
        var y = new double[] { 10.0, 20.0, 30.0 };
        var original = Plt.Create().SignalXY(x, y).Build();
        var restored = new ChartSerializer().FromJson(original.ToJson());
        var s = (SignalXYSeries)restored.SubPlots[0].Series[0];
        Assert.Equal(x, s.XData);
        Assert.Equal(y, s.YData);
    }

    // ── SignalSeries SVG (added in 1d, stub for now) ──────────────────────────

    [Fact]
    public void Signal_Render_ProducesSvgPolyline()
    {
        var y = Enumerable.Range(0, 10_000).Select(i => Math.Sin(i * 0.001)).ToArray();
        var figure = Plt.Create().Signal(y, sampleRate: 1000.0).Build();
        var svg = figure.ToSvg();
        Assert.Contains("<polyline", svg);
    }

    [Fact]
    public void Signal_JsonRoundTrip_RestoresSampleRateAndXStart()
    {
        var y = new double[] { 1.0, 2.0, 3.0 };
        var original = Plt.Create().Signal(y, sampleRate: 100.0, xStart: 5.0).Build();
        var restored = new ChartSerializer().FromJson(original.ToJson());
        var s = (SignalSeries)restored.SubPlots[0].Series[0];
        Assert.Equal(100.0, s.SampleRate);
        Assert.Equal(5.0, s.XStart);
        Assert.Equal(y, s.YData);
    }
}
