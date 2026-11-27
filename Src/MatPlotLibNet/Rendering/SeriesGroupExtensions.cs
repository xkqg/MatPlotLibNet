// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>The one rule for the group a series is drawn in. Every series pass opens it (<see cref="SeriesPass.DrawInItsGroup"/>: the
/// primary series, every Y axis slot's, the top X axis'), so the legend toggle, the hover highlight and a streaming diff — all of
/// which find a series by its group — reach a line on any axis exactly as they reach a primary one.</summary>
internal static class SeriesGroupExtensions
{
    extension(IRenderContext ctx)
    {
        /// <summary>Opens the group <paramref name="series"/> is drawn in. With interactive attributes it carries
        /// <c>data-series-index</c> = <paramref name="slotIndex"/> (the index the series' legend entry carries) and the
        /// series label as <c>aria-label</c>; without them a labelled series gets a group with its <c>aria-label</c> and an
        /// unlabelled one none. Only a context that writes markup opens one (<see cref="IMarkupRenderContext.OpenSeriesGroup"/>).</summary>
        /// <param name="axes">The axes that owns the series; what it writes for the browser
        /// (<see cref="Svg.SvgEmit.Of(Models.Axes, IRenderContext)"/>) decides the kind of group.</param>
        /// <param name="series">The series about to be drawn.</param>
        /// <param name="slotIndex">The series' slot (<see cref="SeriesSlotExtensions.SlotIndex"/>).</param>
        /// <param name="drawnWith">The transform the series is drawn with — the primary one, or the secondary axis' own.
        /// With <see cref="SvgEmit.DataCursorMapping"/> the interactive group carries it, so a click on the series
        /// converts with the range of the axis the series belongs to; <see langword="null"/> where the series is not drawn
        /// through a Cartesian data range (polar, 3-D).</param>
        /// <returns><see langword="true"/> when a group was opened; the caller closes it after the series (and whatever
        /// belongs to it, such as its end label) is drawn.</returns>
        internal bool BeginSeriesGroup(Axes axes, ISeries series, int slotIndex,
            DataTransform? drawnWith = null) =>
            ctx.OpenSeriesGroup(SvgEmit.Of(axes, ctx), series.Label, slotIndex, drawnWith);

        /// <summary>Cuts a series' marks to the clip they are drawn in: <paramref name="region"/> (the plot area, or the disc of a polar
        /// axes), where the axes cuts the series off at the plot (<see cref="ClipsItsMarksUnder"/>, asked with the series' artist and
        /// whether its marks stand past its range). Opened inside the series' group; it cuts the series' marks only, never its text (a
        /// label, a value, a name: <see cref="MarkClips{TClip}"/>).</summary>
        /// <param name="axes">The axes that owns the series.</param>
        /// <param name="series">The series about to be drawn, whose kind says whether matplotlib cuts its artist off
        /// (<see cref="MatplotlibArtist.ClipOn"/>) and whether its marks stand past its range
        /// (<see cref="ChartSeries.MarksReachPastItsRange"/>).</param>
        /// <param name="region">Where the series' marks are cut off.</param>
        /// <param name="theme">The theme the series is drawn under, whose frame says whether every mark is cut off.</param>
        /// <returns>The clip, which ends with the scope the caller disposes right after the series is drawn; a scope that was never opened
        /// where the axes does not cut the series off.</returns>
        internal ClipScope ClippedForSeries(Axes axes, ISeries series, in ClipRegion region, Theme theme)
        {
            bool clipped = series is ChartSeries chart
                ? axes.ClipsItsMarksUnder(theme, chart.Artist, chart.MarksReachPastItsRange(theme))
                : axes.ClipsItsMarksUnder(theme, MatplotlibArtist.Own);
            return clipped ? ctx.ClippedTo(region) : default;
        }
    }

    extension(Axes axes)
    {
        /// <summary>Whether the marks of the axes drawn as <paramref name="artist"/> (a series of that kind, or a patch) are cut off at
        /// the plot: the one clip rule, which the series' clip and the patches' clip ask. Under a theme whose frame clips every artist
        /// (<see cref="AxesFrameLook.ClipsEveryArtist"/>, the matplotlib themes) it is matplotlib's <c>clip_on</c> of the artist
        /// (<see cref="MatplotlibArtist.ClipOn"/>: every artist, a kind of the library's own too, but a pie's wedges and a table, with a
        /// limit or without one). Under the library's own themes a mark is cut off where the axes has an explicit limit
        /// (<see cref="Axes.HasExplicitLimit"/>), because a set limit is exact and the data may reach past it, or where the kind's marks
        /// stand past the range it reports (<paramref name="reachesPastRange"/>); a range that scaled itself encloses every other series,
        /// which draws as it did. A clip cuts marks only: the text of a series stands whole past it (<see cref="MarkClips{TClip}"/>).</summary>
        /// <param name="theme">The theme the axes is drawn under.</param>
        /// <param name="artist">What the mark is to matplotlib.</param>
        /// <param name="reachesPastRange">Whether the marks stand past the range their series reports
        /// (<see cref="ChartSeries.MarksReachPastItsRange"/>); a patch's take part in the range.</param>
        /// <returns><see langword="true"/> when the marks are clipped to the plot.</returns>
        internal bool ClipsItsMarksUnder(Theme theme, in MatplotlibArtist artist, bool reachesPastRange = false) =>
            theme.Looks.Frame.ClipsEveryArtist ? artist.ClipOn : reachesPastRange || axes.HasExplicitLimit;
    }
}
