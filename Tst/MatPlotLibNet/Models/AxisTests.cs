// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="Axis"/> behavior.</summary>
public class AxisTests
{
    /// <summary>Verifies that a default axis has a null label.</summary>
    [Fact]
    public void DefaultAxis_HasNullLabel()
    {
        var axis = new Axis();
        Assert.Null(axis.Label);
    }

    /// <summary>Verifies that a default axis has null Min and Max limits.</summary>
    [Fact]
    public void DefaultAxis_HasNullLimits()
    {
        var axis = new Axis();
        Assert.Null(axis.Min);
        Assert.Null(axis.Max);
    }

    /// <summary>Verifies that a default axis uses the Linear scale.</summary>
    [Fact]
    public void DefaultAxis_IsLinear()
    {
        var axis = new Axis();
        Assert.Equal(AxisScale.Linear, axis.Scale);
    }

    /// <summary>Verifies that a default axis is not inverted.</summary>
    [Fact]
    public void DefaultAxis_IsNotInverted()
    {
        var axis = new Axis();
        Assert.False(axis.Inverted);
    }

    /// <summary>Verifies that all axis properties can be set and retrieved.</summary>
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
