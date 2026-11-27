// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>The one title rule of an axes and of its axis titles: the font a title is drawn in, the pad its style adds,
/// where the axes title stands above the plot and the room the layout keeps for it, and the room the X axis title takes
/// below the bottom spine. The renderer draws by it and the layout reserves by it (<see cref="YAxisTitleExtensions"/> holds
/// the same for the Y axis titles).</summary>
internal static class AxesTitleExtensions
{
    /// <summary>The gap between the axes title's baseline and what is drawn below it (the top spine, or the band above it) under the
    /// library's own themes, in pixels; the matplotlib themes read matplotlib's <c>axes.titlepad</c> (6 points under the default style,
    /// <see cref="TextLook.TitlePad"/>).</summary>
    internal const double TitleGap = 8;

    extension(Theme theme)
    {
        /// <summary>The gap between the axes title's baseline and what is drawn below it under this theme: matplotlib's
        /// <c>axes.titlepad</c> where the theme names one (<see cref="TextLook.TitlePad"/>), else <see cref="TitleGap"/>.</summary>
        internal double TitleGapUnder => theme.Looks.Text.TitlePadOr(TitleGap);

        /// <summary>The gap between the tick labels of a top or bottom edge and the horizontal axis title beside them under this theme:
        /// matplotlib's <c>axes.labelpad</c> where the theme names one (<see cref="TextLook.LabelPad"/>), else
        /// <see cref="TitleClearance.XLabelGap"/>.</summary>
        internal double XLabelGapUnder => theme.Looks.Text.LabelPadOver(null, TitleClearance.XLabelGap);
    }

    extension(Axes axes)
    {
        /// <summary>The font the axes title is drawn in: the theme's title font with the title's style applied.</summary>
        /// <param name="theme">The active theme.</param>
        /// <returns>The title font.</returns>
        internal Font TitleFont(Theme theme)
        {
            var font = theme.TitleFont();
            return axes.TitleStyle?.ApplyTo(font) ?? font;
        }

        /// <summary>How much further up the title's style moves it (<see cref="TextStyle.Pad"/>); 0 without one.</summary>
        internal double TitlePad => axes.TitleStyle?.Pad ?? 0;

        /// <summary>The title <paramref name="axis"/> of this axes draws under <paramref name="theme"/>: the label it was given, else, on the
        /// primary X or Y axis under a theme whose verbs name their value axis (<see cref="SeabornVerbLook.NamesValueAxis"/>), the name the
        /// first series that names the axis its values run along gives it (<see cref="INamesValueAxis"/>: a count plot's 'count', a density
        /// curve's 'Density'), as seaborn's <c>_add_axis_labels</c> writes a label only where none is set. The one title every axis title
        /// is drawn and kept room for by; the name a verb gives is never written to the axis, so the JSON holds what was given.</summary>
        /// <param name="axis">An axis of this axes.</param>
        /// <param name="theme">The theme the axes is drawn under.</param>
        /// <returns>The title; <see langword="null"/> when the axis draws none.</returns>
        internal string? TitleOf(Axis axis, Theme theme)
        {
            if (axis.Label is { } label) return label;
            if (!theme.Looks.Verbs.NamesValueAxis) return null;
            bool alongX = ReferenceEquals(axis, axes.XAxis);
            if (!alongX && !ReferenceEquals(axis, axes.YAxis)) return null;
            // Walked by index: a foreach over the list boxes its enumerator.
            for (int i = 0; i < axes.Series.Count; i++)
                if (axes.Series[i] is INamesValueAxis named && named.ValuesAlongX == alongX) return named.ValueAxisName;
            return null;
        }

        /// <summary>Where the axes title's baseline stands: <paramref name="lift"/> (<c>TitleLift</c>) above <paramref name="topBand"/>,
        /// which stands on the top spine at <paramref name="plotTop"/>.</summary>
        /// <param name="plotTop">The y of the top spine.</param>
        /// <param name="topBand">What is drawn above the top spine below the title: the mirrored X tick labels or the
        /// secondary X axis; 0 when nothing is.</param>
        /// <param name="lift">How far the title's baseline stands above the band (<c>TitleLift</c>).</param>
        /// <returns>The y of the title's baseline.</returns>
        internal double TitleBaseline(double plotTop, double topBand, double lift) => plotTop - lift - topBand;

        /// <summary>How far the axes title's baseline stands above what is drawn below it (the top band, or the spine): the theme's gap
        /// (<c>TitleGapUnder</c>, matplotlib's <c>axes.titlepad</c>) and the style's pad, or, where the theme lifts its title over what stands
        /// below it (<see cref="TextLook.TitleOverWhatIsBelow"/>, matplotlib's <c>axes.titley</c> None), the descent of the title's box when that
        /// is deeper: matplotlib's <c>Axes._update_title_position</c> moves the title up until the bottom of its box clears the top of what is
        /// below it. The one lift the title is drawn by and kept room by.</summary>
        /// <param name="ctx">The context the title is measured with.</param>
        /// <param name="theme">The active theme.</param>
        /// <returns>The lift, in pixels.</returns>
        internal double TitleLift(IRenderContext ctx, Theme theme) =>
            !theme.Looks.Text.TitleOverWhatIsBelow || axes.Title is not { } title
                ? axes.TitleOffset(theme)
                : axes.TitleLiftOver(ctx.ExtentOf(TextLabel.Of(title), axes.TitleFont(theme)), theme);

