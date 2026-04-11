// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="Legend"/> record properties and <see cref="AxesBuilder"/> overloads.</summary>
public class LegendTests
{
    [Fact]
    public void Legend_Defaults_NCols1_FrameOn()
    {
        var legend = new Legend();
        Assert.Equal(1, legend.NCols);
        Assert.True(legend.FrameOn);
    }

    [Fact]
    public void Legend_NCols_CanBeSet()
    {
        var legend = new Legend { NCols = 3 };
        Assert.Equal(3, legend.NCols);
    }

    [Fact]
    public void Legend_FontSize_CanBeSet()
    {
        var legend = new Legend { FontSize = 11.0 };
        Assert.Equal(11.0, legend.FontSize);
    }

    [Fact]
    public void Legend_FontSize_DefaultsToNull()
    {
        var legend = new Legend();
        Assert.Null(legend.FontSize);
    }

    [Fact]
    public void Legend_Title_CanBeSet()
    {
        var legend = new Legend { Title = "My Legend" };
        Assert.Equal("My Legend", legend.Title);
    }

    [Fact]
    public void Legend_Title_DefaultsToNull()
    {
        var legend = new Legend();
        Assert.Null(legend.Title);
    }

    [Fact]
    public void Legend_TitleFontSize_DefaultsToNull()
    {
        var legend = new Legend();
        Assert.Null(legend.TitleFontSize);
    }

    [Fact]
    public void Legend_FancyBox_Shadow_Defaults()
    {
        var legend = new Legend();
        Assert.False(legend.FancyBox);
        Assert.False(legend.Shadow);
    }

    [Fact]
    public void Legend_FrameAlpha_DefaultsToPoint8()
    {
        var legend = new Legend();
        Assert.Equal(0.8, legend.FrameAlpha);
    }

    [Fact]
    public void Legend_MarkerScale_DefaultsTo1()
    {
        var legend = new Legend();
        Assert.Equal(1.0, legend.MarkerScale);
    }

    [Fact]
    public void Legend_LabelSpacing_DefaultsToHalf()
    {
        var legend = new Legend();
        Assert.Equal(0.5, legend.LabelSpacing);
    }

    [Fact]
    public void Legend_ColumnSpacing_DefaultsTo2()
    {
        var legend = new Legend();
        Assert.Equal(2.0, legend.ColumnSpacing);
    }

    [Fact]
    public void Legend_EdgeColor_FaceColor_Default()
    {
        var legend = new Legend();
        Assert.Null(legend.EdgeColor);
        Assert.Null(legend.FaceColor);
    }

    [Fact]
    public void Legend_EdgeColor_FaceColor_Override()
    {
        var legend = new Legend { EdgeColor = Colors.Red, FaceColor = Colors.White };
        Assert.Equal(Colors.Red, legend.EdgeColor);
        Assert.Equal(Colors.White, legend.FaceColor);
    }

    [Fact]
    public void LegendPosition_HasAllExpectedValues()
    {
        var values = Enum.GetValues<LegendPosition>();
        Assert.Equal(11, values.Length);
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
}
