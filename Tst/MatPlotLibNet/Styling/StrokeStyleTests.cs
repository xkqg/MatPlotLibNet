// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="StrokeStyle"/> value semantics and the centralized visibility guard.</summary>
public class StrokeStyleTests
{
    /// <summary>Verifies that the constructor sets Color, Thickness, and Style.</summary>
    [Fact]
    public void Constructor_SetsComponents()
    {
        var stroke = new StrokeStyle(new Color(10, 20, 30), 2.5, LineStyle.Dashed);
        Assert.Equal(new Color(10, 20, 30), stroke.Color);
        Assert.Equal(2.5, stroke.Thickness);
        Assert.Equal(LineStyle.Dashed, stroke.Style);
    }

    /// <summary>Verifies that the record struct deconstructs into its three components.</summary>
    [Fact]
    public void Deconstruct_YieldsComponents()
    {
        var (color, thickness, style) = new StrokeStyle(Colors.Red, 1.0, LineStyle.Dotted);
        Assert.Equal(Colors.Red, color);
        Assert.Equal(1.0, thickness);
        Assert.Equal(LineStyle.Dotted, style);
    }

    /// <summary>Verifies value equality for identical component values.</summary>
    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new StrokeStyle(Colors.Blue, 3.0, LineStyle.Solid);
        var b = new StrokeStyle(Colors.Blue, 3.0, LineStyle.Solid);
        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    /// <summary>Verifies inequality when any component differs.</summary>
    [Fact]
    public void Equality_DifferentThickness_AreNotEqual()
    {
        var a = new StrokeStyle(Colors.Blue, 3.0, LineStyle.Solid);
        var b = new StrokeStyle(Colors.Blue, 4.0, LineStyle.Solid);
        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    /// <summary>Verifies that a positive-thickness solid stroke is visible.</summary>
    [Fact]
    public void IsVisible_PositiveThicknessSolid_True()
    {
        Assert.True(new StrokeStyle(Colors.Black, 1.0, LineStyle.Solid).IsVisible);
    }

    /// <summary>Verifies that a zero-thickness stroke is not visible.</summary>
    [Fact]
    public void IsVisible_ZeroThickness_False()
    {
        Assert.False(new StrokeStyle(Colors.Black, 0.0, LineStyle.Solid).IsVisible);
    }

    /// <summary>Verifies that a negative-thickness stroke is not visible.</summary>
    [Fact]
    public void IsVisible_NegativeThickness_False()
    {
        Assert.False(new StrokeStyle(Colors.Black, -1.0, LineStyle.Solid).IsVisible);
    }

    /// <summary>Verifies that a <see cref="LineStyle.None"/> stroke is not visible even with positive thickness.</summary>
    [Fact]
    public void IsVisible_StyleNone_False()
    {
        Assert.False(new StrokeStyle(Colors.Black, 2.0, LineStyle.None).IsVisible);
    }
}
