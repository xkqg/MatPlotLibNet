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

    /// <summary>A positive-thickness solid stroke is visible; a zero or negative thickness is not, and a <see cref="LineStyle.None"/> stroke is
    /// not visible even with a positive thickness.</summary>
    [Theory]
    [InlineData(1.0, LineStyle.Solid, true)]
    [InlineData(0.0, LineStyle.Solid, false)]
    [InlineData(-1.0, LineStyle.Solid, false)]
    [InlineData(2.0, LineStyle.None, false)]
    public void IsVisible_OnlyAPositiveThicknessOfADrawnStyle(double thickness, LineStyle style, bool visible)
    {
        Assert.Equal(visible, new StrokeStyle(Colors.Black, thickness, style).IsVisible);
    }

    /// <summary>A solid line is a colour and a width and nothing to say about the dash.</summary>
    [Fact]
    public void Solid_IsAColourAndAWidthInTheSolidStyle()
    {
        var stroke = StrokeStyle.Solid(new Color(10, 20, 30), 2.5);

        Assert.Equal(new StrokeStyle(new Color(10, 20, 30), 2.5, LineStyle.Solid), stroke);
        Assert.Null(stroke.Join);
        Assert.True(stroke.IsVisible);
    }

    /// <summary>The one place a stroke is put together: its colour, width, dash and the join it names, none by default.</summary>
    [Fact]
    public void Of_IsTheColourTheWidthTheDashAndTheJoinNamed()
    {
        var stroke = StrokeStyle.Of(new Color(10, 20, 30), 2.5, LineStyle.DashDot, JoinStyle.Bevel);

        Assert.Equal(new StrokeStyle(new Color(10, 20, 30), 2.5, LineStyle.DashDot) { Join = JoinStyle.Bevel }, stroke);
        Assert.Null(StrokeStyle.Of(Colors.Black, 1, LineStyle.Dashed).Join);
    }

    /// <summary>Whether a stroke has no dash is the stroke's own answer: true for solid only, not for a dashed, dotted or dash-dot line, nor
    /// for no line.</summary>
    [Theory]
    [InlineData(LineStyle.Solid, true)]
    [InlineData(LineStyle.Dashed, false)]
    [InlineData(LineStyle.Dotted, false)]
    [InlineData(LineStyle.DashDot, false)]
    [InlineData(LineStyle.None, false)]
    public void IsSolid_OnlyForALineWithNoDash(LineStyle style, bool solid) =>
        Assert.Equal(solid, StrokeStyle.Of(Colors.Black, 1, style).IsSolid);

    /// <summary>A solid stroke takes a join as any stroke does.</summary>
    [Fact]
    public void Solid_TakesAJoinWithAWithExpression()
    {
        var stroke = StrokeStyle.Solid(Colors.Black, 1) with { Join = JoinStyle.Round };

        Assert.Equal(JoinStyle.Round, stroke.Join);
    }

    /// <summary>A faded stroke is the stroke at a share of its colour's opacity and of its width, the dash pattern and the join kept:
    /// the look a minor grid line has next to a major one. The opacity is scaled by the one rule a colour's own opacity is scaled by
    /// (<c>ScaleAlpha</c>, the nearest step): half of 255 is 128, not the 127 that cutting the fraction off gave.</summary>
    [Fact]
    public void Faded_ScalesTheOpacityAndTheWidthAndKeepsTheRest()
    {
        var stroke = new StrokeStyle(new Color(10, 20, 30, 255), 2.0, LineStyle.Dashed) { Join = JoinStyle.Round };

        var faded = stroke.Faded(0.5);

        Assert.Equal(new Color(10, 20, 30, 128), faded.Color);
        Assert.Equal(stroke.Color.ScaleAlpha(0.5), faded.Color);
        Assert.Equal(1.0, faded.Thickness);
        Assert.Equal(LineStyle.Dashed, faded.Style);
        Assert.Equal(JoinStyle.Round, faded.Join);
    }
}
