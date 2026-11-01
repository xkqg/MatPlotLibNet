// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Models.Patches;

/// <summary>The one rule the outline of a curved patch (<see cref="Circle"/>, <see cref="Ellipse"/>, <see cref="Wedge"/>,
/// <see cref="Arc"/>) is built by: matplotlib's cubic Bézier curves of the unit circle, the whole circle (<c>Path.unit_circle</c>) or
/// an arc of it (<c>Path.arc</c>), placed on an ellipse in data units by matplotlib's patch transform: scaled to half its
/// <see cref="Width"/> and half its <see cref="Height"/>, turned through its <see cref="Angle"/>, moved to its <see cref="Center"/>.
/// Every point of the outline, a curve's control points included, is a data value, so the outline needs no transform: the automatic
/// range reads where the curves reach, and the drawing maps each point through the axes' transform, which on a log axis bends a
/// curve as matplotlib's does. An <see cref="ArcSegment"/> is in pixels and turns the other way, so none is made.</summary>
/// <remarks>A chain is matplotlib's list of vertices for a run of cubic curves: the point it starts at, then three points per curve
/// (its two control points and its end).</remarks>
/// <param name="Center">The centre of the ellipse, in data units.</param>
/// <param name="Width">The full width of the ellipse before it is turned, in data units.</param>
/// <param name="Height">The full height of the ellipse before it is turned, in data units.</param>
/// <param name="Angle">The angle the ellipse is turned through, in degrees counter-clockwise.</param>
internal readonly record struct EllipticArc(DataPoint Center, double Width, double Height, double Angle)
{
    /// <summary>numpy's <c>deg2rad</c> factor.</summary>
    private const double DegreesToRadians = Math.PI / 180.0;

    private const double HalfPi = Math.PI * 0.5;

    /// <summary>matplotlib's <c>Path.circle</c> constant (Lancaster): how far a control point stands from its end along the tangent,
    /// for a curve of an eighth of the circle.</summary>
    private const double Magic = 0.2652031;

    private static readonly double SqrtHalf = Math.Sqrt(0.5);
    private static readonly double Magic45 = SqrtHalf * Magic;

    /// <summary>matplotlib's unit circle (<c>Path.unit_circle</c>): eight cubic curves, counter-clockwise from the bottom (0, -1), and a
    /// close; 26 vertices in matplotlib's path, the close counted. It is not <c>Path.arc</c> over a whole turn, which has sixteen.</summary>
    internal static IReadOnlyList<PathSegment> UnitCircle { get; } =
    [
        .. Curves(
        [
            new(0.0, -1.0),
            new(Magic, -1.0), new(SqrtHalf - Magic45, -SqrtHalf - Magic45), new(SqrtHalf, -SqrtHalf),
            new(SqrtHalf + Magic45, -SqrtHalf + Magic45), new(1.0, -Magic), new(1.0, 0.0),
            new(1.0, Magic), new(SqrtHalf + Magic45, SqrtHalf - Magic45), new(SqrtHalf, SqrtHalf),
            new(SqrtHalf - Magic45, SqrtHalf + Magic45), new(Magic, 1.0), new(0.0, 1.0),
            new(-Magic, 1.0), new(-SqrtHalf + Magic45, SqrtHalf + Magic45), new(-SqrtHalf, SqrtHalf),
            new(-SqrtHalf - Magic45, SqrtHalf - Magic45), new(-1.0, Magic), new(-1.0, 0.0),
            new(-1.0, -Magic), new(-SqrtHalf - Magic45, -SqrtHalf + Magic45), new(-SqrtHalf, -SqrtHalf),
            new(-SqrtHalf + Magic45, -SqrtHalf - Magic45), new(-Magic, -1.0), new(0.0, -1.0),
        ], joined: false),
        new CloseSegment(),
    ];

    /// <summary>matplotlib's arc of the unit circle (<c>Path.arc</c>) from <paramref name="theta1"/> counter-clockwise to
    /// <paramref name="theta2"/>, as a chain. The sweep is unwrapped to less than a whole turn, so a first angle past the second
    /// sweeps through 0; a whole number of turns (to 1e-12) is the whole circle; equal angles sweep nothing. The arc is cut into 2
    /// to the power of the quarter turns it sweeps, rounded up, curves (Maisonobe's construction): 2 for a quarter turn, 16 for the
    /// whole circle.</summary>
    /// <param name="theta1">The angle it starts at, in degrees counter-clockwise from +X.</param>
    /// <param name="theta2">The angle it ends at, in degrees counter-clockwise from +X.</param>
    /// <returns>The chain: the start, then three points per curve. Its points are not finite when an angle is not.</returns>
    internal static Point[] UnitArc(double theta1, double theta2)
    {
        double turns = (theta2 - theta1) / 360;
        double nearest = Math.Round(turns);   // numpy's rint: a half to the even turn
        bool whole = nearest != 0 && Math.Abs(turns - nearest) <= 1e-12;
        double eta1 = theta1 * DegreesToRadians;
        double eta2 = (whole ? theta1 + 360 : theta2 - 360 * Math.Floor(turns)) * DegreesToRadians;

        // An angle of a size whose difference has lost its digits could ask for more curves than a turn has: 2^16 at most.
        double quarters = Math.Ceiling((eta2 - eta1) / HalfPi);
        int n = quarters > 0 ? 1 << (int)Math.Min(quarters, 16) : 1;

        double step = (eta2 - eta1) / n;
        double t = Math.Tan(0.5 * step);
        double alpha = Math.Sin(step) * (Math.Sqrt(4.0 + 3.0 * t * t) - 1) / 3.0;

        var chain = new Point[3 * n + 1];
        var start = OnCircle(eta1);
        chain[0] = start;
        for (int i = 0; i < n; i++)
        {
            // numpy's linspace: the angles step from eta1 and the last is eta2 itself.
            var end = OnCircle(i + 1 == n ? eta2 : (i + 1) * step + eta1);
            chain[3 * i + 1] = new Point(start.X + alpha * -start.Y, start.Y + alpha * start.X);
            chain[3 * i + 2] = new Point(end.X - alpha * -end.Y, end.Y - alpha * end.X);
            chain[3 * i + 3] = end;
            start = end;
        }

        return chain;

        static Point OnCircle(double eta) => new(Math.Cos(eta), Math.Sin(eta));
    }

    /// <summary>The segments of a chain: a move to its first point (a line when <paramref name="joined"/> to the outline before it),
    /// then a cubic curve for each three points after it.</summary>
    /// <param name="chain">The chain, at least its first point.</param>
    /// <param name="joined">Whether a line joins the chain to the point the outline stands at, instead of a move.</param>
    /// <returns>The segments, in the units of the chain.</returns>
    internal static IEnumerable<PathSegment> Curves(IReadOnlyList<Point> chain, bool joined)
    {
        yield return joined ? new LineToSegment(chain[0]) : new MoveToSegment(chain[0]);
        for (int i = 1; i + 2 < chain.Count; i += 3)
        {
            yield return new BezierSegment(chain[i], chain[i + 1], chain[i + 2]);
        }
    }

    /// <summary>An outline on the unit circle placed on this ellipse, in data units: every point (a curve's control points
    /// included) scaled to half the <see cref="Width"/> and half the <see cref="Height"/>, turned through the <see cref="Angle"/>
    /// and moved to the <see cref="Center"/>, as matplotlib's <c>Affine2D().scale(width / 2, height / 2).rotate_deg(angle)
    /// .translate(center)</c> does.</summary>
    /// <param name="unit">The outline on the unit circle.</param>
    /// <returns>The outline in data units; empty when the ellipse has no size (a width or a height of 0 or less) or a point of the
    /// outline is not a finite number, so the shape draws nothing.</returns>
    internal IReadOnlyList<PathSegment> Place(IEnumerable<PathSegment> unit)
    {
        if (!(Width > 0 && Height > 0))
        {
            return [];
        }

        double radians = Angle * DegreesToRadians;
        double cos = Math.Cos(radians), sin = Math.Sin(radians);
        double halfWidth = Width * 0.5, halfHeight = Height * 0.5;
        double xx = cos * halfWidth, xy = -sin * halfHeight, yx = sin * halfWidth, yy = cos * halfHeight;
        var center = Center;
        bool finite = true;

        var placed = unit.Select(segment => segment switch
        {
            MoveToSegment move => new MoveToSegment(To(move.Point)),
            LineToSegment line => new LineToSegment(To(line.Point)),
            BezierSegment curve => new BezierSegment(To(curve.Control1), To(curve.Control2), To(curve.End)),
            _ => segment,
        }).ToList();

        return finite ? placed : [];

        Point To(Point p)
        {
            var to = new Point(xx * p.X + xy * p.Y + center.DataX, yx * p.X + yy * p.Y + center.DataY);
            finite &= double.IsFinite(to.X) && double.IsFinite(to.Y);
            return to;
        }
    }
}
