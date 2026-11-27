// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Maui.Graphics;
using MatPlotLibNet.Rendering;
using MplPoint = MatPlotLibNet.Rendering.Point;

namespace MatPlotLibNet.Maui;

/// <summary>What a path becomes on a MAUI canvas: a <see cref="PathF"/>, built by the call of the same name for each kind of
/// segment. The path builder has lines and curves and no arc, so an arc is followed with the cubic curves
/// <see cref="ArcSegmentExtensions.ToBeziers"/> gives. The one sink a drawn path, a polygon, a polyline and the outline of a clip are
/// built with.</summary>
/// <param name="Path">The path that is built.</param>
internal readonly record struct MauiPathSink(PathF Path) : IPathSink
{
    /// <inheritdoc />
    public void MoveTo(MplPoint point) => Path.MoveTo((float)point.X, (float)point.Y);

    /// <inheritdoc />
    public void LineTo(MplPoint point) => Path.LineTo((float)point.X, (float)point.Y);

    /// <inheritdoc />
    public void CubicTo(MplPoint control1, MplPoint control2, MplPoint end) =>
        Path.CurveTo((float)control1.X, (float)control1.Y, (float)control2.X, (float)control2.Y, (float)end.X, (float)end.Y);

    /// <inheritdoc />
    public void ArcTo(ArcSegment arc)
    {
        foreach (var curve in arc.ToBeziers()) CubicTo(curve.Control1, curve.Control2, curve.End);
    }

    /// <inheritdoc />
    public void Close() => Path.Close();
}
