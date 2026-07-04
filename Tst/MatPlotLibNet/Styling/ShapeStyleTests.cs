// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="ShapeStyle"/> value semantics and the centralized fill/stroke visibility guards.</summary>
public class ShapeStyleTests
{
    /// <summary>Verifies that the constructor sets Fill, Stroke, and StrokeThickness.</summary>
    [Fact]
    public void Constructor_SetsComponents()
    {
        var shape = new ShapeStyle(Colors.Red, Colors.Black, 1.5);
        Assert.Equal(Colors.Red, shape.Fill);
        Assert.Equal(Colors.Black, shape.Stroke);
        Assert.Equal(1.5, shape.StrokeThickness);
    }

    /// <summary>Verifies that the record struct deconstructs into its three components.</summary>
    [Fact]
    public void Deconstruct_YieldsComponents()
    {
        var (fill, stroke, thickness) = new ShapeStyle(Colors.Green, null, 2.0);
        Assert.Equal(Colors.Green, fill);
        Assert.Null(stroke);
        Assert.Equal(2.0, thickness);
    }

    /// <summary>Verifies value equality for identical component values.</summary>
    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new ShapeStyle(Colors.Blue, Colors.White, 3.0);
        var b = new ShapeStyle(Colors.Blue, Colors.White, 3.0);
        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    /// <summary>Verifies inequality when any component differs.</summary>
    [Fact]
    public void Equality_DifferentStroke_AreNotEqual()
    {
        var a = new ShapeStyle(Colors.Blue, Colors.White, 3.0);
        var b = new ShapeStyle(Colors.Blue, null, 3.0);
        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    /// <summary>Verifies that a present fill is reported as visible.</summary>
    [Fact]
    public void HasVisibleFill_WithFill_True()
    {
        Assert.True(new ShapeStyle(Colors.Red, null, 0.0).HasVisibleFill);
    }

    /// <summary>Verifies that a null fill is reported as not visible.</summary>
    [Fact]
    public void HasVisibleFill_NullFill_False()
    {
        Assert.False(new ShapeStyle(null, Colors.Black, 1.0).HasVisibleFill);
    }

    /// <summary>Verifies that a present stroke with positive thickness is visible.</summary>
    [Fact]
    public void HasVisibleStroke_StrokeAndPositiveThickness_True()
    {
        Assert.True(new ShapeStyle(null, Colors.Black, 1.0).HasVisibleStroke);
    }

    /// <summary>Verifies that a null stroke is not visible.</summary>
    [Fact]
    public void HasVisibleStroke_NullStroke_False()
    {
        Assert.False(new ShapeStyle(Colors.Red, null, 1.0).HasVisibleStroke);
    }

    /// <summary>Verifies the centralized guard: a stroke with zero thickness is not visible.
    /// This is the semantic previously enforced only by the Skia backend; SVG and MAUI now
    /// consult the same guard uniformly.</summary>
    [Fact]
    public void HasVisibleStroke_ZeroThickness_False()
    {
        Assert.False(new ShapeStyle(null, Colors.Black, 0.0).HasVisibleStroke);
    }

    /// <summary>Verifies the centralized guard: a stroke with negative thickness is not visible.</summary>
    [Fact]
    public void HasVisibleStroke_NegativeThickness_False()
    {
        Assert.False(new ShapeStyle(null, Colors.Black, -2.0).HasVisibleStroke);
    }
}
