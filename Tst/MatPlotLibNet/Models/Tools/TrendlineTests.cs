// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Tools;

/// <summary>Verifies <see cref="Trendline"/> behavior.</summary>
public class TrendlineTests
{
    [Fact]
    public void Trendline_StoresEndpoints()
    {
        var line = new Trendline(1.0, 2.0, 3.0, 4.0);
        Assert.Equal(1.0, line.X1);
        Assert.Equal(2.0, line.Y1);
        Assert.Equal(3.0, line.X2);
        Assert.Equal(4.0, line.Y2);
    }

    [Fact]
    public void DefaultLineStyle_IsSolid()
    {
        var line = new Trendline(0, 0, 1, 1);
        Assert.Equal(LineStyle.Solid, line.LineStyle);
    }

    [Fact]
    public void DefaultLineWidth_Is1()
    {
        var line = new Trendline(0, 0, 1, 1);
        Assert.Equal(1.0, line.LineWidth);
    }

    [Fact]
    public void DefaultColor_IsNull()
    {
        var line = new Trendline(0, 0, 1, 1);
        Assert.Null(line.Color);
    }

    [Fact]
    public void DefaultLabel_IsNull()
    {
        var line = new Trendline(0, 0, 1, 1);
        Assert.Null(line.Label);
    }

    [Fact]
    public void DefaultIsExtended_IsFalse()
    {
        var line = new Trendline(0, 0, 1, 1);
        Assert.False(line.IsExtended);
    }

    [Fact]
    public void Properties_AreMutable()
    {
        var line = new Trendline(0, 0, 1, 1);
        line.Color = Color.FromHex("#0000FF");
        line.LineStyle = LineStyle.Dashed;
        line.LineWidth = 2.0;
        line.Label = "trend";
        line.IsExtended = true;

        Assert.Equal(Color.FromHex("#0000FF"), line.Color);
        Assert.Equal(LineStyle.Dashed, line.LineStyle);
        Assert.Equal(2.0, line.LineWidth);
        Assert.Equal("trend", line.Label);
        Assert.True(line.IsExtended);
    }

    [Fact]
    public void Axes_AddTrendline_AppendsToCollection()
    {
        var axes = new Axes();
        var line = axes.AddTrendline(0.0, 10.0, 5.0, 20.0);

        Assert.Single(axes.Trendlines);
        Assert.Same(line, axes.Trendlines[0]);
    }

    [Fact]
    public void Axes_AddTrendline_StoresCoordinates()
    {
        var axes = new Axes();
        var line = axes.AddTrendline(1.0, 2.0, 3.0, 4.0);

        Assert.Equal(1.0, line.X1);
        Assert.Equal(2.0, line.Y1);
        Assert.Equal(3.0, line.X2);
        Assert.Equal(4.0, line.Y2);
    }

    [Fact]
    public void Axes_AddTrendline_MultipleLinesAllStored()
    {
        var axes = new Axes();
        axes.AddTrendline(0, 0, 1, 1);
        axes.AddTrendline(2, 2, 3, 3);

        Assert.Equal(2, axes.Trendlines.Count);
    }
}
