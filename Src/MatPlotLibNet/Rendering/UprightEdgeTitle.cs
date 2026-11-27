// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>The title of a top or a bottom edge (<see cref="EdgeTitle.Upright"/>): upright, centred along the plot's width, its baseline
/// placed beside the edge past the tick labels by the rule every title beside an edge is placed by (<see cref="AxesTitleExtensions"/>'
/// <c>XTitleBeside</c>, <see cref="TitleClearance.TitleOffset"/>), its style's pad further out; as rich text when it carries math. The X
/// title below the plot, the top axis' title and a linked axis' title below the plot are drawn here.</summary>
internal sealed class UprightEdgeTitle : EdgeTitle
{
    /// <inheritdoc />
    internal override void Draw(IRenderContext ctx, Axes axes, Axis axis, EdgeTerms edge, Theme theme, Rect plot, double edgeLine,
        double tallestTickLabel)
    {
        if (axes.TitleOf(axis, theme) is not { } label) return;
        var font = axis.TitleFontUnder(theme);
        double offset = axis.XTitleBeside(edge, theme, tallestTickLabel, font.Size).TitleOffset() + axis.TitlePad;
        ctx.Draw(TextLabel.Of(label), font, new TextPlacement(new Point(plot.Center.X, edgeLine + edge.Outward * offset), TextAlignment.Center));
    }
}
