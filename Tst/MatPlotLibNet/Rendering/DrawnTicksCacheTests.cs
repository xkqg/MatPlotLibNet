// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The ticks of an axis as a theme draws them are worked out once for a pair of the axis' ticks and a theme, not once for each
/// time a renderer asks: a theme that names a look for the ticks (the matplotlib themes) gives a copy of the held ticks, and a draw
/// asks for that copy several times for every axis, so a draw that allocated one each time spent an allocation a draw on a value
/// that cannot change between two draws of the same figure.</summary>
public class DrawnTicksCacheTests
{
    private static Axis WithMinorTicksOn() => new() { MinorTicks = new Axis().MinorTicks with { Visible = true } };

    [Fact]
    public void MinorTicksUnderV2_AreTheSameInstanceOnEveryAsk()
    {
        var axis = WithMinorTicksOn();

        Assert.Same(axis.MinorTicksUnder(Theme.MatplotlibV2, ScaleRule.Linear), axis.MinorTicksUnder(Theme.MatplotlibV2, ScaleRule.Linear));
    }

    [Fact]
    public void MajorTicksUnderClassic_AreTheSameInstanceOnEveryAsk()
    {
        var axis = new Axis();

        Assert.Same(axis.MajorTicksUnder(Theme.MatplotlibClassic), axis.MajorTicksUnder(Theme.MatplotlibClassic));
    }

    [Fact]
    public void TheCachedTicks_AreWhatTheThemeDrawsThemAs()
    {
        var axis = WithMinorTicksOn();

        Assert.Equal(axis.MinorTicks.DrawnUnder(Theme.MatplotlibV2, major: false), axis.MinorTicksUnder(Theme.MatplotlibV2, ScaleRule.Linear));
        Assert.Equal(axis.MajorTicks.DrawnUnder(Theme.MatplotlibClassic, major: true), axis.MajorTicksUnder(Theme.MatplotlibClassic));
    }

    [Fact]
    public void AnotherTheme_GetsItsOwnDrawnTicks()
    {
        var axis = WithMinorTicksOn();
        var v2 = axis.MinorTicksUnder(Theme.MatplotlibV2, ScaleRule.Linear);

        var classic = axis.MinorTicksUnder(Theme.MatplotlibClassic, ScaleRule.Linear);

        Assert.NotSame(v2, classic);
        Assert.Equal(0.5.PointsInPixels, classic.Width, 9);
        Assert.Equal(0.6.PointsInPixels, axis.MinorTicksUnder(Theme.MatplotlibV2, ScaleRule.Linear).Width, 9);
    }

    [Fact]
    public void ChangedTicks_AreDrawnAsChanged()
    {
        var axis = WithMinorTicksOn();
        _ = axis.MinorTicksUnder(Theme.MatplotlibV2, ScaleRule.Linear);
        _ = axis.MajorTicksUnder(Theme.MatplotlibClassic);

        axis.MinorTicks = axis.MinorTicks with { Width = 3 };
        axis.MajorTicks = axis.MajorTicks with { Pad = 9 };

        Assert.Equal(3, axis.MinorTicksUnder(Theme.MatplotlibV2, ScaleRule.Linear).Width, 9);
        Assert.Equal(9, axis.MajorTicksUnder(Theme.MatplotlibClassic).Pad, 9);
    }

    [Fact]
    public void MinorTicksThatAreOff_AreTheHeldTicks_AndTurnedOnAreDrawn()
    {
        var axis = new Axis();
        Assert.Same(axis.MinorTicks, axis.MinorTicksUnder(Theme.MatplotlibV2, ScaleRule.Linear));

        axis.MinorTicks = axis.MinorTicks with { Visible = true };

        Assert.True(axis.MinorTicksUnder(Theme.MatplotlibV2, ScaleRule.Linear).Visible);
        Assert.Equal(3.4.PointsInPixels, axis.MinorTicksUnder(Theme.MatplotlibV2, ScaleRule.Linear).Pad, 9);
    }

    [Fact]
    public void TheLibrarysOwnThemes_DrawTheHeldTicks()
    {
        var axis = WithMinorTicksOn();

        Assert.Same(axis.MajorTicks, axis.MajorTicksUnder(Theme.Default));
        Assert.Same(axis.MinorTicks, axis.MinorTicksUnder(Theme.Default, ScaleRule.Linear));
    }

    /// <summary>A log axis' minor ticks stand under v2 with no call: they are drawn shown, and the shown copy is kept like any other,
    /// one allocation for the axis and the theme, not one a draw.</summary>
    [Fact]
    public void TheMinorTicksOfALogAxisUnderV2_AreShown_AndTheSameInstanceOnEveryAsk()
    {
        var axis = new Axis();

        var drawn = axis.MinorTicksUnder(Theme.MatplotlibV2, ScaleRule.Log);

        Assert.True(drawn.Visible);
        Assert.False(axis.MinorTicks.Visible);
        Assert.Same(drawn, axis.MinorTicksUnder(Theme.MatplotlibV2, ScaleRule.Log));
        Assert.Same(axis.MinorTicks, axis.MinorTicksUnder(Theme.Default, ScaleRule.Log));
    }
}
