// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Where the free ends of an axis stop, picked once from the theme's look (<see cref="AutolimitRule"/>): one leaf per
/// <c>axes.autolimit_mode</c> and one for the library's own nice bounds, so the 2-D range, the 3-D cube, the radius of a polar axes and
/// the tick rule ask the leaf and never the mode.</summary>
public class AutolimitRuleTests
{
    [Fact]
    public void TheRule_IsPickedOnceFromTheLook()
    {
        Assert.Same(AutolimitRule.RoundNumbers, Theme.MatplotlibClassic.Looks.Range.AutolimitRule);
        Assert.Same(AutolimitRule.Data, Theme.MatplotlibV2.Looks.Range.AutolimitRule);
        Assert.Same(AutolimitRule.Library, Theme.Default.Looks.Range.AutolimitRule);
    }

    [Fact]
    public void OnlyRoundNumbers_RoundsAnAxisOutToItsTicks()
    {
        Assert.True(AutolimitRule.RoundNumbers.RoundsToTicks);
        Assert.False(AutolimitRule.Data.RoundsToTicks);
        Assert.False(AutolimitRule.Library.RoundsToTicks);
    }

    /// <summary>A 3-D cube has no nice bounds of the library's: under a theme that names no mode it ends where the data and the margin
    /// end, as matplotlib's default does.</summary>
    [Fact]
    public void ACube_EndsAtTheData_WhereTheThemeNamesNoMode()
    {
        Assert.Same(AutolimitRule.Data, AutolimitRule.Library.InCube);
        Assert.Same(AutolimitRule.Data, AutolimitRule.Data.InCube);
        Assert.Same(AutolimitRule.RoundNumbers, AutolimitRule.RoundNumbers.InCube);
    }

    [Fact]
    public void OnlyAModeTheThemeNames_IsMatplotlibsRule()
    {
        Assert.True(AutolimitRule.Data.NamedByTheme);
        Assert.True(AutolimitRule.RoundNumbers.NamedByTheme);
        Assert.False(AutolimitRule.Library.NamedByTheme);
    }

    [Fact]
    public void EachLeaf_EndsTheRangeByItsOwnRule()
    {
        var auto = new AxisAutoscale(new Axis(), isX: true, Theme.Default);
        var range = new Range1D(0.13, 9.71);

        Assert.Equal(range, AutolimitRule.Data.Autolimited(auto, range, null, range, hasSticky: false, namesCategories: false));
        Assert.Equal(auto.RoundedOutToTicks(range, TickLadder.Classic),
            AutolimitRule.RoundNumbers.Autolimited(auto, range, null, range, hasSticky: false, namesCategories: false));
        Assert.Equal(range, AutolimitRule.RoundNumbers.Autolimited(auto, range, null, range, hasSticky: false, namesCategories: true));
        Assert.Equal(auto.ExpandedToNiceBounds(range, range, false, new AutoLocator()),
            AutolimitRule.Library.Autolimited(auto, range, null, range, hasSticky: false, namesCategories: false));
    }
}
