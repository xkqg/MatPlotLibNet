// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.MathText;

/// <summary><c>\substack{a \\ b}</c> (matplotlib's <c>Parser.substack</c>): the rows one over the other, each centred on the widest, two
/// rules apart, the last on the baseline.</summary>
/// <param name="rows">The rows, top to bottom.</param>
internal sealed class StackBox(MathBox[] rows) : MathBox
{
    /// <inheritdoc />
    internal override MathExtent Lay(MathInk ink, in MathStyle style)
    {
        var marks = new MathInkMark[rows.Length + 1];
        var sizes = new MathExtent[rows.Length];
        double width = 0;
        for (int i = 0; i < rows.Length; i++)
        {
            marks[i] = ink.Mark;
            sizes[i] = rows[i].Lay(ink, style);
            width = Math.Max(width, sizes[i].Width);
        }

        marks[^1] = ink.Mark;
        double gap = 2 * style.Rule, baseline = 0;
        for (int i = rows.Length - 1; i >= 0; i--)
        {
            if (i < rows.Length - 1) baseline -= sizes[i + 1].Height + gap + sizes[i].Depth;
            ink.Shift(marks[i], marks[i + 1], CentredOffset(width, sizes[i].Width), baseline);
        }

        // A substack holds at least one row (MathSubstack reads one before the first \\).
        return new MathExtent(width, sizes[0].Height - baseline, sizes[^1].Depth);
    }

    /// <inheritdoc />
    internal override bool IsCharNode => false;

    /// <inheritdoc />
    internal override void Flatten(MathSpanWriter writer)
    {
        foreach (var row in rows) row.Flatten(writer);
    }
}
