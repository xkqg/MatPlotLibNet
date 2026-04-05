// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.FluentApi;

public class IntuitiveIndicatorTests
{
    private static readonly double[] Close = [10, 12, 11, 14, 13, 15, 16, 14, 17, 18];

    [Fact]
    public void Sma_DirectOnBuilder_AddsLineSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(Enumerable.Range(0, 10).Select(i => (double)i).ToArray(), Close)
                .Sma(3))
            .Build();
        // Original plot + SMA = 2 series
        Assert.Equal(2, figure.SubPlots[0].Series.Count);
    }

    [Fact]
    public void Ema_DirectOnBuilder_AddsLineSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(Enumerable.Range(0, 10).Select(i => (double)i).ToArray(), Close)
                .Ema(5))
            .Build();
        Assert.Equal(2, figure.SubPlots[0].Series.Count);
    }

    [Fact]
    public void BollingerBands_DirectOnBuilder()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(Enumerable.Range(0, 10).Select(i => (double)i).ToArray(), Close)
                .BollingerBands(5))
            .Build();
        // Original + AreaSeries (bands) + LineSeries (middle) = 3
        Assert.Equal(3, figure.SubPlots[0].Series.Count);
    }

    [Fact]
    public void Rsi_DirectOnBuilder()
    {
        double[] prices = [44, 44.34, 44.09, 43.61, 44.33, 44.83, 45.10, 45.42, 45.84,
            46.08, 45.89, 46.03, 45.61, 46.28, 46.28, 46.00, 46.03, 46.41, 46.22, 45.64];
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Rsi(prices, 14))
            .Build();
        Assert.Single(figure.SubPlots[0].Series);
    }
}

public class IntuitiveSignalTests
{
    [Fact]
    public void BuyAt_AddsSignalMarker()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([0, 1, 2, 3, 4.0], [10, 20, 15, 25, 18.0])
                .BuyAt(1, 20))
            .Build();
        Assert.Single(figure.SubPlots[0].Signals);
        Assert.Equal(SignalDirection.Buy, figure.SubPlots[0].Signals[0].Direction);
    }

    [Fact]
    public void SellAt_AddsSignalMarker()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([0, 1, 2, 3, 4.0], [10, 20, 15, 25, 18.0])
                .SellAt(3, 25))
            .Build();
        Assert.Single(figure.SubPlots[0].Signals);
        Assert.Equal(SignalDirection.Sell, figure.SubPlots[0].Signals[0].Direction);
    }
}

public class IntuitiveExportTests
{
    [Fact]
    public void SaveSvg_CreatesFile()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.svg");
        try
        {
            figure.SaveSvg(path);
            Assert.True(File.Exists(path));
            Assert.StartsWith("<svg", File.ReadAllText(path).TrimStart());
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
