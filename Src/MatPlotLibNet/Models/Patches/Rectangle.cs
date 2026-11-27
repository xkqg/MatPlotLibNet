// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Models.Patches;

/// <summary>A rectangle in data coordinates, matplotlib's <c>Rectangle</c>: a corner, a width and a height, turned about that
/// corner.</summary>
/// <remarks>The corners run from <see cref="Anchor"/> along the width, then the height, then back: anchor, anchor + (width, 0),
/// anchor + (width, height), anchor + (0, height), counter-clockwise when the width and the height have the same sign. A
/// negative width extends the rectangle left of the anchor and a negative height below it, as in matplotlib. It is turned about
/// its anchor unless <see cref="RotationPoint"/> says its centre or a point.</remarks>
/// <example>
/// <code>
/// ax.AddPatch(new Rectangle(new(1, 1), 2, 1) { Angle = 30, EdgeColor = Colors.Black });
/// </code>
/// </example>
public sealed class Rectangle : FilledPatch
{
    /// <summary>Creates a rectangle from a corner and its size, not turned.</summary>
    /// <param name="anchor">The corner the rectangle is measured from, in data units: its lower left when the width and the
    /// height are positive.</param>
    /// <param name="width">The width along X, in data units; negative extends it left of the anchor.</param>
    /// <param name="height">The height along Y, in data units; negative extends it below the anchor.</param>
    public Rectangle(DataPoint anchor, double width, double height)
    {
        Anchor = anchor;
        Width = width;
        Height = height;
    }

    /// <summary>The corner the rectangle is measured from and turned about, in data units (matplotlib's <c>xy</c>).</summary>
    public DataPoint Anchor { get; set => Change(ref field, value); }

    /// <summary>The width along X, in data units, before the rectangle is turned; negative extends it left of the anchor.</summary>
    public double Width { get; set => Change(ref field, value); }

    /// <summary>The height along Y, in data units, before the rectangle is turned; negative extends it below the anchor.</summary>
    public double Height { get; set => Change(ref field, value); }

    /// <summary>The angle the rectangle is turned through about its <see cref="RotationPoint"/>, in degrees, counter-clockwise in
    /// data space. 0 (the default) leaves its sides along the axes.</summary>
    public double Angle { get; set => Change(ref field, value); }

    /// <summary>The point <see cref="Angle"/> turns the rectangle about: its <see cref="Anchor"/> (the default), its centre or a
    /// point of the caller's, matplotlib's <c>rotation_point</c>.</summary>
    public RotationPoint RotationPoint { get; set => Change(ref field, value); }

    /// <inheritdoc />
    /// <remarks>A corner stands at <c>pivot + R · (corner − pivot)</c>, matplotlib's translate, rotate, translate-back about
    /// <c>rotation_point</c>; written from the anchor, so a rectangle turned about its anchor keeps the bytes it always had.</remarks>
    internal override IReadOnlyList<PathSegment> Outline()
    {
        double radians = Angle.ToRadians();
        double cos = Math.Cos(radians), sin = Math.Sin(radians);
        var (pivotX, pivotY) = RotationPoint.OffsetFrom(Anchor, Width, Height);
        DataPoint Corner(double along, double up)
        {
            double fromPivotX = along - pivotX, fromPivotY = up - pivotY;
            return new(Anchor.DataX + pivotX + (fromPivotX * cos - fromPivotY * sin), Anchor.DataY + pivotY + (fromPivotX * sin + fromPivotY * cos));
        }

        return OutlineThrough([Corner(0, 0), Corner(Width, 0), Corner(Width, Height), Corner(0, Height)], closed: true);
    }

    /// <inheritdoc />
    internal override PatchDto Describe() => new RectangleDto(this);
}
