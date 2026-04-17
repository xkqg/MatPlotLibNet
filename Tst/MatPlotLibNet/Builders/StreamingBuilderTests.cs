// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.Tests.Builders;

public sealed class StreamingBuilderTests
{
    [Fact]
    public void StreamingPlot_ReturnsStreamingLineSeries()
    {
        StreamingLineSeries? series = null;
        var sf = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => { series = ax.StreamingPlot(); })
            .BuildStreaming();
        Assert.NotNull(series);
        Assert.Equal(10_000, series!.Capacity);
        sf.Dispose();
    }

    [Fact]
    public void StreamingPlot_CustomCapacity()
    {
        StreamingLineSeries? series = null;
        var sf = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => { series = ax.StreamingPlot(500); })
            .BuildStreaming();
        Assert.Equal(500, series!.Capacity);
        sf.Dispose();
    }

    [Fact]
    public void StreamingScatter_ReturnsStreamingScatterSeries()
    {
        StreamingScatterSeries? series = null;
        var sf = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => { series = ax.StreamingScatter(); })
            .BuildStreaming();
        Assert.NotNull(series);
        sf.Dispose();
    }

    [Fact]
    public void StreamingSignal_ReturnsStreamingSignalSeries()
    {
        StreamingSignalSeries? series = null;
        var sf = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => { series = ax.StreamingSignal(sampleRate: 44100); })
            .BuildStreaming();
        Assert.NotNull(series);
        Assert.Equal(44100.0, series!.SampleRate);
        sf.Dispose();
    }

    [Fact]
    public void StreamingCandlestick_ReturnsStreamingCandlestickSeries()
    {
        StreamingCandlestickSeries? series = null;
        var sf = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => { series = ax.StreamingCandlestick(1000); })
            .BuildStreaming();
        Assert.NotNull(series);
        Assert.Equal(1000, series!.Capacity);
        sf.Dispose();
    }

    [Fact]
    public void BuildStreaming_ReturnsStreamingFigure()
    {
        var sf = Plt.Create().WithTitle("Live").BuildStreaming();
        Assert.NotNull(sf);
        Assert.Equal("Live", sf.Figure.Title);
        sf.Dispose();
    }

    [Fact]
    public void BuildStreaming_CustomInterval()
    {
        var sf = Plt.Create().BuildStreaming(TimeSpan.FromMilliseconds(100));
        Assert.Equal(TimeSpan.FromMilliseconds(100), sf.MinRenderInterval);
        sf.Dispose();
    }

    [Fact]
    public void StreamingPlot_ConfigureCallback_SetsProperties()
    {
        StreamingLineSeries? series = null;
        var sf = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                series = ax.StreamingPlot(configure: s => { s.Label = "Live"; s.LineWidth = 2.0; });
            })
            .BuildStreaming();
        Assert.Equal("Live", series!.Label);
        Assert.Equal(2.0, series.LineWidth);
        sf.Dispose();
    }
}
