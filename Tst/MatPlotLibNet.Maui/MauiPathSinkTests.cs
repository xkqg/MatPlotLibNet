// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Maui.Graphics;
using MatPlotLibNet.Maui;
using MatPlotLibNet.Rendering;
using MplPoint = MatPlotLibNet.Rendering.Point;

namespace MatPlotLibNet.Maui.Tests;

/// <summary>Holds what a path becomes on a MAUI canvas: the same operations, and the same points, as the calls the canvas used to
/// be given segment by segment.</summary>
public class MauiPathSinkTests
{
    /// <summary>Every operation of a path with its points, at the full float.</summary>
    private static string Dump(PathF path) => string.Join('\n', Enumerable.Range(0, path.OperationCount).Select(i =>
        $"{path.GetSegmentType(i)} {string.Join(' ', path.GetPointsForSegment(i).Select(p => $"({p.X:R},{p.Y:R})"))}"));

    private static PathF Walked(params PathSegment[] segments)
    {
        var sink = new MauiPathSink(new PathF());
        segments.WalkInto(ref sink);
        return sink.Path;
    }

    [Fact]
    public void AMoveALineACurveAndAClose_AreTheCanvasCallsOfTheSameName()
    {
        var walked = Walked(
            new MoveToSegment(new MplPoint(1.5, 2.5)),
            new LineToSegment(new MplPoint(10, 20)),
            new BezierSegment(new MplPoint(11, 21), new MplPoint(12, 22), new MplPoint(13, 23)),
            new CloseSegment());

        var direct = new PathF();
        direct.MoveTo(1.5f, 2.5f);
        direct.LineTo(10f, 20f);
        direct.CurveTo(11f, 21f, 12f, 22f, 13f, 23f);
        direct.Close();

        Assert.Equal(Dump(direct), Dump(walked));
    }

    /// <summary>The path builder has no arc: an arc is the cubic curves <c>ToBeziers</c> gives, one for each quarter turn.</summary>
    [Fact]
    public void AnArc_IsFollowedWithTheCurvesOfToBeziers_OneForEachQuarterTurn()
    {
        var arc = new ArcSegment(new MplPoint(0, 0), 10, 20, 0, 135);

        var walked = Walked(new MoveToSegment(new MplPoint(10, 0)), arc);

        var direct = new PathF();
        direct.MoveTo(10f, 0f);
        foreach (var curve in arc.ToBeziers())
            direct.CurveTo((float)curve.Control1.X, (float)curve.Control1.Y, (float)curve.Control2.X, (float)curve.Control2.Y, (float)curve.End.X, (float)curve.End.Y);
        Assert.Equal(Dump(direct), Dump(walked));
        Assert.Equal(1 + 2, walked.OperationCount);
        Assert.Equal(PathOperation.Cubic, walked.GetSegmentType(2));
    }

    [Fact]
    public void AnArcThatDoesNotSweep_AddsNothing()
    {
        var walked = Walked(new MoveToSegment(new MplPoint(1, 1)), new ArcSegment(new MplPoint(0, 0), 5, 5, 30, 30));

        Assert.Equal(1, walked.OperationCount);
    }

    [Fact]
    public void ThePath_IsTheOneTheSinkWasGiven()
    {
        var given = new PathF();

        Assert.Same(given, new MauiPathSink(given).Path);
    }
}
