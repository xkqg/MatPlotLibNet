// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies <see cref="DataTransform.Of"/>, the one rule every Cartesian transform is built by, and the
/// <see cref="AxisMapping"/> of a direction it reads.</summary>
public class DataTransformOfTests
{
    private static readonly Rect Plot = new(100, 50, 500, 400);
    private static readonly DataRange Range = new(1, 1e4, -2, 8);
    private static readonly AxisMapping Plain = new(ScaleRule.Linear, []);

    [Theory]
    [InlineData(AxisScale.Linear, 0, false)]
    [InlineData(AxisScale.Logit, 0, true)]
    [InlineData(AxisScale.Date, 0, false)]
    [InlineData(AxisScale.Log, 0, true)]
    [InlineData(AxisScale.SymLog, 0, true)]
    [InlineData(AxisScale.Linear, 1, true)]
    public void IsShaped_ABreakOrALogSymLogOrLogitScale(AxisScale scale, int breaks, bool shaped) =>
        Assert.Equal(shaped, new AxisMapping(scale.Rule, [.. Enumerable.Repeat(new AxisBreak(2, 3), breaks)]).IsShaped);

    [Fact]
    public void MapOver_NoBreaks_IsNull() => Assert.Null(Plain.MapOver(0, 10));

    [Fact]
    public void MapOver_WithBreaks_IsTheMapInTheMappingsScale()
    {
        var map = new AxisMapping(ScaleRule.Log, [new AxisBreak(10, 100)]).MapOver(1, 1e4);
        Assert.NotNull(map);
        Assert.Equal(new MatPlotLibNet.Numerics.MinMaxRange(0, 3), map.CompressedRange);
    }

    [Fact]
    public void Of_NeitherDirectionShaped_IsThePlainLinearTransform()
    {
        var t = DataTransform.Of(Range, Plot, Plain, new AxisMapping(ScaleRule.Date, []));
        var plain = new DataTransform(Range.XMin, Range.XMax, Range.YMin, Range.YMax, Plot);

        Assert.Equal(plain.DataToPixel(5000, 3), t.DataToPixel(5000, 3));
        Assert.Equal(AxisScale.Linear, t.YRule.Scale);
    }

    [Fact]
    public void Of_ALogX_DrawsTheScaledRange_AndKeepsTheOtherDirectionsOwnScale()
    {
        var t = DataTransform.Of(Range, Plot, new AxisMapping(ScaleRule.Log, []), new AxisMapping(ScaleRule.Date, []));

        Assert.Equal(0, t.DataXMin, 12);
        Assert.Equal(4, t.DataXMax, 12);
        Assert.Equal(AxisScale.Date, t.YRule.Scale);
        Assert.Equal(Plot.X + Plot.Width / 2, t.DataToPixel(100, 0).X, 9);
    }

    [Fact]
    public void Of_ABrokenY_DrawsTheCompressedRange_AndPlacesByTheSameMap()
    {
        var t = DataTransform.Of(Range, Plot, Plain, new AxisMapping(ScaleRule.Linear, [new AxisBreak(0, 4)]));

        Assert.Equal(-2, t.DataYMin, 12);
        Assert.Equal(4, t.DataYMax, 12);
        Assert.Equal(t.DataToPixel(1, 0).Y, t.DataToPixel(1, 4).Y, 9);
        Assert.True(double.IsNaN(t.DataToPixel(1, 2).Y));
        Assert.Equal(Plot.Y, t.DataToPixel(1, 8).Y, 9);
    }

    [Fact]
    public void Of_AgreesWithThePublicConstructor_HandedTheRangeOfTheSameRule()
    {
        var x = new AxisMapping(ScaleRule.SymLogAt(10), [new AxisBreak(100, 1000)]);
        var y = new AxisMapping(ScaleRule.Log, [new AxisBreak(10, 100)]);
        var range = new DataRange(-1e4, 1e4, 1, 1e4);
        var built = DataTransform.Of(range, Plot, x, y);
        var (xMin, xMax) = x.MapOver(range.XMin, range.XMax)!.CompressedRange;
        var (yMin, yMax) = y.MapOver(range.YMin, range.YMax)!.CompressedRange;
        var handed = new DataTransform(xMin, xMax, yMin, yMax, Plot, x.Breaks, y.Breaks,
            range.XMin, range.XMax, range.YMin, range.YMax, x.Rule.Scale, y.Rule.Scale, x.Rule.LinearThreshold ?? 1.0, y.Rule.LinearThreshold ?? 1.0);

        foreach (var (px, py) in new[] { (-5000.0, 2.0), (50.0, 5.0), (5000.0, 500.0), (1e4, 1e4) })
            Assert.Equal(handed.DataToPixel(px, py), built.DataToPixel(px, py));
    }
}
