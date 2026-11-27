// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia;

public sealed partial class SkiaRenderContext
{
    /// <summary>What a raster export does with the straight marks it draws, as matplotlib's Agg backend does it: the ends of a horizontal or
    /// vertical stroke, the corners of a rectangle and the ends of a marker are moved to whole pixels (half a pixel on for an odd width), and
    /// a line or the frame of a rectangle is filled as the rectangle it covers, so each pixel takes the share of the line that falls in it
    /// (Skia covers a stroked path in quarters of a pixel). The one owner of that arithmetic: a render context that has one asks it, one that
    /// has none leaves its marks where they stand and strokes its lines. On a canvas that is no grid of pixels (<see cref="Skia.SkiaRenderContext.AggPixelGrid.DeviceGrid"/>)
    /// a mark is left where it stands and nothing is filled.</summary>
    /// <param name="canvas">The canvas the marks are drawn on, whose matrix is read at every ask: a canvas can be scaled between two.</param>
    internal sealed class AggPixelGrid(SKCanvas canvas)
    {
        /// <summary>The colours and positions of the last dashed line's stops, written again for the next dashed line of the same pattern
        /// length (a shader copies them when it is made).</summary>
        private SKColor[]? _dashStopColors;

        /// <inheritdoc cref="Skia.SkiaRenderContext.AggPixelGrid._dashStopColors"/>
        private float[]? _dashStopPositions;

        /// <summary>The grid of the canvas as it stands now; <see langword="null"/> when it is turned or skewed.</summary>
        private DeviceGrid? Device => DeviceGrid.Of(canvas.TotalMatrix);

        /// <summary>Where Agg's <c>PathSnapper</c> puts a snapped vertex past the whole pixel it rounds to: half a pixel on when the width
        /// it strokes with rounds to an odd number of pixels (<c>(mpl_round_to_int(stroke_width) % 2) ? 0.5 : 0.0</c>), so the stroke
        /// fills whole pixels; none for an even width or no stroke.</summary>
        /// <param name="width">The stroke's width, in device pixels.</param>
        private static double OnGridFor(double width) => DeviceAxis.Rounded(width) % 2 == 1 ? 0.5 : 0.0;

        /// <summary>Which way a run from <paramref name="from"/> to <paramref name="to"/> goes along its direction: +1 when it rises or stays,
        /// -1 when it falls (or an end is missing).</summary>
        private static double RunSign(double from, double to) => to >= from ? 1 : -1;

        /// <summary>A horizontal or vertical stroke with each end moved to a whole pixel and, when the width rounds to an odd number of
        /// pixels, half a pixel on; the stroke itself when it is neither horizontal nor vertical or the canvas has no grid.</summary>
        /// <param name="line">The stroke, in the figure's units.</param>
        /// <param name="thickness">The width it is stroked with, in the figure's units.</param>
        /// <param name="projectingEnds">Whether its two ends carry on by half the width.</param>
        /// <returns>The stroke on the pixel grid, or <paramref name="line"/> itself when it stays where it is.</returns>
        internal PixelSegment OnPixelGrid(PixelSegment line, double thickness, bool projectingEnds)
        {
            if (line.StraightAlongX is not { } alongX || Device is not { } grid) return line;

            double width = thickness * grid.Across(alongX).Scale;
            double onGrid = OnGridFor(width);
            double cap = projectingEnds ? width.Reach : 0;

            // Each end on the grid, in device pixels; then carried on by the cap along the stroke, away from the other end.
            Point Capped(Point device, double by) => Point.Along(alongX, device.CoordinateAlong(alongX) + by, device.CoordinateAcross(alongX));

            Point from = grid.OnGrid(line.From, onGrid), to = grid.OnGrid(line.To, onGrid);
            double sign = RunSign(from.CoordinateAlong(alongX), to.CoordinateAlong(alongX));
            return new PixelSegment(grid.Unit(Capped(from, -(sign * cap))), grid.Unit(Capped(to, sign * cap)));
        }

