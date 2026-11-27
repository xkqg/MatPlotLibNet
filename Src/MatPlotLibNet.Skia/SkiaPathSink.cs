// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using SkiaSharp;

namespace MatPlotLibNet.Skia;

/// <summary>What a path becomes on a Skia canvas: a Skia path, built by the call of the same name for each kind of segment. An arc
/// is Skia's own, an exact conic about the ellipse (as the SVG <c>A</c> command is an exact ellipse), with no curves to follow it by.
/// The one sink a drawn path, a polygon, a polyline and the outline of a clip are built with.</summary>
/// <param name="Path">The path that is built; the caller owns it and disposes it.</param>
internal readonly record struct SkiaPathSink(SKPath Path) : IPathSink
{
    /// <inheritdoc />
    public void MoveTo(Point point) => Path.MoveTo((float)point.X, (float)point.Y);

    /// <inheritdoc />
    public void LineTo(Point point) => Path.LineTo((float)point.X, (float)point.Y);

    /// <inheritdoc />
    public void CubicTo(Point control1, Point control2, Point end) =>
        Path.CubicTo((float)control1.X, (float)control1.Y, (float)control2.X, (float)control2.Y, (float)end.X, (float)end.Y);

    /// <inheritdoc />
    /// <remarks>Without a forced move, so an arc that starts away from the pen is reached by a line, as Skia does.</remarks>
    public void ArcTo(ArcSegment arc) =>
        Path.ArcTo(
            new SKRect((float)(arc.Center.X - arc.RadiusX), (float)(arc.Center.Y - arc.RadiusY),
                       (float)(arc.Center.X + arc.RadiusX), (float)(arc.Center.Y + arc.RadiusY)),
            (float)arc.StartAngle, (float)(arc.EndAngle - arc.StartAngle), false);

    /// <inheritdoc />
    public void Close() => Path.Close();
}
