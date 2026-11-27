// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.Models.Patches;

/// <summary>The point a <see cref="Rectangle"/> is turned about, matplotlib's <c>rotation_point</c>: its anchor corner
/// (<see cref="Anchor"/>, matplotlib's <c>'xy'</c>, the default), its centre (<see cref="Center"/>, <c>'center'</c>) or a point
/// of the caller's in data units (<see cref="At"/>, an <c>(x, y)</c> pair). A rectangle that is not turned looks the same
/// whichever of them is set.</summary>
/// <example>
/// <code>
/// ax.AddPatch(new Rectangle(new(1, 1), 2, 1) { Angle = 30, RotationPoint = RotationPoint.Center });
/// ax.AddPatch(new Rectangle(new(1, 1), 2, 1) { Angle = 30, RotationPoint = RotationPoint.At(new(3, 0)) });
/// </code>
/// </example>
public readonly record struct RotationPoint
{
    private RotationPoint(bool isCenter, DataPoint? point)
    {
        IsCenter = isCenter;
        Point = point;
    }

    /// <summary>The corner the rectangle is measured from, matplotlib's <c>'xy'</c>. A default <see cref="RotationPoint"/> is this
    /// one.</summary>
    public static RotationPoint Anchor => default;

    /// <summary>The middle of the rectangle's four corners, matplotlib's <c>'center'</c>: half the width and half the height from
    /// the anchor, whatever their signs.</summary>
    public static RotationPoint Center => new(isCenter: true, point: null);

    /// <summary>A point of the caller's, in data units.</summary>
    /// <param name="point">The point the rectangle is turned about.</param>
    /// <returns>The point.</returns>
    public static RotationPoint At(DataPoint point) => new(isCenter: false, point);

    /// <summary>Whether this is the centre of the rectangle.</summary>
    public bool IsCenter { get; }

    /// <summary>The point given to <see cref="At"/>, <see langword="null"/> for the anchor and for the centre.</summary>
    public DataPoint? Point { get; }

    /// <summary>Where this point lies from the anchor of a rectangle of this size, in data units: nowhere for the anchor, half the
    /// size for the centre, the difference of the two points for a point given.</summary>
    /// <param name="anchor">The anchor corner of the rectangle.</param>
    /// <param name="width">The width of the rectangle, in data units.</param>
    /// <param name="height">The height of the rectangle, in data units.</param>
    /// <returns>The offset from the anchor, in data units.</returns>
    internal DataPoint OffsetFrom(DataPoint anchor, double width, double height) =>
        IsCenter ? new(width / 2, height / 2)
        : Point is { } given ? new(given.DataX - anchor.DataX, given.DataY - anchor.DataY)
        : default;
}
