// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>A nucleus with a superscript, a subscript or both beside it (matplotlib's <c>Parser.subsuper</c>, TeX's nodes 756 to 759):
/// each script one size smaller, kerned from the nucleus by the set's delta (further from a leaning last character), the superscript
/// raised by at least <see cref="MathConstants.Sup1"/>, the subscript dropped by at least <see cref="MathConstants.Sub1"/> alone or
/// <see cref="MathConstants.Sub2"/> under a superscript, the two kept four rules apart, the scripts of a nucleus that is no character
/// measured from its top and its bottom, and an integral's set by its top and its foot; a space of
/// <see cref="MathConstants.ScriptSpace"/> after them.</summary>
/// <param name="nucleus">The nucleus; an empty row when the scripts stand alone.</param>
/// <param name="superscript">The superscript, or <see langword="null"/>.</param>
/// <param name="subscript">The subscript, or <see langword="null"/>.</param>
internal sealed class ScriptsBox(MathBox nucleus, MathBox? superscript, MathBox? subscript) : MathBox
{
    /// <inheritdoc />
    internal override MathExtent Lay(MathInk ink, in MathStyle style)
    {
        var c = style.Constants;
        double xHeight = style.XHeight, rule = style.Rule;
        var core = nucleus.Lay(ink, style);
        var last = nucleus.LastChar(style);
        bool drops = last is { DropsScripts: true };
        double lastHeight = last?.Height ?? 0, lastBaseline = drops ? last!.Value.Depth : 0;

        double superKern = c.Delta * xHeight, subKern = c.Delta * xHeight;
        if (last is { Slanted: true })
        {
            superKern += c.Delta * xHeight + c.DeltaSlanted * (lastHeight - xHeight * 2 / 3);
            if (drops)
            {
                subKern = (3 * c.Delta - c.DeltaIntegral) * lastHeight;
                superKern = (3 * c.Delta + c.DeltaIntegral) * lastHeight;
            }
            else
            {
                subKern = 0;
            }
        }

        double up = 0, down = 0;
        var small = style.Smaller;
        if (!nucleus.IsCharNode)
        {
            double smallXHeight = c.XHeight * style.Size * c.ShrinkFactor;
            up = core.Height - c.SupDrop * smallXHeight;
            down = core.Depth + c.SubDrop * smallXHeight;
        }

        // A script's kern shrinks with it (the kern and the script are one list, shrunk together).
        double shrink = small.Size / style.Size;
        double width, height = core.Height, depth = core.Depth;
        if (superscript is null)
        {
            var from = ink.Mark;
            var sub = subscript!.Lay(ink, small);
            down = drops ? lastBaseline + c.SubDrop * xHeight : Math.Max(Math.Max(down, c.Sub1 * xHeight), sub.Height - xHeight * 4 / 5);
            ink.Shift(from, core.Width + subKern * shrink, down);
            width = subKern * shrink + sub.Width;
            height = Math.Max(height, sub.Height - down);
            depth = Math.Max(depth, sub.Depth + down);
        }
        else
        {
            var supFrom = ink.Mark;
            var sup = superscript.Lay(ink, small);
            var supTo = ink.Mark;
            up = drops ? lastHeight - c.SubDrop * xHeight : Math.Max(Math.Max(up, c.Sup1 * xHeight), sup.Depth + xHeight / 4);
            width = superKern * shrink + sup.Width;
            if (subscript is not null)
            {
                var subFrom = ink.Mark;
                var sub = subscript.Lay(ink, small);
                down = drops ? lastBaseline + c.SubDrop * xHeight : Math.Max(down, c.Sub2 * xHeight);

                // The two scripts kept four rules apart: the subscript goes down, then both go up until the superscript's foot is at
                // four fifths of the x-height.
                double clearance = 4 * rule - ((up - sup.Depth) - (sub.Height - down));
                if (clearance > 0)
                {
                    down += clearance;
                    clearance = xHeight * 4 / 5 - up + sup.Depth;
                    if (clearance > 0)
                    {
                        up += clearance;
                        down -= clearance;
                    }
                }

                ink.Shift(subFrom, core.Width + subKern * shrink, down);
                width = Math.Max(width, subKern * shrink + sub.Width);
                depth = Math.Max(depth, sub.Depth + down);
            }

            ink.Shift(supFrom, supTo, core.Width + superKern * shrink, -up);
            height = Math.Max(height, sup.Height + up);
            if (subscript is null) depth = Math.Max(depth, sup.Depth - up);
        }

        if (!drops) width += c.ScriptSpace * xHeight;
        return new MathExtent(core.Width + width, height, depth);
    }

    /// <inheritdoc />
    /// <remarks>Both scripts stand in a vertical list; one stands in a horizontal one, a character node as its content is.</remarks>
    internal override bool IsCharNode => (superscript is null || subscript is null) && nucleus.IsCharNode
        && (superscript?.IsCharNode ?? true) && (subscript?.IsCharNode ?? true);

    /// <inheritdoc />
    internal override void Flatten(MathSpanWriter writer)
    {
        nucleus.Flatten(writer);
        if (subscript is not null) writer.Script(subscript, TextSpanKind.Subscript);
        if (superscript is not null) writer.Script(superscript, TextSpanKind.Superscript);
    }
}
