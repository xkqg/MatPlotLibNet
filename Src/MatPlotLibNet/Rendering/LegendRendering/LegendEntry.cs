// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.LegendRendering;

/// <summary>One row of an axes' legend: the series it names, the index that series has in the whole legend list,
/// and the text the legend prints for it.</summary>
/// <param name="Series">The series the entry stands for.</param>
/// <param name="OriginalIndex">The series' IDENTITY slot (<see cref="SeriesSlotExtensions.SlotIndex"/>): its index
/// across <see cref="Axes.Series"/> followed by <see cref="Axes.SecondarySeries"/>. It is the one number the drawn
/// group (<c>data-legend-index</c>) and the hit-test bounds use, so a primary series keeps its own index and a
/// secondary one continues where the primary series stopped — unaffected by
/// <see cref="Models.Series.ChartSeries.ColorGroup"/>, so a click never crosses from one member of a group to
/// another.</param>
/// <param name="ColorSlotIndex">The series' COLOUR slot (<see cref="SeriesSlotExtensions.ColorSlotIndex"/>): the
/// swatch reads this, not <paramref name="OriginalIndex"/>, so a grouped series' swatch wears the colour its group
/// is actually drawn in.</param>
/// <param name="Label">The display label: the series label plus the legend-values suffix when
/// <see cref="Legend.LegendValues"/> is on.</param>
internal readonly record struct LegendEntry(ISeries Series, int OriginalIndex, int ColorSlotIndex, string Label);

/// <summary>The single place that decides which series of an axes appear in its legend.</summary>
/// <remarks>The drawing (<c>AxesRenderer.RenderLegend</c>), the hit-test (<c>AxesRenderer.ComputeLegendBounds</c>)
/// and the margin reservation (<c>LegendMeasurer.MeasureBox</c>) all read this list, so they cannot disagree about
/// how many rows there are, in which order, or what they say.</remarks>
internal static class AxesLegendEntryExtensions
{
    extension(Axes axes)
    {
        /// <summary>Lists the legend entries of <paramref name="axes"/>: the primary series, then the secondary-axis
        /// series, each one that has a legend entry (see <c>HasLegendEntry</c>).</summary>
        /// <param name="directLabelsDrawn">Whether the renderer of these axes draws end-of-line labels
        /// (<c>AxesRenderer.DrawsDirectLabels</c>). Only then does a series that asked for one leave the legend.</param>
        /// <returns>The entries in drawing order; empty when no series has an entry.</returns>
        internal IReadOnlyList<LegendEntry> LegendEntries(bool directLabelsDrawn)
        {
            var entries = new List<LegendEntry>();
            bool legendValues = axes.Legend.LegendValues;
            Collect(axes, SeriesAxisPair.Primary, axes.Series, legendValues, directLabelsDrawn, entries);
            // The SECONDARY-axis traces belong in the same legend: a dual-axis chart draws two lines, and a legend that
            // names one of them reads as "the other line has no name". Their index is their colour slot, which continues
            // where the primary series stopped (SeriesSlotExtensions.SlotIndex, the rule the secondary part draws with).
            Collect(axes, SeriesAxisPair.SecondaryY, axes.SecondarySeries, legendValues, directLabelsDrawn, entries);
            return entries;
        }
    }

    private static void Collect(Axes axes, SeriesAxisPair pair, IReadOnlyList<ISeries> series, bool legendValues,
                                bool directLabelsDrawn, List<LegendEntry> entries)
    {
        for (int i = 0; i < series.Count; i++)
        {
            var one = series[i];
            if (!HasLegendEntry(one, directLabelsDrawn, out var label)) continue;
            entries.Add(new LegendEntry(one, axes.SlotIndex(pair, i), axes.ColorSlotIndex(pair, i),
                AxesRenderer.BuildLegendDisplayLabel(label, one, legendValues)));
        }
    }

    /// <summary>The one rule for "this series is named in the legend": it carries a label, and its name is not drawn
    /// somewhere else instead. Two kinds draw it elsewhere: a series that draws its own label as part of itself (a
    /// stat tile, <see cref="ChartSeries.DrawsOwnLabel"/>) never has an entry, and a series that asked for an end label
    /// leaves the legend only where the renderer draws that label, so on axes whose renderer draws none (polar, 3D)
    /// its name is never lost. Hands back the label so a caller never has to re-check it for null.</summary>
    private static bool HasLegendEntry(ISeries series, bool directLabelsDrawn, [NotNullWhen(true)] out string? label)
    {
        label = series.Label;
        if (string.IsNullOrEmpty(label)) return false;
        if (series is ChartSeries { DrawsOwnLabel: true }) return false;
        return !(directLabelsDrawn && series is IHasLabelPlacement { LabelPlacement: SeriesLabelPlacement.LineEnd });
    }
}
