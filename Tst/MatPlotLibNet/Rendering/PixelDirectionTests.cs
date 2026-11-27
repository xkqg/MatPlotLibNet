// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>One direction of a <see cref="DataTransform"/>: a value through its axis' scale and past its breaks, then spread over
/// its edge of the plot by one signed rate per unit. The X direction and the Y direction (whose rate is negative, so a larger
/// value lands higher) are the same rule; these tests read it on its own, and the DataTransform suites hold the transform.</summary>
public sealed class PixelDirectionTests
{
    // An X direction over [0, 10] spread on the pixels 50..450, and a Y direction over [0, 10] on 400..0 (drawn upward).
    private static PixelDirection X(AxisScale scale = AxisScale.Linear, AxisBreakMap? breaks = null) =>
        new(40, 0, 50, breaks, scale.Rule, new Range1D(0, 10));

    private static PixelDirection Y() => new(-40, 0, 400, null, ScaleRule.Linear, new Range1D(0, 10));

    [Fact]
    public void AValue_IsMeasuredFromWhereTheRangeStarts()
    {
        // Over [1000, 1010] on the pixels 50..450: 1005 is 5 units in, at 50 + 5 * 40.
        var x = new PixelDirection(40, 1000, 50, null, ScaleRule.Linear, new Range1D(1000, 1010));

        Assert.Equal(250, x.Place(1005));
        Assert.Equal(1000, x.Anchor);
        Assert.Equal(50, x.Start);
        Assert.Equal([50, 250, 450], x.Transform([1000, 1005, 1010]));
    }

    [Fact]
    public void AWholeDirection_IsPlacedAsEachOfItsValues_ToTheBit()
    {
        var x = new PixelDirection(517.9 / 3, 2.5, 13.7, null, ScaleRule.Linear, new Range1D(2.5, 5.5));
        var rng = new Random(3);
        var values = Enumerable.Range(0, 37).Select(_ => 2.5 + rng.NextDouble() * 3).ToArray();

        var pixels = x.Transform(values);

        for (int i = 0; i < values.Length; i++)
            Assert.Equal(BitConverter.DoubleToInt64Bits(x.Place(values[i])), BitConverter.DoubleToInt64Bits(pixels[i]));
    }

    [Fact]
    public void AValue_IsSpreadOverTheEdgeByTheSignedRate()
    {
        Assert.Equal(250, X().Place(5));
        Assert.Equal(200, Y().Place(5));
        Assert.Equal(0, Y().Place(10));
    }

    [Fact]
    public void OnAFlatDirection_EveryValueItCanPlaceLandsAtTheCentre_AndOneItCannotStaysNaN()
    {
        var flat = new PixelDirection(0, 1, 250, null, ScaleRule.Log, new Range1D(1, 1));

        Assert.Equal(250, flat.Place(3));
        Assert.True(double.IsNaN(flat.Place(-1)));
        Assert.Equal([250, double.NaN], flat.Transform([3, -1]));
    }

    [Fact]
    public void ALogDirection_PlacesTheDecades()
    {
        var log = new PixelDirection(100, 0, 50, null, ScaleRule.Log, new Range1D(0, 4));

        Assert.Equal(150, log.Place(10));
        Assert.Equal([50, 150, 350], log.Transform([1, 10, 1000]));
    }

    [Fact]
    public void ABreak_IsCutOutOfTheDirection()
    {
        var map = new AxisBreakMap([new AxisBreak(4, 6)], 0, 10, ScaleRule.Linear);
        var broken = new PixelDirection(50, 0, 50, map, ScaleRule.Linear, new Range1D(0, 10));

        Assert.True(double.IsNaN(broken.Place(5)));
        Assert.Equal(broken.Place(7), broken.Transform([7])[0]);
        Assert.Equal(50 + 5 * 50, broken.Place(7));
    }

