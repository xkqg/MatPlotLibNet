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
/// negative width extends the rectangle left of the anchor and a negative height below it, as in matplotlib. A rectangle to
/// be turned about its centre is a <see cref="Polygon"/> of its four corners (matplotlib's <c>rotation_point</c> is not
/// offered).</remarks>
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
    public DataPoint Anchor { get; set; }

    /// <summary>The width along X, in data units, before the rectangle is turned; negative extends it left of the anchor.</summary>
    public double Width { get; set; }

    /// <summary>The height along Y, in data units, before the rectangle is turned; negative extends it below the anchor.</summary>
    public double Height { get; set; }

    /// <summary>The angle the rectangle is turned through about its <see cref="Anchor"/>, in degrees, counter-clockwise in data
    /// space. 0 (the default) leaves its sides along the axes.</summary>
    public double Angle { get; set; }

    /// <inheritdoc />
    internal override IReadOnlyList<PathSegment> Outline()
    {
        double radians = double.DegreesToRadians(Angle);
        double cos = Math.Cos(radians), sin = Math.Sin(radians);
        DataPoint Corner(double along, double up) =>
            new(Anchor.DataX + (along * cos - up * sin), Anchor.DataY + (along * sin + up * cos));

        return OutlineThrough([Anchor, Corner(Width, 0), Corner(Width, Height), Corner(0, Height)], closed: true);
    }

    /// <inheritdoc />
    internal override PatchDto Describe() => new RectangleDto(this);
}
