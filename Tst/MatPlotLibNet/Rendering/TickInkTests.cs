// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What a drawing of ticks puts on the page is asked of its <see cref="TickInk"/> (<see cref="TickInkExtensions"/>), the one
/// reader of its flags.</summary>
public class TickInkTests
{
    [Theory]
    [InlineData((int)TickInk.Labels, true, false)]
    [InlineData((int)TickInk.Marks, false, true)]
    [InlineData((int)TickInk.Both, true, true)]
    public void AnInk_SaysWhetherItDrawsTheLabelsAndTheMarks(int value, bool labels, bool marks)
    {
        var ink = (TickInk)value;

        Assert.Equal(labels, ink.DrawsLabels);
        Assert.Equal(marks, ink.DrawsMarks);
    }

    [Theory]
    [InlineData((int)TickInk.Both, (int)TickInk.Marks)]
    [InlineData((int)TickInk.Marks, (int)TickInk.Marks)]
    [InlineData((int)TickInk.Labels, 0)]
    public void AnInkWithoutItsLabels_KeepsItsMarksAlone(int value, int without)
    {
        Assert.Equal((TickInk)without, ((TickInk)value).WithoutLabels);
    }

    /// <summary>An axis whose labels stand on the other side draws the marks alone on its own side; one that labels its own side draws
    /// all of the ink.</summary>
    [Fact]
    public void AnAxisOwnSide_DrawsTheInkWithoutTheLabels_WhenItsLabelsStandOnTheOtherSide()
    {
        Assert.Equal(TickInk.Both, new TickConfig().OnOwnSide(TickInk.Both));
        Assert.Equal(TickInk.Marks, new TickConfig { LabelsOnOwnSide = false }.OnOwnSide(TickInk.Both));
    }
}
