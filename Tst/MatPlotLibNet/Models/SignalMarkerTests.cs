// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="SignalMarker"/> behavior.</summary>
public class SignalMarkerTests
{
    /// <summary>Verifies that a signal marker stores its X, Y, and direction.</summary>
    [Fact]
    public void SignalMarker_StoresPositionAndDirection()
    {
        var marker = new SignalMarker(5.0, 100.0, SignalDirection.Buy);
        Assert.Equal(5.0, marker.X);
        Assert.Equal(100.0, marker.Y);
        Assert.Equal(SignalDirection.Buy, marker.Direction);
    }

    /// <summary>Verifies that the default color is null.</summary>
    [Fact]
    public void DefaultColor_IsNull()
    {
        var marker = new SignalMarker(0, 0, SignalDirection.Sell);
        Assert.Null(marker.Color);
    }

    /// <summary>Verifies that the default marker size is 12.</summary>
    [Fact]
    public void DefaultSize_Is12()
    {
        var marker = new SignalMarker(0, 0, SignalDirection.Buy);
        Assert.Equal(12, marker.Size);
    }

    /// <summary>Verifies that the Signals collection defaults to empty.</summary>
    [Fact]
    public void Axes_Signals_DefaultsToEmpty()
    {
        var axes = new Axes();
        Assert.Empty(axes.Signals);
    }

    /// <summary>Verifies that AddSignal adds a signal marker to the collection.</summary>
    [Fact]
    public void Axes_AddSignal_AppearsInCollection()
    {
        var axes = new Axes();
        axes.AddSignal(5.0, 100.0, SignalDirection.Buy);
        Assert.Single(axes.Signals);
        Assert.Equal(SignalDirection.Buy, axes.Signals[0].Direction);
    }

    /// <summary>Verifies that AddSignal supports fluent chaining via the builder API.</summary>
    [Fact]
    public void AxesBuilder_AddSignal_FluentChaining()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0, 3.0], [10, 20, 15])
                .AddSignal(1.0, 10.0, SignalDirection.Buy)
                .AddSignal(2.0, 20.0, SignalDirection.Sell))
            .Build();
        Assert.Equal(2, figure.SubPlots[0].Signals.Count);
    }

    /// <summary>Verifies that SVG output contains a polygon element for a signal marker.</summary>
    [Fact]
    public void SvgOutput_ContainsPolygonForSignal()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Plot([1.0, 2.0, 3.0], [10, 20, 15]);
        axes.AddSignal(1.0, 10.0, SignalDirection.Buy);

        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.Contains("<polygon", svg);
    }
}

/// <summary>Verifies <see cref="BuySellSignal"/> behavior.</summary>
public class BuySellSignalIndicatorTests
{
    /// <summary>Verifies that Apply adds buy and sell signals to the axes.</summary>
    [Fact]
    public void Apply_AddsSignalsToAxes()
    {
        var axes = new Axes();
        axes.Plot([0, 1, 2, 3, 4], [10, 20, 15, 25, 18]);

        var indicator = new BuySellSignal(
            buyIndices: [0, 2],
            buyPrices: [10, 15],
            sellIndices: [1, 3],
            sellPrices: [20, 25]);
        indicator.Apply(axes);

        Assert.Equal(4, axes.Signals.Count);
        Assert.Equal(2, axes.Signals.Count(s => s.Direction == SignalDirection.Buy));
        Assert.Equal(2, axes.Signals.Count(s => s.Direction == SignalDirection.Sell));
    }
}
