// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Models.Patches;

/// <summary>A wedge of a disc in data coordinates, matplotlib's <c>Wedge</c>: the slice of the circle about a centre between two
/// angles, or with a <see cref="Width"/> the slice of a ring.</summary>
/// <remarks>
/// <para>The angles are in degrees counter-clockwise from +X, and the wedge runs counter-clockwise from <see cref="Theta1"/> to
/// <see cref="Theta2"/>: a first angle past the second sweeps through 0 (300 to 60 is the third of the disc around +X), and the sweep is
/// unwrapped to less than a whole turn. A sweep of exactly a whole turn (to 1e-12) is the whole disc, drawn from 0 with no edge to the
/// centre; equal angles enclose nothing, and an edge draws the radius at that angle. These are matplotlib's rules.</para>
/// <para>The outline is matplotlib's arc of the circle (<c>Path.arc</c>) as cubic Bézier curves, every control point in data units,
/// then a line to the centre and a close; with a <see cref="Width"/>, the inner ring at <see cref="Radius"/> - <see cref="Width"/> is
/// run backwards in its place, so a whole ring is filled with its hole open. A radius of 0 or less, or a number that is not finite,
/// draws nothing.</para>
/// </remarks>
/// <example>
/// <code>
/// ax.AddPatch(new Wedge(new(0, 0), 2, 30, 120) { Width = 0.5, EdgeColor = Colors.Black });
/// </code>
/// </example>
public sealed class Wedge : FilledPatch
{
    /// <summary>matplotlib's tolerance for a sweep of a whole turn.</summary>
    private const double WholeTurnTolerance = 1e-12;

    /// <summary>Creates the wedge of the disc about <paramref name="center"/> from <paramref name="theta1"/> counter-clockwise to
    /// <paramref name="theta2"/>.</summary>
    /// <param name="center">The centre of the disc, in data units.</param>
    /// <param name="radius">The radius of the disc, in data units.</param>
    /// <param name="theta1">The angle the wedge starts at, in degrees counter-clockwise from +X.</param>
    /// <param name="theta2">The angle the wedge ends at, in degrees counter-clockwise from +X.</param>
    public Wedge(DataPoint center, double radius, double theta1, double theta2)
    {
        Center = center;
        Radius = radius;
        Theta1 = theta1;
        Theta2 = theta2;
    }

    /// <summary>The centre of the disc, in data units (matplotlib's <c>center</c>).</summary>
    public DataPoint Center { get; set; }

    /// <summary>The radius of the disc, in data units (matplotlib's <c>r</c>); 0 or less draws nothing.</summary>
    public double Radius { get; set; }

    /// <summary>The angle the wedge starts at, in degrees counter-clockwise from +X.</summary>
    public double Theta1 { get; set; }

    /// <summary>The angle the wedge ends at, in degrees counter-clockwise from +X; the wedge runs counter-clockwise to it from
    /// <see cref="Theta1"/>.</summary>
    public double Theta2 { get; set; }

    /// <summary>How far the wedge reaches in from its <see cref="Radius"/>, in data units, making it the slice of a ring from
    /// <see cref="Radius"/> - <see cref="Width"/> out to <see cref="Radius"/>. <see langword="null"/> (the default) reaches the
    /// centre.</summary>
    public double? Width { get; set; }

    /// <inheritdoc />
    internal override IReadOnlyList<PathSegment> Outline()
    {
        // matplotlib's Wedge._recompute_path: a sweep of a whole turn is the whole ring from 0, with nothing drawn to the centre or
        // between the rings (a move instead of a line).
        bool whole = Math.Abs(Theta2 - Theta1 - 360) <= WholeTurnTolerance;
        var arc = EllipticArc.UnitArc(whole ? 0 : Theta1, whole ? 360 : Theta2);
        List<PathSegment> unit = [.. EllipticArc.Curves(arc, joined: false)];

        if (Width is double width)
        {
            double radius = Radius;
            Point[] inner = [.. Enumerable.Reverse(arc).Select(p => new Point(p.X * (radius - width) / radius, p.Y * (radius - width) / radius))];
            unit.AddRange(EllipticArc.Curves(inner, joined: !whole));
        }
        else
        {
            var center = new Point(0, 0);
            unit.Add(whole ? new MoveToSegment(center) : new LineToSegment(center));
        }

        unit.Add(new CloseSegment());
        return new EllipticArc(Center, 2 * Radius, 2 * Radius, 0).Place(unit);
    }

    /// <inheritdoc />
    internal override PatchDto Describe() => new WedgeDto(this);
}
