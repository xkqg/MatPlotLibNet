// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>What <see cref="OverlineBox.LayBar"/> placed: where the body's baseline stands and the room the bar and the body take.</summary>
/// <param name="BodyBaseline">How far below the baseline the body's baseline stands (a part of a pixel: the stretched glue is set on whole
/// pixels).</param>
/// <param name="Extent">The room of the bar over the body.</param>
internal readonly record struct MathBar(double BodyBaseline, MathExtent Extent);

/// <summary><c>\overline{x}</c> (matplotlib's <c>Parser.overline</c>): a rule over the body, three rules above its top.</summary>
/// <param name="body">The body.</param>
internal sealed class OverlineBox(MathBox body) : MathBox
{
    /// <inheritdoc />
    internal override MathExtent Lay(MathInk ink, in MathStyle style)
    {
        var bodyFrom = ink.Mark;
        var inner = body.Lay(ink, style);
        var end = ink.Mark;
        var bar = LayBar(ink, style, inner, inner.Height + 3 * style.Rule, inner.Depth, 0);
        ink.Shift(bodyFrom, end, 0, bar.BodyBaseline);
        return bar.Extent;
    }

    /// <summary>The list of a rule over a body (matplotlib's <c>Vlist([Hrule, Glue('fill'), body])</c> packed to a height): the rule's
    /// top a tenth and a half of a twelfth of the size above <paramref name="height"/>, the body under it, the glue between stretched on
    /// whole pixels, the depth at most <paramref name="depth"/>. Places the rule; the caller moves the body by
    /// <see cref="MathBar.BodyBaseline"/>.</summary>
    /// <param name="ink">Where the rule goes.</param>
    /// <param name="style">The style.</param>
    /// <param name="body">The body's room.</param>
    /// <param name="height">How high the list reaches above the baseline, before the reach of the rule.</param>
    /// <param name="depth">How deep the list may reach.</param>
    /// <param name="padding">The room on each side of the body.</param>
    /// <returns>Where the body stands and the room of the list.</returns>
    internal static MathBar LayBar(MathInk ink, in MathStyle style, MathExtent body, double height, double depth, double padding)
    {
        double thickness = style.Rule;
        // (fontsize * dpi) / (100 * 12) points of matplotlib: six hundredths of the size in pixels.
        double top = height + style.Size * 0.06;
        double natural = thickness + body.Height, below = body.Depth;
        if (below > depth)
        {
            natural += below - depth;
            below = depth;
        }

        double glue = Math.Round(top - natural);
        double width = body.Width + 2 * padding;
        ink.AddRule(0, -top, width, thickness);
        return new MathBar(-top + thickness + glue + body.Height, new MathExtent(width, top, below));
    }

    /// <inheritdoc />
    internal override bool IsCharNode => false;

    /// <inheritdoc />
    internal override void Flatten(MathSpanWriter writer) => body.Flatten(writer);
}
