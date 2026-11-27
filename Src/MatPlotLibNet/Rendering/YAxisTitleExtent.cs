// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Where one Y axis slot's title stands and how far the slot reaches past its edge.</summary>
/// <param name="TitleOffset">From the slot's edge to the title's anchor (the line it is turned about), its pad included;
/// 0 when the axis has no title.</param>
/// <param name="Band">From the slot's edge to the far side of everything it draws: the outward ink of its title, or the
/// far side of its tick labels when it has no title.</param>
internal readonly record struct YAxisTitleExtent(double TitleOffset, double Band);

/// <summary>The one band rule of a Y axis slot, for every slot of the container including the primary: the layout
/// reserves by it (<c>ConstrainedLayoutEngine</c>), the renderer stacks the slots by it and each slot places its title by
/// it, so what is reserved is what is drawn and the gap between a title and its tick labels is the same on both
/// sides.</summary>
internal static class YAxisTitleExtensions
{
    extension(YAxisContainer container)
    {
        /// <summary>The input a Y title's band is measured from, for the drawing and the layout alike: <paramref name="tickLabelBand"/>,
        /// <paramref name="side"/>, the container's gap under <paramref name="theme"/> (<see cref="YAxisContainer.TitleGapUnder"/>) and
        /// the theme's axis label font.</summary>
        /// <param name="theme">The theme the axes is drawn under.</param>
        /// <param name="side">The side the axis stands on.</param>
        /// <param name="tickLabelBand">From the axis' edge to the far side of its tick labels
        /// (<see cref="TickConfig.TickLabelBand"/>); 0 when none is drawn.</param>
        /// <returns>The input.</returns>
        internal YAxisTitleInput TitleInputUnder(Theme theme, AxisSide side, double tickLabelBand) =>
            new YAxisTitleInput(tickLabelBand, side, container.TitleGapUnder(theme), theme.LabelFont());
    }

    extension(Axis axis)
    {
        /// <summary>The title offset and band of this axis as a Y slot. The title stands by the one rule that places a title beside
        /// its edge (<see cref="TitleClearance.Beside"/>, <see cref="TitleClearance.TitleOffset"/>): the ink that faces the tick labels
        /// (its baseline on the left, the body of its text on the right, <see cref="FacingInk"/>) stands the container's gap past their
        /// band, or the title stands at the edge's place without labels (<see cref="EdgeTerms.TitleWithoutTickLabels"/>) when no tick
        /// label is drawn; its style's pad (<see cref="TextStyle.Pad"/>) moves it further out on top of the gap. The band reaches the
        /// title's outward ink past its anchor (its ascent on the left, its descent on the right), so on either side it is the tick label
        /// band, the gap, the pad and the title's font size.</summary>
        /// <param name="title">The title the axis draws (<c>AxesTitleExtensions.TitleOf</c>), <see langword="null"/> when none.</param>
        /// <param name="input">The slot's tick label band, side, gap and the theme's title font.</param>
        /// <returns>The title offset and the band; without a title, no offset and the tick label band alone.</returns>
        internal YAxisTitleExtent YTitleExtent(string? title, YAxisTitleInput input)
        {
            if (title is null) return new YAxisTitleExtent(0, input.TickLabelBand);
            double size = axis.TitleFont(input.BaseLabelFont).Size;
            var clearance = TitleClearance.Beside(EdgeTerms.Of(input.Side), input.TickLabelBand, input.TitleGap, size);
            double offset = clearance.TitleOffset() + axis.TitlePad;
            return new YAxisTitleExtent(offset, offset + clearance.Ink.Outward * size);
        }
    }

    extension(IRenderContext ctx)
    {
        /// <summary>Draws the title of a Y axis by the one band rule (<see cref="YTitleExtent"/>): turned a quarter, centred
        /// on <paramref name="edge"/>'s height, its offset out past the edge on its side, in the axis' title font; a title
        /// with math markup is drawn as the rich text it is. Nothing is drawn when the axis has no title.</summary>
        /// <param name="axis">The axis whose title is drawn.</param>
        /// <param name="title">The title the axis draws (<c>AxesTitleExtensions.TitleOf</c>), <see langword="null"/> when none.</param>
        /// <param name="input">The axis' tick label band, side, gap and the theme's title font.</param>
        /// <param name="edge">The x of the axis' edge and the height the title is centred on.</param>
        internal void DrawYAxisTitle(Axis axis, string? title, YAxisTitleInput input, Point edge)
        {
            if (title is not { } label) return;
            var extent = axis.YTitleExtent(label, input);
            var at = new Point(edge.X + EdgeTerms.Of(input.Side).Outward * extent.TitleOffset, edge.Y);
            var font = axis.TitleFont(input.BaseLabelFont);
            ctx.Draw(TextLabel.Of(label), font, new TextPlacement(at, TextAlignment.Center) { Rotation = 90 });
        }
    }

    /// <summary>What the band of one Y axis slot is measured from (<see cref="YAxisTitleExtensions"/>).</summary>
    /// <param name="TickLabelBand">From the slot's edge to the far side of its widest tick label
    /// (<see cref="TickConfig.TickLabelBand"/>); 0 when no tick label is drawn.</param>
    /// <param name="Side">The side the slot stands on: which part of the title's ink faces the tick labels.</param>
    /// <param name="TitleGap">The blank gap between the tick labels' ink and the title's ink: the container's
    /// <see cref="YAxisContainer.TitleGap"/>.</param>
    /// <param name="BaseLabelFont">The theme's axis title font, before the axis' own label style.</param>
    internal readonly record struct YAxisTitleInput(double TickLabelBand, AxisSide Side, double TitleGap, Font BaseLabelFont);
}
