// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Models.Patches;

/// <summary>A circle in data coordinates, matplotlib's <c>Circle</c>: a centre and a radius.</summary>
/// <remarks>The circle is round in data units, so it is drawn as an ellipse wherever the two axes give a unit different lengths, as in
/// matplotlib. Its outline is matplotlib's: the unit circle's eight cubic Bézier curves (26 vertices), scaled and moved, every
/// control point in data units. On a log axis each point moves through the scale on its own: the curves bend and the side towards
/// the smaller values stretches, the shape matplotlib draws there. A radius of 0 or less, or a number that is not finite, draws
/// nothing.</remarks>
/// <example>
/// <code>
/// ax.AddPatch(new Circle(new(3, 2), 1.5) { EdgeColor = Colors.Black, Alpha = 0.6 });
/// </code>
/// </example>
public sealed class Circle : FilledPatch
{
    /// <summary>Creates a circle from its centre and its radius.</summary>
    /// <param name="center">The centre, in data units.</param>
    /// <param name="radius">The radius, in data units.</param>
    public Circle(DataPoint center, double radius)
    {
        Center = center;
        Radius = radius;
    }

    /// <summary>The centre, in data units (matplotlib's <c>xy</c>).</summary>
    public DataPoint Center { get; set; }

    /// <summary>The radius, in data units; 0 or less draws nothing.</summary>
    public double Radius { get; set; }

    /// <inheritdoc />
    internal override IReadOnlyList<PathSegment> Outline() =>
        new EllipticArc(Center, 2 * Radius, 2 * Radius, 0).Place(EllipticArc.UnitCircle);

    /// <inheritdoc />
    internal override PatchDto Describe() => new CircleDto(this);
}
