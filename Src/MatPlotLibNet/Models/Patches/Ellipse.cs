// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Models.Patches;

/// <summary>An ellipse in data coordinates, matplotlib's <c>Ellipse</c>: a centre, a full width and a full height, turned about
/// the centre.</summary>
/// <remarks>The outline is matplotlib's: the unit circle's eight cubic Bézier curves, scaled to the ellipse, turned and moved, every
/// control point in data units. On a log axis each point moves through the scale on its own, so the ellipse bends there as it does
/// in matplotlib. An ellipse with a width or a height of 0 or less, or a number that is not finite, has no outline and draws
/// nothing.</remarks>
/// <example>
/// <code>
/// ax.AddPatch(new Ellipse(new(2, 1), 4, 1.5) { Angle = 30, FaceColor = Colors.Tab10Orange });
/// </code>
/// </example>
public sealed class Ellipse : FilledPatch
{
    /// <summary>Creates an ellipse from its centre and its two diameters, not turned.</summary>
    /// <param name="center">The centre, in data units.</param>
    /// <param name="width">The full width along X before the ellipse is turned, in data units (a diameter, not a radius).</param>
    /// <param name="height">The full height along Y before the ellipse is turned, in data units (a diameter, not a radius).</param>
    public Ellipse(DataPoint center, double width, double height)
    {
        Center = center;
        Width = width;
        Height = height;
    }

    /// <summary>The centre, in data units (matplotlib's <c>xy</c>).</summary>
    public DataPoint Center { get; set; }

    /// <summary>The full width along X before the ellipse is turned, in data units; 0 or less draws nothing.</summary>
    public double Width { get; set; }

    /// <summary>The full height along Y before the ellipse is turned, in data units; 0 or less draws nothing.</summary>
    public double Height { get; set; }

    /// <summary>The angle the ellipse is turned through about its <see cref="Center"/>, in degrees, counter-clockwise in data
    /// space. 0 (the default) keeps its width along X.</summary>
    public double Angle { get; set; }

    /// <inheritdoc />
    internal override IReadOnlyList<PathSegment> Outline() => new EllipticArc(Center, Width, Height, Angle).Place(EllipticArc.UnitCircle);

    /// <inheritdoc />
    internal override PatchDto Describe() => new EllipseDto(this);
}
