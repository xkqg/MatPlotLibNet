// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

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

    // --- TickConfig defaults (v1.1.2 matplotlib-fidelity fixes) ---

    /// <summary>Verifies that the default tick length matches matplotlib's xtick.major.size (3.5 pt) pre-converted to pixels at 100 DPI.</summary>
    [Fact]
    public void TickConfig_DefaultLength_MatchesMatplotlib()
    {
        var tc = new TickConfig();
        Assert.Equal(3.5 * 100.0 / 72.0, tc.Length, 3);
    }

    /// <summary>Verifies that the default tick width matches matplotlib's xtick.major.width (0.8 pt) pre-converted to pixels at 100 DPI.</summary>
    [Fact]
    public void TickConfig_DefaultWidth_MatchesMatplotlib()
    {
        var tc = new TickConfig();
        Assert.Equal(0.8 * 100.0 / 72.0, tc.Width, 3);
    }

    /// <summary>Verifies that the default tick direction is Out (matches matplotlib's xtick.direction).</summary>
    [Fact]
    public void TickConfig_DefaultDirection_IsOut()
    {
        var tc = new TickConfig();
        Assert.Equal(TickDirection.Out, tc.Direction);
    }

    // ── Axis.LabelRotation shorthand ─────────────────────────────────────────

    [Fact]
    public void LabelRotation_Default_IsZero()
    {
        Assert.Equal(0.0, new Axis().LabelRotation);
    }

    [Fact]
    public void LabelRotation_Set_UpdatesMajorTicks()
    {
        var axis = new Axis();
        axis.LabelRotation = -45.0;
        Assert.Equal(-45.0, axis.MajorTicks.LabelRotation);
    }

    [Fact]
    public void LabelRotation_Get_ReadsMajorTicks()
    {
        var axis = new Axis();
        axis.MajorTicks = axis.MajorTicks with { LabelRotation = 30.0 };
        Assert.Equal(30.0, axis.LabelRotation);
    }

    [Fact]
    public void LabelRotation_SetNegative_Accepted()
    {
        var axis = new Axis();
        axis.LabelRotation = -90.0;
        Assert.Equal(-90.0, axis.LabelRotation);
    }

    [Fact]
    public void LabelRotation_SetDoesNotAffectMinorTicks()
    {
        var axis = new Axis();
        double before = axis.MinorTicks.LabelRotation;
        axis.LabelRotation = -45.0;
        Assert.Equal(before, axis.MinorTicks.LabelRotation);
    }
}