        /// <summary>The lift of <c>TitleLift</c> for a title whose box is <paramref name="box"/>, measured once by the caller.</summary>
        private double TitleLiftOver(TextExtent box, Theme theme) =>
            theme.Looks.Text.TitleOverWhatIsBelow ? Math.Max(axes.TitleOffset(theme), box.Height - box.Ascent) : axes.TitleOffset(theme);

        /// <summary>The gap and the style's pad: how far the title's baseline stands above what is below it unless it is lifted further.</summary>
        private double TitleOffset(Theme theme) => theme.TitleGapUnder + axes.TitlePad;

        /// <summary>The room the layout keeps above the top spine for the top band and the axes title, as the title is drawn:
        /// the band, the title's lift (<c>TitleLift</c>), the part of the title's box above its baseline (its whole line under the
        /// library's line rule, which measures no box: <see cref="TextLook.MeasuresLikeAgg"/>) as measured in its own font
        /// (<c>TitleFont</c>) by the rule it is drawn by (a title that holds math as its rich text,
        /// <see cref="MathTextRenderContextExtensions"/>), and <paramref name="padAbove"/> above it: matplotlib's tight layout keeps room to
        /// the top of the title's box (<c>get_tightbbox</c>), its descent standing in the lift. Without a title the band and the pad, and
        /// nothing when neither is drawn.</summary>
        /// <param name="ctx">The context text is measured with.</param>
        /// <param name="theme">The active theme.</param>
        /// <param name="topBand">What is drawn above the top spine below the title (<see cref="Layout.EdgeBands.Top"/>).</param>
        /// <param name="padAbove">The pad the layout keeps above what it reserves: the buffer against the figure's edge and the
        /// figure's title of a tight layout, none where nothing is drawn above it in a fixed layout
        /// (<see cref="Layout.ConstrainedLayoutEngine.PadTop"/>, <see cref="FigureLook.NoPadAboveTitle"/>).</param>
        /// <returns>The room, in pixels.</returns>
        internal double TitleBand(IRenderContext ctx, Theme theme, double topBand, double padAbove)
        {
            if (axes.Title is null) return topBand > 0 ? topBand + padAbove : 0;
            // Measured as Agg measures it, the extent is matplotlib's text box, whose descent stands in the lift; the library's line rule
            // keeps the whole line.
            var box = ctx.ExtentOf(TextLabel.Of(axes.Title), axes.TitleFont(theme));
            double above = theme.Looks.Text.MeasuresLikeAgg ? box.Ascent : box.Height;
            return topBand + axes.TitleLiftOver(box, theme) + above + padAbove;
        }
    }

    extension(Axis axis)
    {
        /// <summary>The font the axis title is drawn in: <paramref name="baseLabelFont"/> with the axis' label style
        /// applied.</summary>
        /// <param name="baseLabelFont">The theme's axis title font.</param>
        /// <returns>The title font.</returns>
        internal Font TitleFont(Font baseLabelFont) => axis.LabelStyle?.ApplyTo(baseLabelFont) ?? baseLabelFont;

        /// <summary>The font the axis title is drawn in under <paramref name="theme"/>: the theme's axis label font with the axis' label
        /// style applied (<c>TitleFont</c>). Every axis title is drawn and measured in it: the X axis', the top axis', a
        /// linked axis' below the plot, a ternary corner's.</summary>
        /// <param name="theme">The theme the axis is drawn under.</param>
        /// <returns>The title font.</returns>
        internal Font TitleFontUnder(Theme theme) => axis.TitleFont(theme.LabelFont());

        /// <summary>How much further out the axis title's style moves it (<see cref="TextStyle.Pad"/>); 0 without one.</summary>
        internal double TitlePad => axis.LabelStyle?.Pad ?? 0;

        /// <summary>What places this axis' horizontal title beside <paramref name="edge"/> (a top or a bottom one), over tick labels
        /// <paramref name="tallestTickLabel"/> high: the band of its major ticks as drawn (<see cref="TickConfig.TickLabelBand"/>), the
        /// theme's gap (<c>XLabelGapUnder</c>) and the title's size, by the rule every title beside an edge is placed by
        /// (<see cref="TitleClearance.Beside"/>). The X title below the plot, the title of a linked X axis below it and the top axis' title
        /// are placed by it, and the room the layout keeps below the plot is measured by it.</summary>
        /// <param name="edge">The edge the title stands beside.</param>
        /// <param name="theme">The theme the axis is drawn under: its ticks and its gap.</param>
        /// <param name="tallestTickLabel">The tallest tick label drawn on that edge, 0 when none is.</param>
        /// <param name="labelSize">The size of the title's font.</param>
        /// <returns>The clearance.</returns>
        internal TitleClearance XTitleBeside(EdgeTerms edge, Theme theme, double tallestTickLabel, double labelSize) =>
            TitleClearance.Beside(edge, axis.MajorTicksUnder(theme).TickLabelBand(tallestTickLabel), theme.XLabelGapUnder, labelSize);

