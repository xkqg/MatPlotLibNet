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

    /// <summary>A segment that runs along an edge, inside it, is kept whole.</summary>
    [Fact]
    public void ASegmentParallelToAnEdge_Inside_IsKeptWhole() =>
        AssertSegment(Clip(100, 200, 300, 200), 100, 200, 300, 200);

    /// <summary>A segment of no length (a point) inside the area is kept; one outside is dropped.</summary>
    [Fact]
    public void APoint_IsKeptInside_AndDroppedOutside()
    {
        AssertSegment(Clip(100, 100, 100, 100), 100, 100, 100, 100);
        Assert.Null(Clip(10, 100, 10, 100));
    }

    // ---- cut at an edge ---------------------------------------------------------------------------------------------------

    /// <summary>A segment that leaves through the top edge ends where it crosses it.</summary>
    [Fact]
    public void ASegmentLeavingThroughTheTop_EndsOnTheTopEdge() =>
        // From (100, 220) to (300, -180): it crosses y = 20 half way, at x = 200.
        AssertSegment(Clip(100, 220, 300, -180), 100, 220, 200, 20);

    /// <summary>A segment that comes in through the left edge starts where it crosses it.</summary>
    [Fact]
    public void ASegmentEnteringThroughTheLeft_StartsOnTheLeftEdge() =>
        // From (0, 100) to (100, 200): it crosses x = 50 half way, at y = 150.
        AssertSegment(Clip(0, 100, 100, 200), 50, 150, 100, 200);

    /// <summary>A segment that runs through the area and out on both sides is cut on both: through the bottom edge and the
    /// right edge.</summary>
    [Fact]
    public void ASegmentThroughTheArea_IsCutAtBothEdges() =>
        // The line y = x - 30 from (0, -30) to (500, 470): it enters at the top (x = 50 gives y = 20) and leaves at the
        // bottom (y = 320 at x = 350).
        AssertSegment(Clip(0, -30, 500, 470), 50, 20, 350, 320);

    /// <summary>The cut does not depend on the direction the segment is drawn in: the reversed segment is cut at the same
    /// points, its ends swapped.</summary>
    [Fact]
    public void TheCut_DoesNotDependOnTheDirection() =>
        AssertSegment(Clip(500, 470, 0, -30), 350, 320, 50, 20);

    /// <summary>A segment that leaves through the right edge, with its start inside, ends on the right edge.</summary>
    [Fact]
    public void ASegmentLeavingThroughTheRight_EndsOnTheRightEdge() =>
        AssertSegment(Clip(350, 100, 550, 100), 350, 100, 450, 100);

    // ---- outside ----------------------------------------------------------------------------------------------------------

    /// <summary>A segment wholly above the area is dropped.</summary>
    [Fact]
    public void ASegmentAbove_IsDropped() => Assert.Null(Clip(100, 0, 300, 10));

    /// <summary>A segment parallel to an edge, outside it, is dropped.</summary>
    [Fact]
    public void ASegmentParallelToAnEdge_Outside_IsDropped() => Assert.Null(Clip(100, 400, 300, 400));

    /// <summary>A segment wholly to the right of the area, drawn leftwards, is dropped.</summary>
    [Fact]
    public void ASegmentToTheRight_IsDropped() => Assert.Null(Clip(550, 100, 500, 150));

    /// <summary>A segment wholly to the left of the area is dropped.</summary>
    [Fact]
    public void ASegmentToTheLeft_IsDropped() => Assert.Null(Clip(-100, 100, 0, 150));

    /// <summary>A segment that passes outside a corner, crossing the lines of two edges but never the area, is dropped.</summary>
    [Fact]
    public void ASegmentPastACorner_IsDropped() =>
        // The line x + y = 50 runs from (-100, 150) to (150, -100): it crosses the line x = 50 at y = 0 (above the area) and
        // the line y = 20 at x = 30 (left of it).
        Assert.Null(Clip(-100, 150, 150, -100));

    /// <summary>A segment that touches the area in one point only (a corner) has nothing to draw and is dropped.</summary>
    [Fact]
    public void ASegmentTouchingOnlyACorner_IsDropped() =>
        // The line x + y = 70 touches the area only at its top left corner (50, 20).
        Assert.Null(Clip(0, 70, 70, 0));

    /// <summary>A segment that starts on an edge and runs away from the area has nothing inside and is dropped.</summary>
    [Fact]
    public void ASegmentLeavingFromAnEdge_IsDropped() => Assert.Null(Clip(50, 100, 0, 100));
}
