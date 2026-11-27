// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Rendering;

/// <summary>Transforms polar coordinates (r, theta) to pixel coordinates within a circular plot area.</summary>
public sealed class PolarTransform
{
    private readonly double _cx, _cy, _maxRadius, _rMax, _rOrigin, _innerShare;

    /// <summary>Creates a polar transform for the given plot bounds and data range: radius 0 at the centre, <paramref name="rMax"/>
    /// on the edge.</summary>
    /// <param name="plotBounds">The rectangle the polar plot is drawn in, in pixels.</param>
    /// <param name="rMax">The radius at the edge; a value of 0 or below is read as 1.</param>
    public PolarTransform(Rect plotBounds, double rMax) : this(plotBounds, 0, rMax > 0 ? rMax : 1, 0) { }

    /// <summary>Creates a polar transform whose radii run from <paramref name="rMin"/> to <paramref name="rMax"/>, with
    /// <paramref name="rOrigin"/> at the centre, as matplotlib's polar axes maps them (<c>set_rmin</c>, <c>set_rmax</c>,
    /// <c>set_rorigin</c>): a radius stands <c>(r - rOrigin) / (rMax - rOrigin)</c> of the way from the centre to the edge, so a
    /// radius below zero is placed when the scale reaches it, an origin below <paramref name="rMin"/> leaves a hole at the centre,
    /// and a <paramref name="rMax"/> below <paramref name="rOrigin"/> runs the scale from the outside in.</summary>
    /// <param name="plotBounds">The plot area; the circle fills the square at its centre.</param>
    /// <param name="rMin">The radius at the low end of the scale, at the edge of the hole when there is one.</param>
    /// <param name="rMax">The radius on the edge of the circle.</param>
    /// <param name="rOrigin">The radius at the centre; matplotlib's default is <paramref name="rMin"/>. An origin at
    /// <paramref name="rMax"/> leaves the scale no length, and <paramref name="rMax"/> is then taken a unit further out.</param>
    public PolarTransform(Rect plotBounds, double rMin, double rMax, double rOrigin)
    {
        var frame = RoundFrame.In(plotBounds);
        _cx = frame.Centre.X;
        _cy = frame.Centre.Y;
        _maxRadius = frame.Radius;
        _rMax = rMax != rOrigin ? rMax : rOrigin + 1;
        _rOrigin = rOrigin;
        _innerShare = Math.Max(ShareOf(rMin), 0);
    }

    /// <summary>The transform a polar series is drawn with: over the ends and from the origin of the axes' radial scale when the
    /// axes handed one over, otherwise over the scale the series makes alone under the theme.</summary>
    /// <param name="context">The context the series is drawn in.</param>
    /// <param name="series">The series that is drawn.</param>
    internal static PolarTransform For(SeriesRenderContext context, Models.Series.ISeries series)
    {
        var scale = context.RadialScale ?? RadialScale.Alone(series, context.Theme);
        return new(context.Area.PlotBounds, scale.Min, scale.Extent, scale.Origin);
    }

    /// <summary>Gets the center X coordinate in pixels.</summary>
    public double CenterX => _cx;

    /// <summary>Gets the center Y coordinate in pixels.</summary>
    public double CenterY => _cy;

    /// <summary>Gets the maximum pixel radius: half the side of the square, so the circle fills it, as matplotlib's outermost radius
    /// stands on the edge of the polar axes' box. The layout gives a polar axes the centred square of its cell;
    /// the spoke labels and the title stand outside it, in the cell's margins.</summary>
    public double MaxRadius => _maxRadius;

    /// <summary>The pixel radius of the hole at the centre: where the low end of the scale stands when the origin lies below it,
    /// 0 when the origin is the low end.</summary>
    internal double InnerRadius => _maxRadius * _innerShare;

    /// <summary>Where the marks of the axes are cut off, as matplotlib clips them to the axes patch (a wedge of a full turn): the
    /// disc of the outer ring, or, with a hole, the ring between the hole and the outer ring (<see cref="AxesOutline"/>), with the
    /// square round it.</summary>
    internal ClipRegion Clip => new(
        new Rect(_cx - _maxRadius, _cy - _maxRadius, 2 * _maxRadius, 2 * _maxRadius), AxesOutline());

