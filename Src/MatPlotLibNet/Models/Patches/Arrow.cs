// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Patches;

/// <summary>An arrow that is a shape in data coordinates, matplotlib's <c>FancyArrow</c> (what <c>Axes.arrow</c> draws): a
/// shaft from a start point along a delta and a head, every size in data units, so the arrow stretches with the axes as any
/// data does. An arrow that points AT something from a text, with a head sized in pixels, is an <see cref="Annotation"/>.</summary>
/// <remarks>
/// <para>The shaft runs from <see cref="Start"/> to <see cref="Start"/> + (<see cref="Dx"/>, <see cref="Dy"/>) and the head
/// stands beyond that end point, its tip <see cref="HeadLength"/> past it, unless <see cref="LengthIncludesHead"/> puts the tip
/// on the end point. An arrow of no length points up, as matplotlib's does. The full arrow is matplotlib's eight vertices (the
/// tip, both sides of the head and of the shaft, and the close); a half arrow is five and the close.</para>
/// <para>The sizes default as matplotlib's do: a shaft 0.001 wide, a head three times as wide as the shaft and one and a half
/// times as long as it is wide, a face of the first colour of the cycle and a black edge. A shaft 0.001 data units wide is a
/// hairline on any axes much wider than one unit: set <see cref="Width"/> to the scale of the data.</para>
/// </remarks>
/// <example>
/// <code>
/// ax.AddPatch(new Arrow(new(0, 0), 3, 2) { Width = 0.05 });
/// </code>
/// </example>
public sealed class Arrow : FilledPatch
{
    /// <summary>Creates an arrow from <paramref name="start"/> along (<paramref name="dx"/>, <paramref name="dy"/>), at
    /// matplotlib's sizes, with a black edge.</summary>
    /// <param name="start">Where the shaft starts, in data units.</param>
    /// <param name="dx">How far the shaft runs along X, in data units.</param>
    /// <param name="dy">How far the shaft runs along Y, in data units.</param>
    public Arrow(DataPoint start, double dx, double dy)
    {
        Start = start;
        Dx = dx;
        Dy = dy;
        EdgeColor = Colors.Black;
    }

    /// <summary>Where the shaft starts, in data units (matplotlib's <c>x</c>, <c>y</c>).</summary>
    public DataPoint Start { get; set; }

    /// <summary>How far the shaft runs along X from <see cref="Start"/>, in data units.</summary>
    public double Dx { get; set; }

    /// <summary>How far the shaft runs along Y from <see cref="Start"/>, in data units.</summary>
    public double Dy { get; set; }

    /// <summary>The width of the shaft, in data units. 0.001 by default, as in matplotlib.</summary>
    public double Width { get; set; } = 0.001;

    /// <summary>The width of the head, in data units. <see langword="null"/> (the default) makes it three times the
    /// <see cref="Width"/> of the shaft, as matplotlib does.</summary>
    public double? HeadWidth { get; set; }

    /// <summary>The length of the head, in data units. <see langword="null"/> (the default) makes it one and a half times the
    /// width of the head, as matplotlib does.</summary>
    public double? HeadLength { get; set; }

    /// <summary>Whether the tip stands on the end point, the head taken out of the length. <see langword="false"/> by default,
    /// as in matplotlib: the shaft spans the whole delta and the tip stands a head length past its end.</summary>
    public bool LengthIncludesHead { get; set; }

    /// <summary>Which part of the arrow is drawn: <see cref="ArrowHeadShape.Full"/> (the default) or one half of it.</summary>
    public ArrowHeadShape Shape { get; set; } = ArrowHeadShape.Full;

    /// <summary>How far the back of the head is swept towards the tip, as a fraction of the head length: 0 (the default) is a
    /// triangle, 0.5 sweeps the back halfway to the tip, a negative value pushes it back along the shaft.</summary>
    public double Overhang { get; set; }

    /// <inheritdoc />
    internal override IReadOnlyList<PathSegment> Outline()
    {
        double headWidth = HeadWidth ?? 3 * Width;
        double headLength = HeadLength ?? 1.5 * headWidth;
        double distance = double.Hypot(Dx, Dy);
        double length = LengthIncludesHead ? distance : distance + headLength;
        double shift = LengthIncludesHead ? 0 : headLength;

        // matplotlib's FancyArrow._make_verts: the half below the shaft of an arrow pointing along +X with its tip at the
        // origin (tip, the corner of the head, where the head meets the shaft, the shaft's far corner, the shaft's far end on
        // its axis), moved a head length forward when the head is not part of the length.
        DataPoint[] below =
        [
            new(0 + shift, 0),
            new(-headLength + shift, -headWidth / 2),
            new(-headLength * (1 - Overhang) + shift, -Width / 2),
            new(-length + shift, -Width / 2),
            new(-length + shift, 0),
        ];
        DataPoint Above(int i) => new(below[i].DataX, -below[i].DataY);

        DataPoint[] local = Shape switch
        {
            ArrowHeadShape.Left => below,
            ArrowHeadShape.Right => [.. Enumerable.Range(0, below.Length).Select(Above)],
            // The two halves share the tip and the shaft's far end on its axis, which the full arrow leaves out: its last vertex,
            // the tip again, is the close.
            _ => [below[0], below[1], below[2], below[3], Above(3), Above(2), Above(1)],
        };

        // Turned to the direction of the delta; an arrow of no length points up, as matplotlib's does.
        double cos = distance != 0 ? Dx / distance : 0;
        double sin = distance != 0 ? Dy / distance : 1;
        double endX = Start.DataX + Dx, endY = Start.DataY + Dy;
        return OutlineThrough(
            [.. local.Select(p => new DataPoint(p.DataX * cos - p.DataY * sin + endX, p.DataX * sin + p.DataY * cos + endY))],
            closed: true);
    }

    /// <inheritdoc />
    internal override PatchDto Describe() => new ArrowDto(this);
}
