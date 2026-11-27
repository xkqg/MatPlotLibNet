// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="Legend"/> record properties and <see cref="AxesBuilder"/> overloads.</summary>
public class LegendTests
{
    [Fact]
    public void LegendPosition_HasAllExpectedValues()
    {
        var values = Enum.GetValues<LegendPosition>();
        Assert.Equal(15, values.Length);
        Assert.Contains(LegendPosition.Best, values);
        Assert.Contains(LegendPosition.UpperRight, values);
        Assert.Contains(LegendPosition.UpperLeft, values);
        Assert.Contains(LegendPosition.LowerRight, values);
        Assert.Contains(LegendPosition.LowerLeft, values);
        Assert.Contains(LegendPosition.Right, values);
        Assert.Contains(LegendPosition.CenterLeft, values);
        Assert.Contains(LegendPosition.CenterRight, values);
        Assert.Contains(LegendPosition.LowerCenter, values);
        Assert.Contains(LegendPosition.UpperCenter, values);
        Assert.Contains(LegendPosition.Center, values);
        Assert.Contains(LegendPosition.OutsideRight, values);
        Assert.Contains(LegendPosition.OutsideLeft, values);
        Assert.Contains(LegendPosition.OutsideTop, values);
        Assert.Contains(LegendPosition.OutsideBottom, values);
    }

    [Fact]
    public void AxesBuilder_WithLegend_FuncOverload()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.WithLegend(l => l with { NCols = 2, Title = "Test" }))
            .Build();
        var axes = figure.SubPlots[0];
        Assert.Equal(2, axes.Legend.NCols);
        Assert.Equal("Test", axes.Legend.Title);
    }

    [Fact]
    public void AxesBuilder_WithLegend_FuncOverload_PreservesExisting()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithLegend(LegendPosition.UpperLeft)
                .WithLegend(l => l with { NCols = 3 }))
            .Build();
        var axes = figure.SubPlots[0];
        Assert.Equal(LegendPosition.UpperLeft, axes.Legend.Position);
        Assert.Equal(3, axes.Legend.NCols);
    }

    /// <summary>Root-fix regression test: the positional WithLegend(position, visible) overload
    /// used to replace the whole Legend record (<c>_axes.Legend = new Legend {...}</c>), silently
    /// discarding EVERY other property set earlier in the chain (not just LegendValues). The fix
    /// must preserve ALL prior state via `with`-mutation, general across every Legend property.</summary>
    [Fact]
    public void AxesBuilder_WithLegend_PositionalOverload_PreservesAllPriorLegendState()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithLegend(l => l with { NCols = 3, Title = "Metrics", FontSize = 11.0 })
                .WithLegendValues()
                .WithLegend(LegendPosition.LowerLeft, visible: false))
            .Build();
        var legend = figure.SubPlots[0].Legend;

        // Updated by the positional call:
        Assert.Equal(LegendPosition.LowerLeft, legend.Position);
        Assert.False(legend.Visible);

        // Preserved from earlier in the chain:
        Assert.Equal(3, legend.NCols);
        Assert.Equal("Metrics", legend.Title);
        Assert.Equal(11.0, legend.FontSize);
        Assert.True(legend.LegendValues);
    }
}
