// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Tools;

/// <summary>Verifies <see cref="HorizontalLevel"/> behavior.</summary>
public class HorizontalLevelTests
{
    [Fact]
    public void HorizontalLevel_StoresValue()
    {
        var level = new HorizontalLevel(42.5);
        Assert.Equal(42.5, level.Value);
    }

    [Fact]
    public void DefaultLineStyle_IsDashed()
    {
        var level = new HorizontalLevel(0.0);
        Assert.Equal(LineStyle.Dashed, level.LineStyle);
    }

    [Fact]
    public void DefaultLineWidth_Is1()
    {
        var level = new HorizontalLevel(0.0);
        Assert.Equal(1.0, level.LineWidth);
    }

    [Fact]
    public void DefaultColor_IsNull()
    {
        var level = new HorizontalLevel(0.0);
        Assert.Null(level.Color);
    }

    [Fact]
    public void DefaultLabel_IsNull()
    {
        var level = new HorizontalLevel(0.0);
        Assert.Null(level.Label);
    }

    [Fact]
    public void Properties_AreMutable()
    {
        var level = new HorizontalLevel(100.0);
        level.Color = Color.FromHex("#FF0000");
        level.LineStyle = LineStyle.Dotted;
        level.LineWidth = 2.5;
        level.Label = "support";

        Assert.Equal(Color.FromHex("#FF0000"), level.Color);
        Assert.Equal(LineStyle.Dotted, level.LineStyle);
        Assert.Equal(2.5, level.LineWidth);
        Assert.Equal("support", level.Label);
    }

    [Fact]
    public void Axes_AddLevel_AppendsToCollection()
    {
        var axes = new Axes();
        var level = axes.AddLevel(150.0);

        Assert.Single(axes.HorizontalLevels);
        Assert.Same(level, axes.HorizontalLevels[0]);
    }

    [Fact]
    public void Axes_AddLevel_StoresValue()
    {
        var axes = new Axes();
        var level = axes.AddLevel(250.0);
        Assert.Equal(250.0, level.Value);
    }

    [Fact]
    public void Axes_AddLevel_MultipleLevelsAllStored()
    {
        var axes = new Axes();
        axes.AddLevel(100.0);
        axes.AddLevel(200.0);
        axes.AddLevel(300.0);

        Assert.Equal(3, axes.HorizontalLevels.Count);
    }
}
