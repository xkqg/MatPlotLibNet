// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>Verifies JSON serialization round-trip for <see cref="AxisBreak"/>.</summary>
public class BrokenAxisSerializationTests
{
    private static double[] X => [0, 50, 100, 150, 200];
    private static double[] Y => [0, 1, 2, 3, 4];

    [Fact]
    public void RoundTrip_XBreaks_PreservesFromTo()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(X, Y);
                ax.WithXBreak(60, 90);
            })
            .Build();
        var json = new ChartSerializer().ToJson(figure);
        var restored = new ChartSerializer().FromJson(json);
        var xBreaks = restored.SubPlots[0].XBreaks;
        Assert.Single(xBreaks);
        Assert.Equal(60.0, xBreaks[0].From, 1e-9);
        Assert.Equal(90.0, xBreaks[0].To, 1e-9);
    }

    [Fact]
    public void RoundTrip_YBreaks_PreservesFromTo()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(X, Y);
                ax.WithYBreak(1.5, 2.5);
            })
            .Build();
        var json = new ChartSerializer().ToJson(figure);
        var restored = new ChartSerializer().FromJson(json);
        var yBreaks = restored.SubPlots[0].YBreaks;
        Assert.Single(yBreaks);
        Assert.Equal(1.5, yBreaks[0].From, 1e-9);
        Assert.Equal(2.5, yBreaks[0].To, 1e-9);
    }

    [Fact]
    public void RoundTrip_BreakStyle_Straight()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(X, Y);
                ax.WithXBreak(60, 90, BreakStyle.Straight);
            })
            .Build();
        var json = new ChartSerializer().ToJson(figure);
        var restored = new ChartSerializer().FromJson(json);
        Assert.Equal(BreakStyle.Straight, restored.SubPlots[0].XBreaks[0].Style);
    }

    [Fact]
    public void NoBreaks_XBreaksProperty_AbsentFromJson()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y))
            .Build();
        var json = new ChartSerializer().ToJson(figure);
        Assert.DoesNotContain("\"xBreaks\"", json);
        Assert.DoesNotContain("\"yBreaks\"", json);
    }

    [Fact]
    public void MultipleXBreaks_AllRoundTrip()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(X, Y);
                ax.WithXBreak(20, 40).WithXBreak(80, 100);
            })
            .Build();
        var json = new ChartSerializer().ToJson(figure);
        var restored = new ChartSerializer().FromJson(json);
        Assert.Equal(2, restored.SubPlots[0].XBreaks.Count);
    }
}
