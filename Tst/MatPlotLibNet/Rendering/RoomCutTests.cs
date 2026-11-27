// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A path across the room between two parts of an axis drawn in parts is cut there as matplotlib's broken-axis recipe clips
/// each part (gallery <c>broken_axis</c>): a straight piece of the data runs in each part at the slope the data gives it there, an open
/// path breaks between the parts, a closed one runs straight through the room. Read through the transform members that hand a path to
/// the cut: <see cref="DataTransform.TransformPath"/> for a line and <see cref="DataTransform.CutBeyondTheStart"/> for an outline.</summary>
public sealed class RoomCutTests
{
    private static readonly Rect Plot = new(100, 72, 620, 462);

    /// <summary>A gap in a path, or two points within a millionth of a pixel.</summary>
    private static readonly Func<Point, Point, bool> Same = (expected, actual) =>
        double.IsNaN(expected.X) ? double.IsNaN(actual.X) && double.IsNaN(actual.Y) : expected.IsCloseTo(actual, 1e-6);

    /// <summary>The axes drawn in parts over <paramref name="range"/> (x low, x high, y low, y high), each break a pair of numbers.</summary>
    private static DataTransform InParts(double[] range, double[] xBreaks, double[] yBreaks) =>
        Plot.InParts(new DataRange(range[0], range[1], range[2], range[3]),
            [.. xBreaks.Chunk(2).Select(b => new AxisBreak(b[0], b[1]))], [.. yBreaks.Chunk(2).Select(b => new AxisBreak(b[0], b[1]))]);

    /// <summary>The range, the breaks of each axis, the line's points and what is drawn of it, in data units (a NaN pair is a gap).</summary>
    public static TheoryData<double[], double[], double[], double[], double[], double[]> Lines => new()
    {
        // (9, 18) to (10, 100) leaves the lower part at y = 25 (x = 9 + 7/82) and comes into the upper one at y = 85 (x = 9 + 67/82).
        { [0, 20, 0, 125], [], [25, 85], [9, 10], [18, 100], [9, 18, 9 + 7.0 / 82, 25, double.NaN, double.NaN, 9 + 67.0 / 82, 85, 10, 100] },
        // Down across two rooms of one axis: cut at each.
        { [0, 10, 0, 100], [], [60, 70, 20, 30], [0, 10], [100, 0], [0, 100, 3, 70, double.NaN, double.NaN, 4, 60, 7, 30, double.NaN, double.NaN, 8, 20, 10, 0] },
        // In a room of each axis at once (it crosses where they meet): hidden once, from the first entry to the last exit.
        { [0, 100, 0, 100], [40, 60], [40, 60], [0, 100], [0, 100], [0, 0, 40, 40, double.NaN, double.NaN, 60, 60, 100, 100] },
        { [0, 100, 0, 100], [40, 60], [45, 70], [0, 100], [0, 100], [0, 0, 40, 40, double.NaN, double.NaN, 70, 70, 100, 100] },
    };

    [Theory]
    [MemberData(nameof(Lines))]
    public void ALineAcrossARoom_RunsAtTheDatasSlopeInEachPart_AndBreaksBetweenThem(double[] range, double[] xBreaks, double[] yBreaks,
        double[] x, double[] y, double[] drawn)
    {
        var transform = InParts(range, xBreaks, yBreaks);

        Assert.Equal(drawn.Chunk(2).Select(p => transform.DataToPixel(p[0], p[1])), transform.TransformPath(x, y), Same);
    }

    [Fact]
    public void AnOutlineAcrossTheRoom_HasItsEdgeAtTheDatasSlopeInEachPart_AndRunsStraightThroughTheRoom()
    {
        var transform = InParts([0, 20, 0, 125], [], [25, 85]);
        Point[] outline = [transform.DataToPixel(0, 0), transform.DataToPixel(10, 100), transform.DataToPixel(10, 0)];

        Assert.Equal([outline[0], transform.DataToPixel(2.5, 25), transform.DataToPixel(8.5, 85), outline[1],
            transform.DataToPixel(10, 85), transform.DataToPixel(10, 25), outline[2]], transform.CutBeyondTheStart(outline, closed: true), Same);
    }

    [Fact]
    public void APathThatCrossesNoRoom_HasOnePoint_OrHasAGapAcrossTheRoom_IsHandedBackItself()
    {
        var transform = InParts([0, 20, 0, 125], [], [25, 85]);
        Point[] below = [transform.DataToPixel(0, 0), transform.DataToPixel(1, 10)];
        Point[] gapped = [transform.DataToPixel(9, 18), new(double.NaN, double.NaN), transform.DataToPixel(10, 100)];

        Assert.All([below, below[..1], gapped], path => Assert.Same(path, transform.CutBeyondTheStart(path, closed: false)));
    }
}
