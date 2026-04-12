// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="SpinesConfig"/> and <see cref="SpineConfig"/> behavior.</summary>
public class SpinesConfigTests
{
    [Fact]
    public void SpineConfig_Default_IsVisible()
    {
        var spine = new SpineConfig();
        Assert.True(spine.Visible);
    }

    [Fact]
    public void SpineConfig_Default_PositionIsEdge()
    {
        var spine = new SpineConfig();
        Assert.Equal(SpinePosition.Edge, spine.Position);
    }

    [Fact]
    public void SpineConfig_Default_LineWidthIsOne()
    {
        var spine = new SpineConfig();
        Assert.Equal(1.0, spine.LineWidth);
    }

    [Fact]
    public void SpineConfig_WithRecord_CreatesModifiedCopy()
    {
        var spine = new SpineConfig() with { Visible = false };
        Assert.False(spine.Visible);
    }

    [Fact]
    public void SpinesConfig_Default_AllVisible()
    {
        var spines = new SpinesConfig();
        Assert.True(spines.Top.Visible);
        Assert.True(spines.Bottom.Visible);
        Assert.True(spines.Left.Visible);
        Assert.True(spines.Right.Visible);
    }

    [Fact]
    public void SpinesConfig_HideTop_TopNotVisible()
    {
        var spines = new SpinesConfig() with { Top = new SpineConfig() with { Visible = false } };
        Assert.False(spines.Top.Visible);
        Assert.True(spines.Bottom.Visible);
    }

    [Fact]
    public void SpinesConfig_RecordEquality_Works()
    {
        var a = new SpinesConfig();
        var b = new SpinesConfig();
        Assert.Equal(a, b);
    }

    [Fact]
    public void Axes_Spines_DefaultAllVisible()
    {
        var axes = new Axes();
        Assert.True(axes.Spines.Top.Visible);
        Assert.True(axes.Spines.Bottom.Visible);
        Assert.True(axes.Spines.Left.Visible);
        Assert.True(axes.Spines.Right.Visible);
    }

    [Fact]
    public void SpineConfig_DataPosition_StoresValue()
    {
        var spine = new SpineConfig() with { Position = SpinePosition.Data, PositionValue = 0.0 };
        Assert.Equal(SpinePosition.Data, spine.Position);
        Assert.Equal(0.0, spine.PositionValue);
    }

    [Fact]
    public void SpineConfig_AxesPosition_StoresValue()
    {
        var spine = new SpineConfig() with { Position = SpinePosition.Axes, PositionValue = 0.5 };
        Assert.Equal(SpinePosition.Axes, spine.Position);
        Assert.Equal(0.5, spine.PositionValue);
    }

    // --- 2G: Spine Enrichment ---

    [Fact]
    public void SpineConfig_Color_DefaultsToNull()
    {
        var spine = new SpineConfig();
        Assert.Null(spine.Color);
    }

    [Fact]
    public void SpineConfig_LineStyle_DefaultsToSolid()
    {
        var spine = new SpineConfig();
        Assert.Equal(LineStyle.Solid, spine.LineStyle);
    }

    [Fact]
    public void SpineConfig_Color_CanBeSet()
    {
        var spine = new SpineConfig() with { Color = Colors.Red };
        Assert.Equal(Colors.Red, spine.Color);
    }

    [Fact]
    public void SpineConfig_LineStyle_CanBeSet()
    {
        var spine = new SpineConfig() with { LineStyle = LineStyle.Dashed };
        Assert.Equal(LineStyle.Dashed, spine.LineStyle);
    }

    [Fact]
    public void SpinesConfig_PerSpineColor_Independent()
    {
        var spines = new SpinesConfig() with
        {
            Top    = new SpineConfig() with { Color = Colors.Red },
            Bottom = new SpineConfig() with { Color = Colors.Blue }
        };
        Assert.Equal(Colors.Red, spines.Top.Color);
        Assert.Equal(Colors.Blue, spines.Bottom.Color);
        Assert.Null(spines.Left.Color);
        Assert.Null(spines.Right.Color);
    }
}
