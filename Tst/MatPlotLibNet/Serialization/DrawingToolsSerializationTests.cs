// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>Verifies JSON serialization round-trip for trendlines, horizontal levels,
/// and Fibonacci retracements.</summary>
public class DrawingToolsSerializationTests
{
    private static (Figure fig, ChartSerializer ser) Make(Action<Axes> configure)
    {
        var fig = new Figure();
        var axes = fig.AddSubPlot();
        configure(axes);
        return (fig, new ChartSerializer());
    }

    // ── Trendline ─────────────────────────────────────────────────────────────

    [Fact]
    public void Trendline_RoundTrip_PreservesEndpoints()
    {
        var (fig, ser) = Make(ax => ax.AddTrendline(1.0, 2.0, 3.0, 4.0));
        var restored = ser.FromJson(ser.ToJson(fig));
        var t = restored.SubPlots[0].Trendlines[0];
        Assert.Equal(1.0, t.X1, 1e-9);
        Assert.Equal(2.0, t.Y1, 1e-9);
        Assert.Equal(3.0, t.X2, 1e-9);
        Assert.Equal(4.0, t.Y2, 1e-9);
    }

    [Fact]
    public void Trendline_RoundTrip_PreservesColor()
    {
        var (fig, ser) = Make(ax =>
        {
            var t = ax.AddTrendline(0, 0, 5, 5);
            t.Color = Color.FromHex("#FF0000");
        });
        var restored = ser.FromJson(ser.ToJson(fig));
        Assert.Equal(Color.FromHex("#FF0000"), restored.SubPlots[0].Trendlines[0].Color);
    }

    [Fact]
    public void Trendline_RoundTrip_PreservesLabel()
    {
        var (fig, ser) = Make(ax =>
        {
            var t = ax.AddTrendline(0, 0, 5, 5);
            t.Label = "my trend";
        });
        var restored = ser.FromJson(ser.ToJson(fig));
        Assert.Equal("my trend", restored.SubPlots[0].Trendlines[0].Label);
    }

    [Fact]
    public void Trendline_RoundTrip_PreservesIsExtended()
    {
        var (fig, ser) = Make(ax =>
        {
            var t = ax.AddTrendline(0, 0, 5, 5);
            t.IsExtended = true;
        });
        var restored = ser.FromJson(ser.ToJson(fig));
        Assert.True(restored.SubPlots[0].Trendlines[0].IsExtended);
    }

    [Fact]
    public void Trendline_RoundTrip_MultipleLines()
    {
        var (fig, ser) = Make(ax =>
        {
            ax.AddTrendline(0, 0, 1, 1);
            ax.AddTrendline(2, 2, 3, 3);
        });
        var restored = ser.FromJson(ser.ToJson(fig));
        Assert.Equal(2, restored.SubPlots[0].Trendlines.Count);
    }

    // ── HorizontalLevel ───────────────────────────────────────────────────────

    [Fact]
    public void HorizontalLevel_RoundTrip_PreservesValue()
    {
        var (fig, ser) = Make(ax => ax.AddLevel(150.0));
        var restored = ser.FromJson(ser.ToJson(fig));
        Assert.Equal(150.0, restored.SubPlots[0].HorizontalLevels[0].Value, 1e-9);
    }

    [Fact]
    public void HorizontalLevel_RoundTrip_PreservesLabel()
    {
        var (fig, ser) = Make(ax =>
        {
            var l = ax.AddLevel(100.0);
            l.Label = "support";
        });
        var restored = ser.FromJson(ser.ToJson(fig));
        Assert.Equal("support", restored.SubPlots[0].HorizontalLevels[0].Label);
    }

    [Fact]
    public void HorizontalLevel_RoundTrip_MultipleLevels()
    {
        var (fig, ser) = Make(ax =>
        {
            ax.AddLevel(100.0);
            ax.AddLevel(200.0);
            ax.AddLevel(300.0);
        });
        var restored = ser.FromJson(ser.ToJson(fig));
        Assert.Equal(3, restored.SubPlots[0].HorizontalLevels.Count);
    }

    // ── FibonacciRetracement ──────────────────────────────────────────────────

    [Fact]
    public void FibonacciRetracement_RoundTrip_PreservesPrices()
    {
        var (fig, ser) = Make(ax => ax.AddFibonacci(200.0, 100.0));
        var restored = ser.FromJson(ser.ToJson(fig));
        var f = restored.SubPlots[0].FibonacciRetracements[0];
        Assert.Equal(200.0, f.PriceHigh, 1e-9);
        Assert.Equal(100.0, f.PriceLow, 1e-9);
    }

    [Fact]
    public void FibonacciRetracement_RoundTrip_LevelsRecomputed()
    {
        var (fig, ser) = Make(ax => ax.AddFibonacci(200.0, 100.0));
        var restored = ser.FromJson(ser.ToJson(fig));
        Assert.Equal(7, restored.SubPlots[0].FibonacciRetracements[0].Levels.Count);
    }

    [Fact]
    public void FibonacciRetracement_RoundTrip_PreservesShowLabels()
    {
        var (fig, ser) = Make(ax =>
        {
            var f = ax.AddFibonacci(200.0, 100.0);
            f.ShowLabels = false;
        });
        var restored = ser.FromJson(ser.ToJson(fig));
        Assert.False(restored.SubPlots[0].FibonacciRetracements[0].ShowLabels);
    }

    // ── Trendline non-default LineStyle (solid→dashed branch) ────────────────

    [Fact]
    public void Trendline_RoundTrip_NonDefaultLineStyle_Preserved()
    {
        var (fig, ser) = Make(ax =>
        {
            var t = ax.AddTrendline(0, 0, 5, 5);
            t.LineStyle = MatPlotLibNet.Styling.LineStyle.Dashed;
        });
        var restored = ser.FromJson(ser.ToJson(fig));
        Assert.Equal(MatPlotLibNet.Styling.LineStyle.Dashed, restored.SubPlots[0].Trendlines[0].LineStyle);
    }

    // ── HorizontalLevel non-default LineStyle (dashed→solid branch) ──────────

    [Fact]
    public void HorizontalLevel_RoundTrip_NonDefaultLineStyle_Preserved()
    {
        var (fig, ser) = Make(ax =>
        {
            var l = ax.AddLevel(100.0);
            l.LineStyle = MatPlotLibNet.Styling.LineStyle.Solid;
        });
        var restored = ser.FromJson(ser.ToJson(fig));
        Assert.Equal(MatPlotLibNet.Styling.LineStyle.Solid, restored.SubPlots[0].HorizontalLevels[0].LineStyle);
    }

    // ── No tools → omitted from JSON ─────────────────────────────────────────

    [Fact]
    public void NoTools_Serialized_OmitsCollections()
    {
        var fig = new Figure();
        fig.AddSubPlot();
        var json = new ChartSerializer().ToJson(fig);
        Assert.DoesNotContain("trendlines", json);
        Assert.DoesNotContain("horizontalLevels", json);
        Assert.DoesNotContain("fibonacciRetracements", json);
    }
}