        /// <summary>From the bottom spine to the far side of the X axis title drawn below tick labels
        /// <paramref name="tallestTickLabel"/> high: the room of the title placed by <see cref="XTitleBeside"/>
        /// (<see cref="TitleClearance.TitleExtent"/>) in the title's own font, measured by the rule it is drawn by (a title that holds
        /// math as its rich text), past its pad; 0 when the axis has no title.</summary>
        /// <param name="title">The title the axis draws (<c>TitleOf</c>), <see langword="null"/> when none.</param>
        /// <param name="ctx">The context the title is measured with.</param>
        /// <param name="theme">The theme the axis is drawn under: its axis title font and its ticks.</param>
        /// <param name="tallestTickLabel">The tallest tick label drawn on the bottom edge, 0 when none is.</param>
        /// <returns>The room, in pixels.</returns>
        internal double BottomTitleExtent(string? title, IRenderContext ctx, Theme theme, double tallestTickLabel) =>
            title is not null ? axis.TitleExtentBeside(EdgeTerms.Bottom, title, ctx, theme, tallestTickLabel) : 0;

        /// <summary>From the top spine to the far side of what the top X axis draws above it: the room of its title placed beside the top
        /// edge past the tallest tick label (<see cref="XTitleBeside"/>, <see cref="TitleClearance.TitleExtent"/>, the rule the room below
        /// the bottom spine is kept by) in the title's own font, measured by the rule it is drawn by (a title that holds math as its rich
        /// text), past its pad; the tick label band when the axis has no title. The axes title rises above it, as matplotlib stands it above
        /// the top of the axis title's text box (<c>Axes._update_title_position</c>, <c>xaxis.get_tightbbox().ymax</c>), and the layout
        /// reserves it.</summary>
        /// <param name="title">The title the axis draws (<c>TitleOf</c>), <see langword="null"/> when none.</param>
        /// <param name="ctx">The context the title is measured with.</param>
        /// <param name="theme">The theme the axis is drawn under: its axis title font and its ticks.</param>
        /// <param name="tallestTickLabel">How far the tick labels drawn on the top edge reach past the mark and the pad, 0 when none is.</param>
        /// <returns>The band, in pixels.</returns>
        internal double TopBand(string? title, IRenderContext ctx, Theme theme, double tallestTickLabel) =>
            title is not null
                ? axis.TitleExtentBeside(EdgeTerms.Top, title, ctx, theme, tallestTickLabel)
                : axis.MajorTicksUnder(theme).TickLabelBand(tallestTickLabel);

        /// <summary>From <paramref name="edge"/> (a top or a bottom one) to the far side of this axis' horizontal title
        /// <paramref name="label"/>, placed past tick labels <paramref name="tallestTickLabel"/> high (<see cref="XTitleBeside"/>,
        /// <see cref="TitleClearance.TitleExtent"/>) in its own font, measured by the rule it is drawn by, past its pad: the one room
        /// rule of a horizontal axis title on either edge.</summary>
        private double TitleExtentBeside(EdgeTerms edge, string label, IRenderContext ctx, Theme theme, double tallestTickLabel)
        {
            var font = axis.TitleFontUnder(theme);
            return axis.XTitleBeside(edge, theme, tallestTickLabel, font.Size).TitleExtent(ctx.ExtentOf(TextLabel.Of(label), font).Height, axis.TitlePad);
        }

        /// <summary>What the axis draws below the edge it hangs from, below tick labels <paramref name="tallestTickLabel"/> high: its
        /// tick label band and its title's extent (<see cref="BottomTitleExtent"/>), the one value whose
        /// <see cref="Layout.BandsBelowAxis.Extent"/> says how far it reaches.</summary>
        /// <param name="title">The title the axis draws (<c>TitleOf</c>), <see langword="null"/> when none.</param>
        /// <param name="ctx">The context the title is measured with.</param>
        /// <param name="theme">The theme the axis is drawn under.</param>
        /// <param name="tallestTickLabel">The tallest tick label drawn on that edge, 0 when none is.</param>
        /// <returns>The bands.</returns>
        internal Layout.BandsBelowAxis BandsBelow(string? title, IRenderContext ctx, Theme theme, double tallestTickLabel) =>
            new(axis.MajorTicksUnder(theme).TickLabelBand(tallestTickLabel), axis.BottomTitleExtent(title, ctx, theme, tallestTickLabel));
    }
}
