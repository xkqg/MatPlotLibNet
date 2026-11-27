// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A straight line drawn in an axes is cut to its plot area, as matplotlib clips every line to the axes: the part
/// inside is kept, the parts outside are not drawn, and a line inside is handed back exactly as it was, so a picture whose
/// lines stay inside does not change by a single digit.</summary>
public class LineSegmentExtensionsTests
{
    /// <summary>A plot area 400 wide and 300 high, its top left corner at (50, 20): right edge 450, bottom edge 320.</summary>
    private static readonly Rect Area = new(50, 20, 400, 300);

    private static LineSegment? Clip(double x1, double y1, double x2, double y2) =>
        new LineSegment(new Point(x1, y1), new Point(x2, y2)).ClippedTo(Area);

    private static void AssertSegment(LineSegment? actual, double x1, double y1, double x2, double y2)
    {
        Assert.True(actual is not null, "the segment was dropped");
        var (from, to) = actual.Value;
        Assert.Equal(x1, from.X, 9);
        Assert.Equal(y1, from.Y, 9);
        Assert.Equal(x2, to.X, 9);
        Assert.Equal(y2, to.Y, 9);
    }

    // ---- inside ---------------------------------------------------------------------------------------------------------

    /// <summary>A segment inside the area is handed back as it was, bit for bit: no end is recomputed.</summary>
    [Fact]
    public void ASegmentInside_IsHandedBackExactly()
    {
        var inside = new LineSegment(new Point(60.123456789012345, 300.98765432109876), new Point(440.1, 25.000000000000004));

        var clipped = inside.ClippedTo(Area);

        Assert.Equal(inside, clipped);
    }

    /// <summary>A segment whose ends lie on the edges of the area is inside: it is handed back as it was.</summary>
    [Fact]
    public void ASegmentFromEdgeToEdge_IsInside()
    {
        var corners = new LineSegment(new Point(50, 320), new Point(450, 20));

        Assert.Equal(corners, corners.ClippedTo(Area));
    }

    /// <summary>A segment is cut where it crosses the edges of the area, whichever way it is drawn; one that runs along an edge inside it,
    /// or a point inside, is kept whole.</summary>
    [Theory]
    [InlineData(100, 200, 300, 200, 100, 200, 300, 200)] // along an edge, inside it
    [InlineData(100, 100, 100, 100, 100, 100, 100, 100)] // a point (no length) inside
    [InlineData(100, 220, 300, -180, 100, 220, 200, 20)] // leaves through the top: crosses y = 20 half way, at x = 200
    [InlineData(0, 100, 100, 200, 50, 150, 100, 200)] // comes in through the left: crosses x = 50 half way, at y = 150
    [InlineData(0, -30, 500, 470, 50, 20, 350, 320)] // y = x - 30 through the area: enters at the top (50, 20), leaves at the bottom (350, 320)
    [InlineData(500, 470, 0, -30, 350, 320, 50, 20)] // the same drawn the other way: the same cut, its ends swapped
    [InlineData(350, 100, 550, 100, 350, 100, 450, 100)] // leaves through the right edge
    public void ASegment_IsCutAtTheEdgesItCrosses(double x1, double y1, double x2, double y2, double fromX, double fromY, double toX, double toY) =>
        AssertSegment(Clip(x1, y1, x2, y2), fromX, fromY, toX, toY);

    /// <summary>A segment with nothing inside the area is dropped.</summary>
    [Theory]
    [InlineData(100, 0, 300, 10)] // wholly above
    [InlineData(100, 400, 300, 400)] // parallel to an edge, outside it
    [InlineData(550, 100, 500, 150)] // wholly to the right, drawn leftwards
    [InlineData(-100, 100, 0, 150)] // wholly to the left
    [InlineData(-100, 150, 150, -100)] // past a corner: x + y = 50 crosses x = 50 at y = 0 (above) and y = 20 at x = 30 (left)
    [InlineData(0, 70, 70, 0)] // touching only the top left corner (50, 20): x + y = 70
    [InlineData(50, 100, 0, 100)] // starts on an edge and runs away from the area
    [InlineData(10, 100, 10, 100)] // a point (no length) outside
    public void ASegmentWithNothingInside_IsDropped(double x1, double y1, double x2, double y2) => Assert.Null(Clip(x1, y1, x2, y2));

    // ---- the line through two points (axline) ----------------------------------------------------------------------------

    /// <summary>The line through two points runs on both ways to the edges, in the direction from the first point toward the second,
    /// whether the points lie inside the area or outside it.</summary>
    [Theory]
    [InlineData(100, 70, 200, 170, 50, 20, 350, 320)] // y = x - 30: enters the left edge at (50, 20), leaves the bottom at (350, 320)
    [InlineData(200, 170, 100, 70, 350, 320, 50, 20)] // the same, toward the first point
    [InlineData(0, 100, -10, 100, 450, 100, 50, 100)] // both points outside, the line crosses the area
    [InlineData(100, 300, 100, 200, 100, 320, 100, 20)] // upright: edge to edge across the height
    public void TheLineThroughTwoPoints_RunsOnToTheEdges(double x1, double y1, double x2, double y2, double fromX, double fromY, double toX, double toY) =>
        AssertSegment(Area.LineThrough(new Point(x1, y1), new Point(x2, y2)), fromX, fromY, toX, toY);

    /// <summary>A line that misses the area is not drawn, and one point twice has no direction: there is no line, as matplotlib's axline
    /// refuses it.</summary>
    [Theory]
    [InlineData(0, 0, 10, 0)] // misses the area
    [InlineData(100, 100, 100, 100)] // one point twice
    public void TheLineThroughTwoPoints_IsNotDrawn_WhenItMissesTheAreaOrHasNoDirection(double x1, double y1, double x2, double y2) =>
        Assert.Null(Area.LineThrough(new Point(x1, y1), new Point(x2, y2)));
}
