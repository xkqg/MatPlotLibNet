// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>A nucleus with its limits above and below it (matplotlib's <c>Parser.subsuper</c> for an over/under nucleus): each limit one
/// size smaller, the three centred on the widest, three rules between a limit and the nucleus, the nucleus on the baseline.</summary>
/// <param name="nucleus">The sign or the function name.</param>
/// <param name="over">The upper limit, or <see langword="null"/>.</param>
/// <param name="under">The lower limit, or <see langword="null"/>.</param>
internal sealed class LimitsBox(MathBox nucleus, MathBox? over, MathBox? under) : MathBox
{
    /// <inheritdoc />
    internal override MathExtent Lay(MathInk ink, in MathStyle style)
    {
        var small = style.Smaller;
        var coreFrom = ink.Mark;
        var core = nucleus.Lay(ink, style);
        var overFrom = ink.Mark;
        var top = over?.Lay(ink, small) ?? default;
        var underFrom = ink.Mark;
        var bottom = under?.Lay(ink, small) ?? default;
        var end = ink.Mark;

        double width = Math.Max(core.Width, Math.Max(top.Width, bottom.Width));
        double gap = 3 * style.Rule;
        ink.Shift(coreFrom, overFrom, CentredOffset(width, core.Width), 0);
        double height = core.Height, depth = core.Depth;
        if (over is not null)
        {
            ink.Shift(overFrom, underFrom, CentredOffset(width, top.Width), -(core.Height + gap + top.Depth));
            height = core.Height + gap + top.Depth + top.Height;
        }

        if (under is not null)
        {
            ink.Shift(underFrom, end, CentredOffset(width, bottom.Width), core.Depth + gap + bottom.Height);
            depth = core.Depth + gap + bottom.Height + bottom.Depth;
        }

        return new MathExtent(width, height, depth);
    }

    /// <inheritdoc />
    internal override bool IsCharNode => false;

    /// <inheritdoc />
    internal override void Flatten(MathSpanWriter writer)
    {
        nucleus.Flatten(writer);
        if (under is not null) writer.Script(under, TextSpanKind.OperatorSubscript);
        if (over is not null) writer.Script(over, TextSpanKind.OperatorSuperscript);
    }
}
