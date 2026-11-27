// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.MathText;

/// <summary><c>\sqrt{x}</c> and <c>\sqrt[n]{x}</c> (matplotlib's <c>Parser.sqrt</c>): the radical sign grown to the body's height and
/// five rules more, a bar from its top over the body (<see cref="OverlineBox"/>'s list, with two rules of padding each side of the
/// body), the index two sizes smaller, raised to six tenths of the sign's height, standing over the sign's left half.</summary>
/// <param name="body">The body.</param>
/// <param name="index">The index, or <see langword="null"/>.</param>
internal sealed class RadicalBox(MathBox body, MathBox? index) : MathBox
{
    /// <summary>The radical sign.</summary>
    private const string Sign = "√";

    /// <inheritdoc />
    internal override MathExtent Lay(MathInk ink, in MathStyle style)
    {
        double thickness = style.Rule;
        var bodyFrom = ink.Mark;
        var inner = body.Lay(ink, style);
        var signFrom = ink.Mark;
        var sign = SizedGlyph.Lay(ink, style, Sign, inner.Height + 5 * thickness, inner.Depth);
        // The index stands over the sign's left half, the body padded by two rule thicknesses each side (matplotlib's Hbox(2 * thickness)).
        double half = 0.5 * sign.Width, padding = 2 * thickness;
        var indexFrom = ink.Mark;
        var root = index?.Lay(ink, style.Smaller.Smaller) ?? new MathExtent(half, 0, 0);
        var end = ink.Mark;

        double signX = root.Width - half, barX = signX + sign.Width;
        var bar = OverlineBox.LayBar(ink, style, inner, sign.Height, sign.Depth, padding);
        ink.Shift(bodyFrom, signFrom, barX + padding, bar.BodyBaseline);
        ink.Shift(signFrom, indexFrom, signX, 0);
        double rootBaseline = -sign.Height * 0.6;
        ink.Shift(indexFrom, end, 0, rootBaseline);
        ink.Shift(end, barX, 0);

        double width = barX + inner.Width + 2 * padding;
        double height = Math.Max(Math.Max(sign.Height, bar.Extent.Height), root.Height - rootBaseline);
        double depth = Math.Max(Math.Max(sign.Depth, bar.Extent.Depth), root.Depth + rootBaseline);
        return new MathExtent(width, height, depth);
    }

    /// <inheritdoc />
    internal override bool IsCharNode => false;

    /// <inheritdoc />
    internal override void Flatten(MathSpanWriter writer)
    {
        if (index is not null) writer.Part(index, TextSpanKind.Superscript, 2);
        writer.Marked(body, TextSpanKind.Radical);
    }
}
