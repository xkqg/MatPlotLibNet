// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Rendering;

/// <summary>The one rule for the group a series is drawn in. The primary series passes (<see cref="AxesRenderer"/>) and
/// both secondary-axis parts open it the same way, so the legend toggle, the hover highlight and a streaming diff — all of
/// which find a series by its group — reach a secondary line exactly as they reach a primary one.</summary>
internal static class SeriesGroupExtensions
{
    /// <summary>Opens the group <paramref name="series"/> is drawn in. With interactive attributes it carries
    /// <c>data-series-index</c> = <paramref name="slotIndex"/> (the index the series' legend entry carries) and the
    /// series label as <c>aria-label</c>; without them a labelled series gets a group with its <c>aria-label</c> and an
    /// unlabelled one none. Only an SVG context draws groups.</summary>
    /// <param name="ctx">The context the series is drawn into.</param>
    /// <param name="axes">The axes that owns the series; its <see cref="Axes.EnableInteractiveAttributes"/> decides the
    /// kind of group.</param>
    /// <param name="series">The series about to be drawn.</param>
    /// <param name="slotIndex">The series' slot (<see cref="SeriesSlotExtensions.SlotIndex"/>).</param>
    /// <param name="drawnWith">The transform the series is drawn with — the primary one, or the secondary axis' own.
    /// With <see cref="Axes.EmitDataCursorMapping"/> the interactive group carries it, so a click on the series
    /// converts with the range of the axis the series belongs to; <see langword="null"/> where the series is not drawn
    /// through a Cartesian data range (polar, 3-D).</param>
    /// <returns><see langword="true"/> when a group was opened; the caller closes it after the series (and whatever
    /// belongs to it, such as its end label) is drawn.</returns>
    internal static bool BeginSeriesGroup(this IRenderContext ctx, Axes axes, ISeries series, int slotIndex,
        DataTransform? drawnWith = null)
    {
        if (ctx is not SvgRenderContext svg) return false;
        if (axes.EnableInteractiveAttributes)
        {
            svg.BeginDataGroup("series", slotIndex, series.Label, axes.EmitDataCursorMapping ? drawnWith : null);
            return true;
        }
        if (string.IsNullOrEmpty(series.Label)) return false;
        svg.BeginAccessibleGroup("series", series.Label);
        return true;
    }

    /// <summary>Opens the clip a series' marks are drawn in: the plot area, when <paramref name="axes"/> has an explicit
    /// limit (<see cref="Axes.HasExplicitLimit"/>), because a set limit is exact and the data may reach past it. Opened
    /// inside the series' group and closed before whatever else belongs to the series is drawn (its end label stands
    /// right of the plot and must not be clipped). An axes whose range scaled itself encloses every series and gets no
    /// clip, so its output is unchanged.</summary>
    /// <param name="ctx">The context the series is drawn into.</param>
    /// <param name="axes">The axes that owns the series.</param>
    /// <param name="plotArea">The plot area the series is drawn in.</param>
    /// <returns><see langword="true"/> when a clip was pushed; the caller pops it right after the series' marks.</returns>
    internal static bool BeginSeriesClip(this IRenderContext ctx, Axes axes, Rect plotArea)
    {
        if (!axes.HasExplicitLimit) return false;
        ctx.PushClip(plotArea);
        return true;
    }
}
