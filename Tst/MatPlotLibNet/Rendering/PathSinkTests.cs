// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Holds the one walk of a path and of a polyline: every target (a Skia path, a MAUI path, the marks a legend keeps clear
/// of) is a sink that says what a move, a line, a curve, an arc and a close become, and none reads the kinds of segment itself.</summary>
public class PathSinkTests
{
    /// <summary>A sink that writes down what it was told, in order.</summary>
    private readonly struct Recorder(List<string> log) : IPathSink
    {
        public void MoveTo(Point point) => log.Add($"M {point.X} {point.Y}");
        public void LineTo(Point point) => log.Add($"L {point.X} {point.Y}");
        public void CubicTo(Point control1, Point control2, Point end) => log.Add($"C {control1.X} {control1.Y} {control2.X} {control2.Y} {end.X} {end.Y}");
        public void ArcTo(ArcSegment arc) => log.Add($"A {arc.Center.X} {arc.Center.Y} {arc.RadiusX} {arc.RadiusY} {arc.StartAngle} {arc.EndAngle}");
        public void Close() => log.Add("Z");
    }

    /// <summary>A segment of a kind the library does not draw: an application's own.</summary>
    private sealed record OwnSegment : PathSegment
    {
        public override string ToSvgPathData() => "";
    }

    private static List<string> Walked(params PathSegment[] segments)
    {
        var log = new List<string>();
        var sink = new Recorder(log);
        segments.WalkInto(ref sink);
        return log;
    }

    private static List<string> Polyline(Point[] points, bool closed)
    {
        var log = new List<string>();
        var sink = new Recorder(log);
        points.WalkPolyline(ref sink, closed);
        return log;
    }

    [Fact]
    public void EveryKindOfSegment_IsToldToTheSink_InTheOrderItIsWritten()
    {
        var log = Walked(
            new MoveToSegment(new Point(1, 2)),
            new LineToSegment(new Point(3, 4)),
            new BezierSegment(new Point(5, 6), new Point(7, 8), new Point(9, 10)),
            new ArcSegment(new Point(11, 12), 13, 14, 15, 16),
            new CloseSegment());

        Assert.Equal(["M 1 2", "L 3 4", "C 5 6 7 8 9 10", "A 11 12 13 14 15 16", "Z"], log);
    }

    [Fact]
    public void ANoPath_TellsTheSinkNothing() => Assert.Empty(Walked());

    [Fact]
    public void ASegmentOfAKindTheLibraryDoesNotDraw_IsLeftOut()
    {
        var log = Walked(new MoveToSegment(new Point(1, 2)), new OwnSegment(), new LineToSegment(new Point(3, 4)));

        Assert.Equal(["M 1 2", "L 3 4"], log);
    }

    [Fact]
    public void AnOpenPolyline_IsAMoveToItsFirstPoint_ThenALineToEachOther()
    {
        Assert.Equal(["M 0 0", "L 1 2", "L 3 4"], Polyline([new Point(0, 0), new Point(1, 2), new Point(3, 4)], closed: false));
    }

    [Fact]
    public void AClosedPolyline_ClosesAfterItsLastPoint()
    {
        Assert.Equal(["M 0 0", "L 1 2", "L 3 4", "Z"], Polyline([new Point(0, 0), new Point(1, 2), new Point(3, 4)], closed: true));
    }

    [Fact]
    public void APolylineOfOnePoint_IsItsMove_AndItsCloseWhenClosed()
    {
        Assert.Equal(["M 5 6"], Polyline([new Point(5, 6)], closed: false));
        Assert.Equal(["M 5 6", "Z"], Polyline([new Point(5, 6)], closed: true));
    }

    [Fact]
    public void APolylineOfNoPoint_TellsTheSinkNothing_ClosedOrNot()
    {
        Assert.Empty(Polyline([], closed: false));
        Assert.Empty(Polyline([], closed: true));
    }

    /// <summary>The sinks of a list and of an array walk the same: the walk takes the list it is given, whatever its kind.</summary>
    [Fact]
    public void APolylineOfAList_WalksAsOneOfAnArray()
    {
        var log = new List<string>();
        var sink = new Recorder(log);
        IReadOnlyList<Point> list = new List<Point> { new(0, 0), new(1, 1) };

        list.WalkPolyline(ref sink, closed: false);

        Assert.Equal(["M 0 0", "L 1 1"], log);
    }
}