        /// <summary>A mark of one horizontal or vertical stroke hung from a point, as Agg's <c>draw_markers</c> puts it: the point is moved
        /// half a pixel on and truncated (a whole pixel), and the marker's own path is snapped by its width from there, so both ends stand a
        /// whole number of pixels from that pixel, plus half a pixel for an odd width.</summary>
        /// <param name="anchor">The point the mark hangs from, in the figure's units.</param>
        /// <param name="mark">The stroke, in the figure's units.</param>
        /// <param name="thickness">The width it is stroked with, in the figure's units.</param>
        /// <returns>The stroke to draw; <see langword="null"/> when it is neither horizontal nor vertical or the canvas has no grid.</returns>
        internal PixelSegment? MarkerOnPixelGrid(Point anchor, PixelSegment mark, double thickness)
        {
            if (mark.StraightAlongX is not { } alongX || Device is not { } grid) return null;

            double onGrid = OnGridFor(thickness * grid.Across(alongX).Scale);
            var at = grid.Rounded(anchor);

            double Snapped(double offset) => DeviceAxis.Rounded(offset) + onGrid;
            Point End(Point end)
            {
                var offset = grid.Stretched(new Point(end.X - anchor.X, end.Y - anchor.Y));
                return grid.Unit(new Point(at.X + Snapped(offset.X), at.Y + Snapped(offset.Y)));
            }
            return new PixelSegment(End(mark.From), End(mark.To));
        }

