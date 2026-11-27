// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>What the left edge of an axes needs, read from the axes alone: the left stack of its Y axis container and the pad
/// past it, and the label a Y axis whose labels its renderer does not measure is estimated by. Both layout paths of
/// <see cref="ConstrainedLayoutEngine"/> reserve the left edge by these rules.</summary>
internal static class AxesLeftEdgeExtensions
{
    extension(Axes axes)
    {
        /// <summary>The one rule for what the left edge of the axes needs: its left stack (<see cref="LeftStackBand"/>) and the layout's
        /// pad on the left (<see cref="LayoutPads.Edges"/>), or nothing when the stack draws nothing: the pad holds what is drawn off the
        /// edge, so a pad alone is no need. The mirror of the bottom edge's rule (<see cref="EdgeBands.BottomNeed"/>) and of the right
        /// edge's (<see cref="RightBand.WidthWith"/>).</summary>
        /// <param name="theme">The active theme (the title font).</param>
        /// <param name="primaryTickBand">From the left spine to the far side of the primary's tick labels; 0 when none are drawn.</param>
        /// <param name="insets">How far the spines stand in from the plot's edges (<see cref="SpineInsets"/>): the primary hangs from
        /// the left spine.</param>
        /// <param name="outwardBands">The band of every left slot past the primary, nearest the plot first.</param>
        /// <param name="pad">The pad the layout keeps left of what an axes needs.</param>
        /// <returns>The room the left edge needs; 0 when nothing is drawn there.</returns>
        internal double LeftNeed(Theme theme, double primaryTickBand, SpineInsets insets, double[] outwardBands, double pad) =>
            axes.LeftStackBand(theme, primaryTickBand, insets, outwardBands) is var band and > 0 ? band + pad : 0;

        /// <summary>From the plot's left edge to the far side of everything the left stack of the Y axis container draws: the primary's
        /// tick label band and its title by the one band rule (<see cref="YAxisTitleExtensions"/>, the rule the title is placed
        /// by), hung from the left spine wherever it stands (<see cref="SpineInsets.PastLeft"/>), and every slot past it, which stand
        /// at the plot's edge past the primary's band with the container's gap between each pair
        /// (<see cref="AxisStackingExtensions.TotalStackedBand"/>). 0 when nothing is drawn there.</summary>
        /// <param name="theme">The active theme (the title font).</param>
        /// <param name="primaryTickBand">From the left spine to the far side of the primary's tick labels; 0 when none are drawn.</param>
        /// <param name="insets">How far the spines stand in from the plot's edges: the primary hangs from the left spine.</param>
        /// <param name="outwardBands">The band of every left slot past the primary, nearest the plot first.</param>
        /// <returns>The band of the left stack, without the pad.</returns>
        internal double LeftStackBand(Theme theme, double primaryTickBand, SpineInsets insets, double[] outwardBands)
        {
            double primary = axes.YAxis.YTitleExtent(axes.TitleOf(axes.YAxis, theme), axes.YAxes.TitleInputUnder(theme, AxisSide.Left, primaryTickBand)).Band;
            double pastEdge = insets.PastLeft(primary);
            if (outwardBands.Length == 0) return pastEdge;
            double[] stack = [primary, .. outwardBands];
            return Math.Max(pastEdge, stack.TotalStackedBand(axes.YAxes.TitleGapUnder(theme)));
        }

        /// <summary>A representative Y tick label for a Y axis whose labels its renderer does not measure (polar, 3-D): the upper
        /// limit when one is set, else the lower one, else a worst-case number of the usual width.</summary>
        /// <returns>The label to measure.</returns>
        internal string EstimateYTickLabel()
        {
            if (axes.YAxis.Max.HasValue) return TickFormatters.NumericTickFormatter.FormatValue(axes.YAxis.Max.Value);
            if (axes.YAxis.Min.HasValue) return TickFormatters.NumericTickFormatter.FormatValue(axes.YAxis.Min.Value);
            return "−9.999";
        }
    }
}
