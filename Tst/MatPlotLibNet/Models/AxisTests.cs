// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Models;

public class AxisTests
{
    [Fact]
    public void DefaultAxis_HasNullLabel()
    {
        var axis = new Axis();
        Assert.Null(axis.Label);
    }

    [Fact]
    public void DefaultAxis_HasNullLimits()
    {
        var axis = new Axis();
        Assert.Null(axis.Min);
        Assert.Null(axis.Max);
    }

    [Fact]
    public void DefaultAxis_IsLinear()
    {
        var axis = new Axis();
        Assert.Equal(AxisScale.Linear, axis.Scale);
    }

    [Fact]
    public void DefaultAxis_IsNotInverted()
    {
        var axis = new Axis();
        Assert.False(axis.Inverted);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var axis = new Axis
        {
            Label = "X Axis",
            Min = -10,
            Max = 10,
            Scale = AxisScale.Log,
            Inverted = true
        };

        Assert.Equal("X Axis", axis.Label);
        Assert.Equal(-10, axis.Min);
        Assert.Equal(10, axis.Max);
        Assert.Equal(AxisScale.Log, axis.Scale);
        Assert.True(axis.Inverted);
    }
}
