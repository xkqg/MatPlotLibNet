// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Data;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series.Streaming;

public sealed class StreamingCandlestickSeriesTests
{
    /// <summary>End-to-end on the real surface: a reader never calls the visitor directly, they call
    /// <c>ToSvg()</c>. <see cref="ISeriesVisitor.Visit(StreamingCandlestickSeries, RenderArea)"/> had no
    /// override in <c>SvgSeriesRenderer</c> (the ONLY production implementor), so the series' data range
    /// still scaled the axes but nothing drew — the same silent gap <c>StreamingLineSeries</c>/
    /// <c>StreamingScatterSeries</c>/<c>StreamingSignalSeries</c> had before 2026-08-22.
    /// <para>Asserts on a deliberately unusual up-colour (<c>#123456</c>) rather than a bare SVG element
    /// name — the plot background, grid lines and axis spines already emit <c>&lt;rect&gt;</c>/<c>&lt;line&gt;</c>
    /// regardless of whether the series itself drew anything, so a bare "any primitive exists" assertion
    /// passes even on the unfixed code (confirmed: it did, before this version was written). Only the
    /// candle body/wick (<see cref="CandlestickSeriesRenderer"/>, which <c>Visit</c> must delegate to) ever
    /// writes this exact colour.</para></summary>
    [Fact]
    public void ToSvg_DrawsTheAppendedBars()
    {
        var distinctiveUpColor = new Color(0x12, 0x34, 0x56);

        var svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                var s = ax.StreamingCandlestick();
                s.Color = distinctiveUpColor; // both bars below close above open -> "up"
                s.AppendBar(100, 110, 95, 105);
                s.AppendBar(105, 118, 100, 112);
            })
            .ToSvg();

        Assert.Contains("123456", svg, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The sibling half of the test above: an explicit <see cref="StreamingCandlestickSeries.DownColor"/>,
    /// the branch no other test sets (every other construction leaves it at its default null, taking
    /// <c>SvgSeriesRenderer</c>'s own fallback, <c>Colors.Red</c>).</summary>
    [Fact]
    public void ToSvg_WithAnExplicitDownColor_DrawsADownBarInIt()
    {
        var distinctiveDownColor = new Color(0x65, 0x43, 0x21);

        var svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                var s = ax.StreamingCandlestick();
                s.DownColor = distinctiveDownColor;
                s.AppendBar(100, 105, 95, 90); // close below open -> "down"
            })
            .ToSvg();

        Assert.Contains("654321", svg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Construction_DefaultCapacity_Is5000()
    {
        var s = new StreamingCandlestickSeries();
        Assert.Equal(5_000, s.Capacity);
        Assert.Equal(0, s.Count);
    }

    [Fact]
    public void AppendBar_IncrementsCountAndVersion()
    {
        var s = new StreamingCandlestickSeries();
        s.AppendBar(100, 110, 95, 105);
        Assert.Equal(1, s.Count);
        Assert.Equal(1, s.Version);
    }

    [Fact]
    public void AppendBar_OhlcBarOverload()
    {
        var s = new StreamingCandlestickSeries();
        s.AppendBar(new OhlcBar(100, 110, 95, 105));
        Assert.Equal(1, s.Count);
    }

    [Fact]
    public void CreateOhlcSnapshot_ReturnsFourParallelArrays()
    {
        var s = new StreamingCandlestickSeries(100);
        s.AppendBar(100, 110, 95, 105);
        s.AppendBar(105, 115, 100, 110);
        var snap = s.CreateOhlcSnapshot();
        Assert.Equal([100.0, 105.0], snap.Open);
        Assert.Equal([110.0, 115.0], snap.High);
        Assert.Equal([95.0, 100.0], snap.Low);
        Assert.Equal([105.0, 110.0], snap.Close);
    }

    [Fact]
    public void CreateOhlcSnapshot_IsImmutable()
    {
        var s = new StreamingCandlestickSeries(100);
        s.AppendBar(100, 110, 95, 105);
        var snap = s.CreateOhlcSnapshot();
        s.AppendBar(200, 220, 190, 210);
        Assert.Single(snap.Open); // snapshot not affected
    }

    [Fact]
    public void ComputeDataRange_UsesLowMinAndHighMax()
    {
        var s = new StreamingCandlestickSeries();
        s.AppendBar(100, 120, 80, 110);
        s.AppendBar(105, 115, 90, 108);
        var range = s.ComputeDataRange(null!);
        Assert.Equal(80.0, range.YMin);
        Assert.Equal(120.0, range.YMax);
    }

    [Fact]
    public void ComputeDataRange_Empty_ReturnsNulls()
    {
        var s = new StreamingCandlestickSeries();
        Assert.Null(s.ComputeDataRange(null!).XMin);
    }

    [Fact]
    public void BarAppended_FiresOnAppend()
    {
        var s = new StreamingCandlestickSeries();
        OhlcBar? received = null;
        s.BarAppended += bar => received = bar;
        s.AppendBar(100, 110, 95, 105);
        Assert.NotNull(received);
        Assert.Equal(100.0, received.Value.Open);
        Assert.Equal(110.0, received.Value.High);
        Assert.Equal(95.0, received.Value.Low);
        Assert.Equal(105.0, received.Value.Close);
    }

    [Fact]
    public void Clear_ResetsAll()
    {
        var s = new StreamingCandlestickSeries();
        s.AppendBar(100, 110, 95, 105);
        s.Clear();
        Assert.Equal(0, s.Count);
    }

    /// <summary>A bar that comes with its volume tells every subscriber of <see cref="StreamingCandlestickSeries.OhlcvBarAppended"/>
    /// that volume, and tells the older <see cref="StreamingCandlestickSeries.BarAppended"/> the prices alone, as it always did.</summary>
    [Fact]
    public void AppendBar_WithAVolume_RaisesBothEvents_EachWithWhatItCarries()
    {
        var series = new StreamingCandlestickSeries();
        OhlcBar? prices = null;
        OhlcvBar? traded = null;
        series.BarAppended += bar => prices = bar;
        series.OhlcvBarAppended += bar => traded = bar;

        series.AppendBar(new OhlcvBar(10, 12, 9, 11, 500));

        Assert.Equal(new OhlcBar(10, 12, 9, 11), prices);
        Assert.Equal(new OhlcvBar(10, 12, 9, 11, 500), traded);
    }

    /// <summary>Every way in without a volume ends in the same place: the volume event, with NaN.</summary>
    [Fact]
    public void AppendBar_WithoutAVolume_RaisesTheVolumeEventWithNaN_ByEveryRoute()
    {
        var series = new StreamingCandlestickSeries();
        var volumes = new List<double>();
        series.OhlcvBarAppended += bar => volumes.Add(bar.Volume);

        series.AppendBar(1, 2, 0.5, 1.5);
        series.AppendBar(new OhlcBar(1, 2, 0.5, 1.5));

        Assert.Equal(2, volumes.Count);
        Assert.All(volumes, v => Assert.True(double.IsNaN(v)));
    }

    /// <summary>The volume is for the indicators attached while the bar arrives; the series keeps the prices.</summary>
    [Fact]
    public void AppendBar_WithAVolume_StoresThePricesOnly()
    {
        var series = new StreamingCandlestickSeries();

        series.AppendBar(new OhlcvBar(10, 12, 9, 11, 500));

        var snapshot = series.CreateOhlcSnapshot();
        Assert.Equal([10.0], snapshot.Open);
        Assert.Equal([12.0], snapshot.High);
        Assert.Equal([9.0], snapshot.Low);
        Assert.Equal([11.0], snapshot.Close);
        Assert.Equal(1, series.Count);
        Assert.Equal(1, series.Version);
    }
}