    [Fact]
    public void AValueOnThePlot_IsPlaced_AndOneOffItOrInABreakIsNot()
    {
        var map = new AxisBreakMap([new AxisBreak(4, 6)], 0, 10, ScaleRule.Linear);
        var broken = new PixelDirection(50, 0, 50, map, ScaleRule.Linear, new Range1D(0, 10));

        Assert.Equal(250, X().OnPlot(5));
        Assert.Equal(450, X().OnPlot(10 + 1e-12)!.Value, 1e-6);
        Assert.Null(X().OnPlot(11));
        Assert.Null(X().OnPlot(double.NaN));
        Assert.Null(broken.OnPlot(5));
    }

    [Fact]
    public void ABaseTheScaleCannotPlace_IsClippedToWhereTheAxisBegins_AndAMissingOneIsNot()
    {
        var log = new PixelDirection(100, 0, 50, null, ScaleRule.Log, new Range1D(0, 4));

        Assert.True(log.ClipsBase(0));
        Assert.True(log.ClipsBase(-3));
        Assert.False(log.ClipsBase(double.NaN));
        Assert.False(log.ClipsBase(10));
        Assert.False(X().ClipsBase(-3));
        Assert.Equal([50, 150, double.NaN], log.TransformClipped([0, 10, double.NaN], 50));
    }

    [Fact]
    public void ALinearDirection_TransformsAsItPlaces()
    {
        double[] values = [0, 1.5, 2.25, 7, 10, -3];

        Assert.Equal(values.Select(v => X().Place(v)), X().Transform(values));
        Assert.Equal(values.Select(v => Y().Place(v)), Y().Transform(values));
    }

    /// <summary>Over 0 to 125 with 25 to 85 cut out a fifth of the mean part apart (6.5 of 71.5 drawn units on 462 pixels up from 534)
    /// the room stands from 330.46 to 372.46, its jump 53.5 units; on an X axis drawn right to left two rooms stand in screen order the
    /// other way round from the map's.</summary>
    [Fact]
    public void TheRoomsOfAnAxisDrawnInParts_StandOnTheScreenFromTheirFirstPixelToTheirLast_InScreenOrder()
    {
        var y = new PixelDirection(-462 / 71.5, 0, 534, new AxisBreakMap([new AxisBreak(25, 85)], 0, 125, null, spacing: 0.2),
            ScaleRule.Linear, new Range1D(0, 125));
        var reversedX = new PixelDirection(-4, 0, 450, new AxisBreakMap([new AxisBreak(60, 70), new AxisBreak(20, 30)], 0, 100, null, spacing: 0.2),
            ScaleRule.Linear, new Range1D(0, 100));

        var room = Assert.Single(y.PixelRooms());
        Assert.Equal(new PixelRoom(330.461538461538, 372.461538461538, 345.692307692308), room, Close);
        Assert.Equal(room, y.RoomAt(0));
        PixelRoom first = new(348.666666666667, 370, 18.6666666666667), second = new(207.333333333333, 228.666666666667, 18.6666666666667);
        Assert.Equal([second, first], reversedX.PixelRooms(), Close);
        Assert.Equal(first, reversedX.RoomAt(0), Close);
        Assert.Empty(new PixelDirection(-4, 0, 450, new AxisBreakMap([new AxisBreak(25, 85)], 0, 125), ScaleRule.Linear, new Range1D(0, 125)).PixelRooms());
        Assert.Empty(X().PixelRooms());
    }

    /// <summary>Two rooms on the screen read as one within a billionth of a pixel.</summary>
    private static readonly Func<PixelRoom, PixelRoom, bool> Close = (a, b) =>
        Math.Abs(a.Lo - b.Lo) < 1e-9 && Math.Abs(a.Hi - b.Hi) < 1e-9 && Math.Abs(a.Jump - b.Jump) < 1e-9;

    [Fact]
    public void OnlyABreakOrAScaleThatIsNotLinear_ShapesADirection()
    {
        Assert.False(X().IsShaped);
        Assert.True(X(AxisScale.SymLog).IsShaped);
        Assert.True(X(breaks: new AxisBreakMap([new AxisBreak(4, 6)], 0, 10, ScaleRule.Linear)).IsShaped);
    }
}