    /// <summary>The outline of the axes: the disc of the outer ring, two half turns (an SVG arc cannot turn a whole circle); with a
    /// hole at the centre, the outer circle one way round and the inner one the other, so the nonzero rule leaves the hole out.</summary>
    /// <returns>The segments of the outline.</returns>
    internal PathSegment[] AxesOutline()
    {
        var centre = new Point(_cx, _cy);
        double outer = _maxRadius, hole = InnerRadius;
        PathSegment[] disc =
        [
            new MoveToSegment(new Point(_cx + outer, _cy)),
            new ArcSegment(centre, outer, outer, 0, 180),
            new ArcSegment(centre, outer, outer, 180, 360),
            new CloseSegment(),
        ];
        if (hole <= 0) return disc;

        return
        [
            .. disc,
            new MoveToSegment(new Point(_cx + hole, _cy)),
            new ArcSegment(centre, hole, hole, 360, 180),
            new ArcSegment(centre, hole, hole, 180, 0),
            new CloseSegment(),
        ];
    }

    /// <summary>How far from the centre <paramref name="r"/> stands, in pixels, unclamped: the radius a ring of that value is
    /// drawn at.</summary>
    /// <param name="r">A radius inside the scale.</param>
    /// <returns>The distance from the centre, in pixels.</returns>
    internal double PixelRadiusAt(double r) => _maxRadius * ShareOf(r);

    /// <summary>Converts polar coordinates to pixel coordinates, as matplotlib's polar transform places them: a finite radius beyond
    /// rMax stands past the edge, and one between the origin and rMin inside the hole, on the same scale, where the axes clips it.
    /// A radius or an angle that cannot be placed (missing, or infinite) and a radius on the far side of the
    /// origin, which reaches nowhere from the centre, map to a non-finite pixel, which no renderer draws, as a cartesian coordinate
    /// that cannot be placed does.</summary>
    /// <param name="r">Radial distance (data units).</param>
    /// <param name="theta">Angle in radians (0 = right, counter-clockwise).</param>
    /// <returns>The pixel of the position.</returns>
    public Point PolarToPixel(double r, double theta) =>
        new Point(_cx, _cy).AtAngleCounterClockwise(PixelRadiusOf(r), theta);

    /// <summary>The slice of the plot between two radii and two angles (radians, counter-clockwise from the right), in pixels: a
    /// polar heat map's cell. A radius that cannot be placed (<see cref="PolarToPixel"/>) leaves the wedge's corners where no
    /// renderer draws them.</summary>
    /// <param name="innerR">The radius of the inner arc, in data units.</param>
    /// <param name="outerR">The radius of the outer arc, in data units.</param>
    /// <param name="startRadians">The angle the slice starts at.</param>
    /// <param name="endRadians">The angle the slice ends at.</param>
    /// <returns>The wedge.</returns>
    internal RingWedge WedgeOf(double innerR, double outerR, double startRadians, double endRadians) =>
        RingWedge.CounterClockwise(new Point(_cx, _cy), PixelRadiusOf(innerR), PixelRadiusOf(outerR), startRadians, endRadians);

    /// <summary>How far from the centre a radius stands, in pixels: its share of the way from the origin to the edge of the scale
    /// of the outer ring, or not a number for a radius that cannot be placed or lies on the far side of the origin.</summary>
    private double PixelRadiusOf(double r)
    {
        double share = ShareOf(r);
        double norm = r.IsDrawable() && share >= 0 ? share : double.NaN;
        return _maxRadius * norm;
    }

    /// <summary>The share of the way from the origin to the edge of the scale <paramref name="r"/> stands at, unclamped: the one
    /// place the transform reads a radius against its scale, so a ring, the hole and the data at one radius stand at one distance.</summary>
    /// <param name="r">A radius, in data units.</param>
    /// <returns>The share: 0 at the origin, 1 on the edge.</returns>
    private double ShareOf(double r) => (r - _rOrigin) / (_rMax - _rOrigin);
}
