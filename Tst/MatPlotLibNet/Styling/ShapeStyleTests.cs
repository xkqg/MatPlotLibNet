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

    public static TheoryData<ShapeStyle, bool, bool> Visibility => new()
    {
        { new ShapeStyle(Colors.Red, null, 0.0), true, false },
        { new ShapeStyle(null, Colors.Black, 1.0), false, true },
        { new ShapeStyle(Colors.Red, null, 1.0), true, false },
        { new ShapeStyle(null, Colors.Black, 0.0), false, false },
        { new ShapeStyle(null, Colors.Black, -2.0), false, false },
    };

    /// <summary>A fill is visible when there is one; a stroke when there is one and its thickness is positive, the one guard the SVG,
    /// Skia and MAUI backends read.</summary>
    [Theory]
    [MemberData(nameof(Visibility))]
    public void AFill_IsVisibleWhenGiven_AndAStroke_WhenGivenAPositiveThickness(ShapeStyle shape, bool fill, bool stroke)
    {
        Assert.Equal(fill, shape.HasVisibleFill);
        Assert.Equal(stroke, shape.HasVisibleStroke);
    }

    /// <summary>A shape built without a line style strokes solid, so every figure drawn before the property existed is
    /// drawn as it was.</summary>
    [Fact]
    public void TheStrokeLineStyle_DefaultsToSolid()
    {
        Assert.Equal(LineStyle.Solid, new ShapeStyle(null, Colors.Black, 1.0).StrokeLineStyle);
    }

    /// <summary>A stroke whose line style is <see cref="LineStyle.None"/> draws no line, as a <see cref="StrokeStyle"/> with
    /// that style does. The guard is <see cref="ShapeStyle.HasVisibleStroke"/>, which every backend already consults, so none
    /// of them has to know the rule; the fill is not touched.</summary>
    [Fact]
    public void ANoneStrokeLineStyle_DrawsNoStroke()
    {
        var shape = new ShapeStyle(Colors.Red, Colors.Black, 2.0) { StrokeLineStyle = LineStyle.None };

        Assert.False(shape.HasVisibleStroke);
        Assert.True(shape.HasVisibleFill);
    }

    /// <summary>Every style that is drawn as a line keeps the stroke visible; only <see cref="LineStyle.None"/> removes it.</summary>
    [Theory]
    [InlineData(LineStyle.Solid)]
    [InlineData(LineStyle.Dashed)]
    [InlineData(LineStyle.Dotted)]
    [InlineData(LineStyle.DashDot)]
    public void ADrawnStrokeLineStyle_KeepsTheStrokeVisible(LineStyle style)
    {
        Assert.True(new ShapeStyle(null, Colors.Black, 1.0) { StrokeLineStyle = style }.HasVisibleStroke);
    }

    /// <summary>The line style is part of the value: two shapes that stroke differently are different shapes.</summary>
    [Fact]
    public void Equality_DifferentStrokeLineStyle_AreNotEqual()
    {
        var solid = new ShapeStyle(Colors.Blue, Colors.White, 3.0);
        var dashed = solid with { StrokeLineStyle = LineStyle.Dashed };

        Assert.NotEqual(solid, dashed);
        Assert.Equal(dashed, new ShapeStyle(Colors.Blue, Colors.White, 3.0) { StrokeLineStyle = LineStyle.Dashed });
    }

    /// <summary>A shape with both a gradient and a hatch is drawn as two layers; the lower one is the shape itself, so it
    /// carries the stroke, and the stroke carries its line style.</summary>
    [Fact]
    public void TheLayerUnderTheHatch_KeepsTheStrokeLineStyle()
    {
        var shape = new ShapeStyle(Colors.Red, Colors.Black, 2.0)
        {
            Hatch = HatchPattern.Cross,
            StrokeLineStyle = LineStyle.Dotted,
        };

        Assert.Equal(LineStyle.Dotted, shape.UnderTheHatch.StrokeLineStyle);
    }

    /// <summary>The hatch layer paints strokes of hatch marks over a fill that is not there and has no outline of its own,
    /// so it keeps the default line style and draws no stroke whatever the shape's was.</summary>
    [Fact]
    public void TheHatchLayer_HasNoStroke_WhateverTheShapesLineStyle()
    {
        var shape = new ShapeStyle(Colors.Red, Colors.Black, 2.0)
        {
            Hatch = HatchPattern.Cross,
            StrokeLineStyle = LineStyle.Dashed,
        };

        Assert.False(shape.HatchLayer.HasVisibleStroke);
        Assert.Equal(LineStyle.Solid, shape.HatchLayer.StrokeLineStyle);
    }

    /// <summary>The strokes of a hatch are the colour the shape names for them.</summary>
    [Fact]
    public void TheHatchStroke_IsTheHatchColorWhenTheShapeNamesOne()
    {
        var shape = new ShapeStyle(Colors.White, null, 0) { Hatch = HatchPattern.Cross, HatchColor = Colors.Red };

        Assert.Equal(Colors.Red, shape.HatchStroke);
    }

    /// <summary>Without a hatch colour the strokes are the contrasting shade of the fill: black on a light fill and white on a
    /// dark one, so one property is enough to get a visible hatch.</summary>
    [Fact]
    public void TheHatchStroke_FallsBackToTheContrastOfTheFill()
    {
        var light = new ShapeStyle(Colors.White, null, 0) { Hatch = HatchPattern.Cross };
        var dark = new ShapeStyle(Colors.Black, null, 0) { Hatch = HatchPattern.Cross };

        Assert.Equal(Colors.Black, light.HatchStroke);
        Assert.Equal(Colors.White, dark.HatchStroke);
    }

    /// <summary>The hatch layer of a shape with a gradient paints in the colour the shape's own hatch resolves to.</summary>
    [Fact]
    public void TheHatchLayer_PaintsInTheHatchStroke()
    {
        var shape = new ShapeStyle(Colors.Black, null, 0) { Hatch = HatchPattern.Cross };

        Assert.Equal(shape.HatchStroke, shape.HatchLayer.HatchColor);
    }

    /// <summary>A shape with a fill and no outline is one value with one spelling: the fill, no stroke colour, a stroke of no
    /// width. Every mark a renderer fills and does not outline is built by it.</summary>
    [Fact]
    public void Filled_IsAFillWithNoOutline()
    {
        var shape = ShapeStyle.Filled(Colors.Red);

        Assert.Equal(new ShapeStyle(Colors.Red, null, 0), shape);
        Assert.True(shape.HasVisibleFill);
        Assert.False(shape.HasVisibleStroke);
    }

    /// <summary>A fill-only shape takes the hatch and the gradient as any shape does, with a <c>with</c> where an init block stood.</summary>
    [Fact]
    public void Filled_TakesAHatchAndAGradientLikeAnyShape()
    {
        var hatched = ShapeStyle.Filled(Colors.White) with { Hatch = HatchPattern.Cross, HatchColor = Colors.Red };

        Assert.Equal(new ShapeStyle(Colors.White, null, 0) { Hatch = HatchPattern.Cross, HatchColor = Colors.Red }, hatched);
        Assert.True(hatched.HasVisibleHatch);
    }

    /// <summary>A shape with an outline and no fill is the stroke colour at its width over no fill.</summary>
    [Fact]
    public void Outlined_IsAnOutlineOverNoFill()
    {
        var shape = ShapeStyle.Outlined(Colors.Blue, 2.5);

        Assert.Equal(new ShapeStyle(null, Colors.Blue, 2.5), shape);
        Assert.False(shape.HasVisibleFill);
        Assert.True(shape.HasVisibleStroke);
    }

    /// <summary>A fill edged by an edge is the one construction every shape goes through: either may be absent where its source names none,
    /// and the shape then has no fill or no outline.</summary>
    [Fact]
    public void Edged_IsTheFillTheEdgeAndItsWidth_EitherMayBeAbsent()
    {
        Assert.Equal(new ShapeStyle(Colors.Red, Colors.Blue, 1.5), ShapeStyle.Edged(Colors.Red, Colors.Blue, 1.5));
        Assert.Equal(ShapeStyle.Filled(Colors.Red), ShapeStyle.Edged(Colors.Red, null, 0));
        Assert.Equal(ShapeStyle.Outlined(Colors.Blue, 2), ShapeStyle.Edged(null, Colors.Blue, 2));
    }

    /// <summary>The stroke an outline is drawn with is its colour, width, dash pattern and join, as every backend that draws a shape's
    /// outline as a line reads them.</summary>
    [Fact]
    public void OutlineStroke_CarriesColourWidthDashAndJoin()
    {
        var shape = new ShapeStyle(Colors.Red, Colors.Black, 1.5) { StrokeLineStyle = LineStyle.Dashed, StrokeJoin = JoinStyle.Round };

        var stroke = shape.OutlineStroke();

        Assert.Equal(new StrokeStyle(Colors.Black, 1.5, LineStyle.Dashed) { Join = JoinStyle.Round }, stroke);
    }

    /// <summary>A shape built the plain way strokes with the mitre, a solid line, as it always did.</summary>
    [Fact]
    public void OutlineStroke_OfAPlainShape_IsSolidAndMitred()
    {
        var stroke = new ShapeStyle(null, Colors.Black, 1).OutlineStroke();

        Assert.Equal(LineStyle.Solid, stroke.Style);
        Assert.Equal(JoinStyle.Miter, stroke.Join);
    }

    /// <summary>A shape with no stroke colour has no outline to hand out: asking for it is a mistake, not a default colour.</summary>
    [Fact]
    public void OutlineStroke_OfAShapeWithNoStrokeColour_IsRefused()
    {
        Assert.Throws<InvalidOperationException>(() => ShapeStyle.Filled(Colors.Red).OutlineStroke());
    }

    /// <summary>Writing a shape out (a failed assertion does, a debugger does) never asks for an outline it does not have.</summary>
    [Fact]
    public void AFilledShape_CanBeWrittenOut()
    {
        Assert.NotEmpty(ShapeStyle.Filled(Colors.Red).ToString());
    }
}