        /// <summary>A rectangle with each corner on the whole pixel nearest to it, half a pixel on for an edge of an odd width, read back into
        /// the figure's units; the rectangle itself when the canvas has no grid. A raster export scales both ways alike, so the edge's width
        /// is read across.</summary>
        /// <param name="area">The rectangle, in the figure's units.</param>
        /// <param name="edgeThickness">The width its edge is drawn with, in the figure's units; 0 when it has none.</param>
        /// <returns>The rectangle on the pixel grid, or <paramref name="area"/> itself when it stays where it is.</returns>
        internal Rect OnPixelGrid(Rect area, double edgeThickness)
        {
            if (Device is not { } grid) return area;

            double onGrid = OnGridFor(edgeThickness * grid.PageScale);
            var topLeft = grid.Snapped(new Point(area.X, area.Y), onGrid);
            var bottomRight = grid.Snapped(new Point(area.X + area.Width, area.Y + area.Height), onGrid);
            return new Rect(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
        }

        /// <summary>The most points of a line Agg's <c>PathSnapper</c> snaps in its automatic mode (<c>total_vertices &gt; 1024</c>: no
        /// snap).</summary>
        private const int MostSnappedPoints = 1024;

        /// <summary>How far apart, in device pixels, the ends of a segment may stand across it for the segment to count as horizontal or
        /// vertical (<c>PathSnapper::should_snap</c>: <c>fabs(x0 - x1) &gt;= 1e-4 &amp;&amp; fabs(y0 - y1) &gt;= 1e-4</c> refuses).</summary>
        private const double StraightWithin = 1e-4;

        /// <summary>A line of horizontal and vertical segments as Agg snaps a <c>Line2D</c> in its automatic mode: when it has at most 1024
        /// points and every segment it draws (both ends drawable) is horizontal or vertical in device pixels, each point moved to a whole
        /// pixel, half a pixel on for a width that rounds to an odd number of pixels; a point that cannot be drawn stays one. Any other line,
        /// or a canvas with no grid, is the line itself.</summary>
        /// <param name="line">The line's points, in the figure's units.</param>
        /// <param name="thickness">The width it is stroked with, in the figure's units.</param>
        /// <returns>The points on the pixel grid, or <paramref name="line"/> itself when the line stays where it is.</returns>
        internal IReadOnlyList<Point> OnPixelGrid(IReadOnlyList<Point> line, double thickness)
        {
            if (line.Count > MostSnappedPoints || Device is not { } grid) return line;

            for (int i = 1; i < line.Count; i++)
            {
                Point from = line[i - 1], to = line[i];
                if (from.CanDrawLineTo(to) && grid.Stretched(new Point(to.X - from.X, to.Y - from.Y)) is var run
                    && Math.Abs(run.X) >= StraightWithin && Math.Abs(run.Y) >= StraightWithin)
                    return line;
            }

            double onGrid = OnGridFor(thickness * grid.PageScale);
            var snapped = new Point[line.Count];
            for (int i = 0; i < snapped.Length; i++)
                snapped[i] = grid.Snapped(line[i], onGrid);
            return snapped;
        }

        /// <summary>Fills a horizontal or vertical line as the rectangle it covers, in its colour when it is solid and through its
        /// <see cref="Skia.SkiaRenderContext.AggPixelGrid.DashCoverage"/> when it is dashed. The ends are butt ends.</summary>
        /// <param name="p1">One end of the line.</param>
        /// <param name="p2">The other end.</param>
        /// <param name="color">The colour, with the opacity of the surface applied.</param>
        /// <param name="thickness">The width of the line.</param>
        /// <param name="dashes">The dashes and gaps along it; empty for a solid line.</param>
        /// <returns>Whether it was filled; <see langword="false"/> for a dashed line its dashes cannot be painted along (a gap under a
        /// pixel, a turned canvas, no finite length), which is then stroked.</returns>
        internal bool TryFillLine(Point p1, Point p2, SKColor color, double thickness, ScaledDashPattern dashes)
        {
            // A point runs neither way and is filled as a line along X, a rectangle of no length.
            bool alongX = new PixelSegment(p1, p2).StraightAlongX ?? true;
            double from = p1.CoordinateAlong(alongX), to = p2.CoordinateAlong(alongX);
            double across = p1.CoordinateAcross(alongX), half = thickness.Reach;
            float low = (float)Math.Min(from, to), high = (float)Math.Max(from, to);
            // The rectangle the line covers: from its low end to its high end along it, half its width either side across.
            Point start = Point.Along(alongX, low, across - half), end = Point.Along(alongX, high, across + half);
            var area = new SKRect((float)start.X, (float)start.Y, (float)end.X, (float)end.Y);

            if (dashes.Count == 0)
            {
                using var fill = new SKPaint { Color = color, Style = SKPaintStyle.Fill, IsAntialias = true };
                canvas.DrawRect(area, fill);
                return true;
            }

            using var shader = DashCoverage(dashes, alongX, from, to, across, color);
            if (shader is null) return false;
            using var paint = new SKPaint { Shader = shader, IsAntialias = true };
            canvas.DrawRect(area, paint);
            return true;
        }

        /// <summary>The paint of a dashed line along its rectangle: the line's colour at the share of each pixel its dashes cover along
        /// the line, so that, with the rectangle covering each pixel by its share across, every pixel takes the area of the dashes in it.
        /// The pattern runs from the line's start and repeats. A pixel centred at u is covered by a dash [s, e] by the overlap of
        /// [u - ½, u + ½] with it: 0 up to s - ½, rising to the dash's length (at most 1) and level, back to 0 at e + ½; a gradient between
        /// those stops, read at the pixel's centre, is that overlap.</summary>
        /// <returns>The shader; <see langword="null"/> when a gap is narrower than a pixel (two dashes would share one), the canvas is
        /// turned or the line has no finite length.</returns>
        private SKShader? DashCoverage(ScaledDashPattern dashes, bool alongX, double from, double to, double across, SKColor on)
        {
            if (Device is not { } grid || !double.IsFinite(to - from)) return null;

            double pixel = 1 / grid.Along(alongX).Scale;
            double period = 0;
            for (int i = 0; i < dashes.Count; i++)
            {
                if (i % 2 == 1 && dashes[i] < pixel) return null;
                period += dashes[i];
            }

            // The pattern is laid from the middle of its last gap, so no dash's slope crosses either end of it. The stops are written into
            // arrays kept for the next dashed line, since the shader copies them.
            double lead = dashes[dashes.Count - 1] / 2;
            int count = 2 * dashes.Count + 2;
            if (_dashStopColors?.Length != count)
            {
                _dashStopColors = new SKColor[count];
                _dashStopPositions = new float[count];
            }
            var colors = _dashStopColors;
            var positions = _dashStopPositions!;
            int n = 0;
            Stop(0, 0);
            double start = lead;
            for (int i = 0; i < dashes.Count; i += 2)
            {
                double end = start + dashes[i];
                float share = (float)Math.Min(dashes[i] / pixel, 1);
                Stop(start - pixel / 2, 0);
                Stop(Math.Min(start, end - pixel) + pixel / 2, share);
                Stop(Math.Max(start, end - pixel) + pixel / 2, share);
                Stop(end + pixel / 2, 0);
                start = end + dashes[i + 1];
            }
            Stop(period, 0);

            double direction = RunSign(from, to);
            double begin = from - direction * lead, finish = begin + direction * period;
            Point first = Point.Along(alongX, begin, across), last = Point.Along(alongX, finish, across);
            return SKShader.CreateLinearGradient(new SKPoint((float)first.X, (float)first.Y), new SKPoint((float)last.X, (float)last.Y),
                colors, positions, SKShaderTileMode.Repeat);

            void Stop(double along, float share)
            {
                colors[n] = on.WithAlpha(on.Alpha.Scaled(share));
                positions[n++] = (float)(along / period);
            }
        }

        /// <summary>Fills the frame an edge of <paramref name="thickness"/> covers round <paramref name="area"/>, centred on its sides, as four
        /// rectangles that do not overlap (a rectangle is covered by the share of each pixel it spans, as a line is by
        /// <see cref="Skia.SkiaRenderContext.AggPixelGrid.TryFillLine"/>): the top and bottom across the whole width, the left and right between them. An edge as wide as the
        /// rectangle is thick, or wider, covers all of it: one rectangle.</summary>
        /// <param name="area">The rectangle, in the figure's units, already on the grid.</param>
        /// <param name="color">The colour of the edge, with the opacity of the surface applied.</param>
        /// <param name="thickness">The width of the edge.</param>
        internal void FillFrame(Rect area, SKColor color, double thickness)
        {
            double half = thickness.Reach;
            float left = (float)(area.X - half), top = (float)(area.Y - half);
            float right = (float)(area.X + area.Width + half), bottom = (float)(area.Y + area.Height + half);
            float innerLeft = (float)(area.X + half), innerTop = (float)(area.Y + half);
            float innerRight = (float)(area.X + area.Width - half), innerBottom = (float)(area.Y + area.Height - half);

            using var paint = new SKPaint { Color = color, Style = SKPaintStyle.Fill, IsAntialias = true };
            if (innerRight <= innerLeft || innerBottom <= innerTop)
            {
                canvas.DrawRect(new SKRect(left, top, right, bottom), paint);
                return;
            }

            canvas.DrawRect(new SKRect(left, top, right, innerTop), paint);
            canvas.DrawRect(new SKRect(left, innerBottom, right, bottom), paint);
            canvas.DrawRect(new SKRect(left, innerTop, innerLeft, innerBottom), paint);
            canvas.DrawRect(new SKRect(innerRight, innerTop, right, innerBottom), paint);
        }

        /// <summary>A canvas whose matrix only scales (up the page and across, by a positive amount) and moves: the pixel a unit lands on is
        /// read off it, put on the grid of whole pixels and read back, one <see cref="Skia.SkiaRenderContext.DeviceAxis"/> per direction, held as one pair a direction
        /// picks its member of. A canvas that is turned, skewed, mirrored or in perspective has no such grid (<see cref="Skia.SkiaRenderContext.AggPixelGrid.DeviceGrid.Of"/> answers
        /// <see langword="null"/>), and nothing that rests on one is done for it.</summary>
        internal readonly struct DeviceGrid
        {
            private readonly AxisPair<DeviceAxis> _axes;

            private DeviceGrid(AxisPair<DeviceAxis> axes) => _axes = axes;

            /// <summary>The grid <paramref name="matrix"/> makes, or <see langword="null"/> when it does more than scale and move.</summary>
            /// <param name="matrix">The total matrix of a canvas.</param>
            /// <returns>The grid of the matrix, or <see langword="null"/> when it does more than scale and move.</returns>
            internal static DeviceGrid? Of(SKMatrix matrix) =>
                matrix is { SkewX: 0, SkewY: 0, Persp0: 0, Persp1: 0, ScaleX: > 0, ScaleY: > 0 }
                    ? new DeviceGrid(new(new DeviceAxis(matrix.ScaleX, matrix.TransX), new DeviceAxis(matrix.ScaleY, matrix.TransY)))
                    : null;

            /// <summary>The grid along a direction: across the page along X, down it along Y.</summary>
            /// <param name="alongX">Along X when <see langword="true"/>.</param>
            /// <returns>The grid along that direction.</returns>
            internal DeviceAxis Along(bool alongX) => _axes.Along(alongX);

            /// <summary>The grid across a direction: down the page across one along X, across it across one along Y (how wide a stroke along
            /// the direction is drawn).</summary>
            /// <param name="alongX">Whether the direction runs along X.</param>
            /// <returns>The grid across that direction.</returns>
            internal DeviceAxis Across(bool alongX) => _axes.Along(!alongX);

            /// <summary>The device pixels of one unit across the page: a raster export scales both ways alike, so a width that runs no way of
            /// its own (the edge of a rectangle, a line of many segments) is read in device pixels with it.</summary>
            internal double PageScale => _axes.X.Scale;

            /// <summary>A distance in the figure's units in device pixels, each coordinate by its own direction's scale (no move: the device
            /// size of a step, not the pixel it lands on).</summary>
            /// <param name="delta">The distance, in the figure's units.</param>
            /// <returns>The distance, in device pixels.</returns>
            internal Point Stretched(Point delta) => new(delta.X * _axes.X.Scale, delta.Y * _axes.Y.Scale);

            /// <summary>The device pixel a point in the figure's units lands on.</summary>
            /// <param name="unit">The point, in the figure's units.</param>
            /// <returns>The point, in device pixels.</returns>
            internal Point Device(Point unit) => new(_axes.X.Device(unit.X), _axes.Y.Device(unit.Y));

            /// <summary>The point in the figure's units that lands on a device pixel.</summary>
            /// <param name="device">The point, in device pixels.</param>
            /// <returns>The point, in the figure's units.</returns>
            internal Point Unit(Point device) => new(_axes.X.Unit(device.X), _axes.Y.Unit(device.Y));

            /// <summary>The whole device pixel a point lands nearest to (<see cref="Skia.SkiaRenderContext.DeviceAxis.Rounded"/>), in device pixels.</summary>
            /// <param name="unit">The point, in the figure's units.</param>
            /// <returns>The pixel.</returns>
            internal Point Rounded(Point unit) => new(DeviceAxis.Rounded(_axes.X.Device(unit.X)), DeviceAxis.Rounded(_axes.Y.Device(unit.Y)));

            /// <summary>The whole device pixel a point lands nearest to, moved <paramref name="onGrid"/> on (<see cref="Skia.SkiaRenderContext.DeviceAxis.OnGrid"/>), in
            /// device pixels.</summary>
            /// <param name="unit">The point, in the figure's units.</param>
            /// <param name="onGrid">How far past the whole pixel, in device pixels (half a pixel for an odd stroke width, else none).</param>
            /// <returns>The point on the grid, in device pixels.</returns>
            internal Point OnGrid(Point unit, double onGrid) => new(_axes.X.OnGrid(unit.X, onGrid), _axes.Y.OnGrid(unit.Y, onGrid));

            /// <summary>A point put on the grid (<see cref="Skia.SkiaRenderContext.AggPixelGrid.DeviceGrid.OnGrid"/>) and read back into the figure's units.</summary>
            /// <param name="unit">The point, in the figure's units.</param>
            /// <param name="onGrid">How far past the whole pixel, in device pixels.</param>
            /// <returns>The point on the grid, in the figure's units.</returns>
            internal Point Snapped(Point unit, double onGrid) => new(_axes.X.Snapped(unit.X, onGrid), _axes.Y.Snapped(unit.Y, onGrid));
        }
    }
}
