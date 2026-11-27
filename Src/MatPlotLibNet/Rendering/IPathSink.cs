// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>Where a path goes once it is read: the one thing a target of a path (a Skia path, a MAUI path, the vertices a legend
/// keeps clear of) says for each kind of segment. A target is a struct that holds what it builds, and it is walked by
/// <see cref="PathSinkExtensions"/>, so the kinds of segment are read in one place and a target is only what its five members
/// become. A real difference between the targets lives in those members: an arc is an exact conic on one canvas, a run of cubic
/// curves on another and a run of sampled points for the marks.</summary>
internal interface IPathSink
{
    /// <summary>Lifts the pen and puts it down at <paramref name="point"/>.</summary>
    /// <param name="point">The start of a subpath.</param>
    void MoveTo(Point point);

    /// <summary>Draws a straight line from the current point to <paramref name="point"/>.</summary>
    /// <param name="point">The end of the line.</param>
    void LineTo(Point point);

    /// <summary>Draws a cubic curve from the current point through two control points to <paramref name="end"/>.</summary>
    /// <param name="control1">The control point that leaves the current point.</param>
    /// <param name="control2">The control point that arrives at <paramref name="end"/>.</param>
    /// <param name="end">The end of the curve.</param>
    void CubicTo(Point control1, Point control2, Point end);

    /// <summary>Draws an elliptical arc from the current point (the arc's start point) to its end angle.</summary>
    /// <param name="arc">The arc, in the convention of <see cref="ArcSegment"/>.</param>
    void ArcTo(ArcSegment arc);

    /// <summary>Closes the current subpath with a line back to its start.</summary>
    void Close();
}
