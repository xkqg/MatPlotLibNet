// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Patches;

/// <summary>An arc of an ellipse in data coordinates, matplotlib's <c>Arc</c>: the open curve of the ellipse about a centre from one
/// angle counter-clockwise to another. It has an edge and no face: matplotlib refuses to fill an arc.</summary>
/// <remarks>
/// <para>The angles are in degrees counter-clockwise, measured from the ellipse's own width (so they turn with its
/// <see cref="Angle"/>), and each names the direction from the centre in which the arc starts or ends, as on a circle: on an ellipse
/// that is not a circle they are stretched to the angles of the unit circle that land in those directions before the arc is built
/// (matplotlib's <c>_theta_stretch</c>). A first angle past the second sweeps through 0, and two angles a whole turn apart are the whole
/// ellipse, open. A width or a height of 0, or a number that is not finite, draws nothing; a negative one mirrors the arc, as matplotlib's scale does.</para>
/// <para>The outline is matplotlib's arc of the unit circle (<c>Path.arc</c>) as cubic Bézier curves, placed on the ellipse, every
/// control point in data units, and not closed. The edge is black by default. A <see cref="Patch.Hatch"/> is drawn as matplotlib draws
/// it on an arc: over the region the arc and the chord between its ends enclose, though the arc fills nothing there.</para>
/// </remarks>
/// <example>
/// <code>
/// ax.AddPatch(new Arc(new(0, 0), 4, 2) { Theta1 = 0, Theta2 = 180, EdgeWidth = 2 });
/// </code>
/// </example>
public sealed class Arc : Patch
{
    /// <summary>Creates the whole ellipse about <paramref name="center"/> as an open arc from 0 to 360 degrees, with a black edge.</summary>
    /// <param name="center">The centre of the ellipse, in data units.</param>
    /// <param name="width">The full width of the ellipse along X before it is turned, in data units (a diameter).</param>
    /// <param name="height">The full height of the ellipse along Y before it is turned, in data units (a diameter).</param>
    public Arc(DataPoint center, double width, double height)
    {
        Center = center;
        Width = width;
        Height = height;
        EdgeColor = Colors.Black;
    }

    /// <summary>The centre of the ellipse, in data units (matplotlib's <c>xy</c>).</summary>
    public DataPoint Center { get; set => Change(ref field, value); }

    /// <summary>The full width of the ellipse along X before it is turned, in data units; 0 draws nothing, and a negative width mirrors the arc across its height axis.</summary>
    public double Width { get; set => Change(ref field, value); }

    /// <summary>The full height of the ellipse along Y before it is turned, in data units; 0 draws nothing, and a negative height mirrors the arc across its width axis.</summary>
    public double Height { get; set => Change(ref field, value); }

    /// <summary>The angle the ellipse is turned through about its <see cref="Center"/>, in degrees, counter-clockwise in data space.
    /// 0 (the default) keeps its width along X. <see cref="Theta1"/> and <see cref="Theta2"/> turn with it.</summary>
    public double Angle { get; set => Change(ref field, value); }

    /// <summary>The direction the arc starts in, in degrees counter-clockwise from the ellipse's width. 0 by default.</summary>
    public double Theta1 { get; set => Change(ref field, value); }

    /// <summary>The direction the arc ends in, in degrees counter-clockwise from the ellipse's width; the arc runs counter-clockwise to
    /// it from <see cref="Theta1"/>. 360 by default: the whole ellipse.</summary>
    public double Theta2 { get; set => Change(ref field, value); } = 360;

    /// <summary>None: an arc has no face.</summary>
    internal override Color DrawnFaceColorUnder(Theme theme) => Colors.Transparent;

    /// <inheritdoc />
    internal override IReadOnlyList<PathSegment> Outline()
    {
        // matplotlib's Arc._theta_stretch: on an ellipse that is not a circle each angle names a direction from the centre, and is
        // taken to the angle of the unit circle the ellipse's stretch carries into it; two angles that name one direction (a whole
        // turn) are left as they are.
        bool stretched = Width != Height && !(Theta1 != Theta2 && Turn(Theta1) == Turn(Theta2));
        double theta1 = stretched ? Stretched(Theta1) : Theta1;
        double theta2 = stretched ? Stretched(Theta2) : Theta2;

        return new EllipticArc(Center, Width, Height, Angle).Place(EllipticArc.Curves(EllipticArc.UnitArc(theta1, theta2), joined: false));
    }

    /// <inheritdoc />
    internal override PatchDto Describe() => new ArcDto(this);

    /// <summary>The angle of the unit circle that the ellipse's stretch carries into the direction <paramref name="theta"/>, in
    /// degrees from 0 to 360.</summary>
    private double Stretched(double theta)
    {
        double radians = theta.ToRadians();
        return (Math.Atan2(Width / Height * Math.Sin(radians), Math.Cos(radians)).ToDegrees() + 360) % 360;
    }

    /// <summary>An angle as the part of a turn from 0 to 360 degrees, as Python's <c>%</c> takes it (a negative angle counts back from
    /// 360).</summary>
    private static double Turn(double angle)
    {
        double turn = angle % 360;
        return turn < 0 ? turn + 360 : turn;
    }
}
