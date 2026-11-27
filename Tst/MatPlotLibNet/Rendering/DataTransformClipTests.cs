// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The one rule a mark that stands on a base draws by: a base the axis' scale cannot place (0 or a negative value
/// on a log axis) is clipped to the edge where the axis begins, as matplotlib clips it (<c>nonpositive='clip'</c>). A
/// missing value or an infinity is not clipped: it stays NaN and the mark is skipped, as a line or a marker that cannot be
/// placed is masked. A value the scale can place is exactly where <see cref="DataTransform.DataToPixel"/> puts it.</summary>
public class DataTransformClipTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    [Theory]
    [InlineData(0.0)]
    [InlineData(-5.0)]
    [InlineData(double.MinValue)]
    public void ABaseTheLogYScaleCannotPlace_IsClippedToTheBottomEdge(double y)
    {
        var transform = Bounds.LogY();

        var clipped = transform.DataToPixelClippedAlong(false, 3, y);

        Assert.Equal(new Point(transform.DataToPixel(3, 10).X, 300), clipped);
    }

    [Fact]
    public void ANegativeZeroBase_IsClippedAsZeroIs() => Assert.Equal(300, Bounds.LogY().DataToPixelClippedAlong(false, 3, -0.0).Y);

    /// <summary>A reversed log axis begins at the other edge: a base it cannot place is clipped to the top edge of a reversed Y axis and to the
    /// right edge of a reversed X axis, where the axis begins.</summary>
    public static TheoryData<Func<double>, double> ClippedToWhereAReversedAxisBegins => new()
    {
        { () => Bounds.LogYReversed().DataToPixelClippedAlong(false, 3, 0).Y, 0 },
        { () => Bounds.LogXReversed().DataToPixelClippedAlong(true, -1, 4).X, 400 },
    };

    [Theory]
    [MemberData(nameof(ClippedToWhereAReversedAxisBegins))]
    public void OnAReversedLogAxis_ABaseItCannotPlace_IsClippedToTheEdge_WhereTheAxisBegins(Func<double> clipped, double edge) =>
        Assert.Equal(edge, clipped());

    [Fact]
    public void ABaseTheLogXScaleCannotPlace_IsClippedToTheLeftEdge()
    {
        var transform = Bounds.LogX();

        Assert.Equal(new Point(0, transform.DataToPixel(10, 4).Y), transform.DataToPixelClippedAlong(true, 0, 4));
    }

    /// <summary>Only the base is clipped: the other coordinate is mapped as ever, so an X the axis cannot place stays NaN.</summary>
    [Fact]
    public void OnlyTheBaseCoordinateIsClipped()
    {
        Assert.True(double.IsNaN(Bounds.LogX().DataToPixelClippedAlong(false, 0, 4).X));
        Assert.True(double.IsNaN(Bounds.LogY().DataToPixelClippedAlong(true, 3, 0).Y));
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(10.0)]
    [InlineData(0.5)]     // placeable, below the range: beyond the edge, as DataToPixel puts it
    [InlineData(1000.0)]
    public void ABaseTheScaleCanPlace_IsWhereDataToPixelPutsIt(double value)
    {
        Assert.Equal(Bounds.LogY().DataToPixel(3, value), Bounds.LogY().DataToPixelClippedAlong(false, 3, value));
        Assert.Equal(Bounds.LogX().DataToPixel(value, 4), Bounds.LogX().DataToPixelClippedAlong(true, value, 4));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void AMissingOrInfiniteBase_IsNotClipped_ItCannotBeDrawn(double value)
    {
        Assert.False(double.IsFinite(Bounds.LogY().DataToPixelClippedAlong(false, 3, value).Y));
        Assert.False(double.IsFinite(Bounds.LogX().DataToPixelClippedAlong(true, value, 4).X));
    }

    /// <summary>A linear axis places every finite value: a negative base is drawn where it lies.</summary>
    [Fact]
    public void OnALinearAxis_ANegativeBase_IsWhereDataToPixelPutsIt()
    {
        var linear = new DataTransform(0, 10, -10, 10, Bounds);

        Assert.Equal(linear.DataToPixel(3, -5), linear.DataToPixelClippedAlong(false, 3, -5));
        Assert.Equal(linear.DataToPixel(-5, 3), linear.DataToPixelClippedAlong(true, -5, 3));
    }

    /// <summary>A batch of values placed along a direction is each value placed alone: the clipped batch as the clipped point, the
    /// unclipped batch as the unclipped point, the X axis' along X, the Y axis' along Y.</summary>
    [Fact]
    public void ABatchPlacedAlongADirection_PlacesEachValueAsTheSingleValueIsPlaced()
    {
        double[] values = [0, 1, 10, 100];

        Assert.Equal([.. values.Select(v => Bounds.LogX().DataToPixelClippedAlong(true, v, 4).X)], Bounds.LogX().TransformClippedAlong(true, values));
        Assert.Equal([.. values.Select(v => Bounds.LogY().DataToPixelClippedAlong(false, 3, v).Y)], Bounds.LogY().TransformClippedAlong(false, values));
        Assert.Equal([.. values.Select(v => Bounds.LogX().DataToPixelUnclipped(v, 4).X)], Bounds.LogX().TransformUnclippedAlong(true, values));
        Assert.Equal([.. values.Select(v => Bounds.LogY().DataToPixelUnclipped(3, v).Y)], Bounds.LogY().TransformUnclippedAlong(false, values));
    }

    [Fact]
    public void TheBatchTransform_ClipsEachBaseTheScaleCannotPlace_AndLeavesTheRest()
    {
        var transform = Bounds.LogY();
        double[] bases = [10, 0, -1, double.NaN, 100];

        double[] pixels = transform.TransformClippedAlong(false, bases);

        Assert.Equal(transform.TransformY(bases)[0], pixels[0]);
        Assert.Equal(300, pixels[1]);
        Assert.Equal(300, pixels[2]);
        Assert.True(double.IsNaN(pixels[3]));
        Assert.Equal(transform.TransformY(bases)[4], pixels[4]);
    }

    /// <summary>On a linear axis the batch is the plain batch, bit for bit (the SIMD path): no figure on a linear axis
    /// changes a byte.</summary>
    [Fact]
    public void OnALinearAxis_TheBatchTransform_IsThePlainOne()
    {
        var linear = new DataTransform(0, 10, -10, 10, Bounds);
        double[] bases = [-7.3, 0, 0.1, 9.99, double.NaN];

        Assert.Equal(linear.TransformY(bases), linear.TransformClippedAlong(false, bases));
    }
}
