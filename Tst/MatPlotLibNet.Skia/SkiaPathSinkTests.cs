// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>Holds what a path becomes on a Skia canvas: the same verbs and the same points, bit for bit, as the Skia calls the
/// canvas used to be given segment by segment, so no raster export moves by a pixel.</summary>
public class SkiaPathSinkTests
{
    /// <summary>Every verb of a path with its points and, for a conic, its weight: all that a path is, at the full float.</summary>
    private static string Dump(SKPath path)
    {
        var lines = new List<string>();
        using var iterator = path.CreateRawIterator();
        var points = new SKPoint[4];
        SKPathVerb verb;
        while ((verb = iterator.Next(points)) != SKPathVerb.Done)
        {
            int count = verb switch { SKPathVerb.Move => 1, SKPathVerb.Line => 2, SKPathVerb.Quad => 3, SKPathVerb.Conic => 3, SKPathVerb.Cubic => 4, _ => 0 };
            string weight = verb == SKPathVerb.Conic ? $" w={iterator.ConicWeight():R}" : "";
            lines.Add($"{verb} {string.Join(' ', points.Take(count).Select(p => $"({p.X:R},{p.Y:R})"))}{weight}");
        }
        return string.Join('\n', lines);
    }

    private static SKPath Walked(params PathSegment[] segments)
    {
        var sink = new SkiaPathSink(new SKPath());
        segments.WalkInto(ref sink);
        return sink.Path;
    }

    [Fact]
    public void AMoveALineACurveAndAClose_AreTheSkiaCallsOfTheSameName()
    {
        using var walked = Walked(
            new MoveToSegment(new Point(1.5, 2.5)),
            new LineToSegment(new Point(10, 20)),
            new BezierSegment(new Point(11, 21), new Point(12, 22), new Point(13, 23)),
            new CloseSegment());

        using var direct = new SKPath();
        direct.MoveTo(1.5f, 2.5f);
        direct.LineTo(10f, 20f);
        direct.CubicTo(11f, 21f, 12f, 22f, 13f, 23f);
        direct.Close();

        Assert.Equal(Dump(direct), Dump(walked));
        Assert.Contains("Cubic", Dump(walked));
        Assert.Contains("Close", Dump(walked));
    }

    /// <summary>An arc is Skia's own, an exact conic about the ellipse (the SVG <c>A</c> command draws an exact ellipse too), not
    /// cubic curves that only follow it.</summary>
    [Fact]
    public void AnArc_IsSkiasOwnArcOnTheEllipse_AConicAndNotCubics()
    {
        using var walked = Walked(new MoveToSegment(new Point(110, 100)), new ArcSegment(new Point(100, 100), 10, 20, 0, 90));

        using var direct = new SKPath();
        direct.MoveTo(110f, 100f);
        direct.ArcTo(new SKRect(90f, 80f, 110f, 120f), 0f, 90f, false);

        Assert.Equal(Dump(direct), Dump(walked));
        Assert.Contains("Conic", Dump(walked));
        Assert.DoesNotContain("Cubic", Dump(walked));
    }

    [Fact]
    public void AnArcThatStartsAwayFromThePen_IsReachedByALine_AsSkiaDoesWithNoForcedMove()
    {
        using var walked = Walked(new MoveToSegment(new Point(0, 0)), new ArcSegment(new Point(100, 100), 10, 10, 0, 90));

        Assert.StartsWith("Move (0,0)\nLine (0,0) (110,100)", Dump(walked));
    }

    [Fact]
    public void AnArcThatSweepsBackwards_IsASweepBelowZero()
    {
        using var walked = Walked(new MoveToSegment(new Point(100, 110)), new ArcSegment(new Point(100, 100), 10, 10, 90, 0));

        using var direct = new SKPath();
        direct.MoveTo(100f, 110f);
        direct.ArcTo(new SKRect(90f, 90f, 110f, 110f), 90f, -90f, false);

        Assert.Equal(Dump(direct), Dump(walked));
    }

    [Fact]
    public void ThePath_IsTheOneTheSinkWasGiven()
    {
        using var given = new SKPath();
        var sink = new SkiaPathSink(given);

        Assert.Same(given, sink.Path);
    }
}
