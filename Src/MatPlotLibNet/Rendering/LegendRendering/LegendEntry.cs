// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.LegendRendering;

/// <summary>One row of an axes' legend: the series it names, the index that series has in the whole legend list,
/// and the text the legend prints for it.</summary>
/// <param name="Series">The series the entry stands for.</param>
/// <param name="OriginalIndex">The series' IDENTITY slot (<see cref="SeriesSlotExtensions.SlotIndex"/>): its place in
/// <see cref="Axes.AllSeries"/>. It is the one number the drawn group (<c>data-legend-index</c>) and the hit-test bounds
/// use, so a primary series keeps its own index and a series on any other axis continues where the series before it
/// stopped — unaffected by
/// <see cref="Models.Series.ChartSeries.ColorGroup"/>, so a click never crosses from one member of a group to
/// another.</param>
/// <param name="ColorSlotIndex">The series' COLOUR slot (<see cref="SeriesSlotExtensions.ColorSlotIndex"/>): the
/// swatch reads this, not <paramref name="OriginalIndex"/>, so a grouped series' swatch wears the colour its group
/// is actually drawn in.</param>
/// <param name="Label">The display label: the series label plus the legend-values suffix when
/// <see cref="Legend.LegendValues"/> is on.</param>
/// <param name="Swatch">How the entry's handle is drawn.</param>
internal readonly record struct LegendEntry(ISeries Series, int OriginalIndex, int ColorSlotIndex, string Label, LegendSwatch Swatch);

/// <summary>The single place that decides which series of an axes appear in its legend.</summary>
/// <remarks>The drawing (<c>AxesRenderer.RenderLegend</c>), the hit-test (<c>AxesRenderer.ComputeLegendBounds</c>)
/// and the margin reservation (<c>LegendMeasurer.MeasureBox</c>) all read this list, so they cannot disagree about
/// how many rows there are, in which order, or what they say.</remarks>
internal static class AxesLegendEntryExtensions
{
    extension(Axes axes)
    {
        /// <summary>Lists the legend entries of <paramref name="axes"/> in identity order (<see cref="Axes.AllSeries"/>):
        /// the series of every Y axis of the container (the primary's, then every right slot's, then every left slot's),
        /// then the series of the top X axis, an entry for each part of each that has one
        /// (<see cref="ChartSeries.LegendParts"/>, unless <c>IsListed</c> says its name is drawn elsewhere). The parts of one
        /// series carry the same index.</summary>
        /// <param name="directLabelsDrawn">Whether the renderer of these axes draws end-of-line labels
        /// (<c>AxesRenderer.DrawsDirectLabels</c>). Only then does a series on a Y axis that asked for one leave the
        /// legend; a top X axis series keeps its entry, since end labels are drawn for the lines of the Y axes only.</param>
        /// <returns>The entries in drawing order; empty when no series has an entry.</returns>
        internal IReadOnlyList<LegendEntry> LegendEntries(bool directLabelsDrawn)
        {
            var entries = new List<LegendEntry>();
            bool legendValues = axes.Legend.LegendValues;
            var colours = axes.ColorSlots();

            // Every trace belongs in the one legend, whatever axis measures it: a chart that draws several lines and names
            // only some reads as "the other lines have no name". An entry's index is its series' identity slot and its
            // swatch its colour slot, numbered by the rule the series passes draw with (SeriesSlotExtensions.SlotIndex).
            void Collect(SeriesAxisPair pair, int firstIndexInPair, IReadOnlyList<ISeries> series, bool endLabelled)
            {
                for (int i = 0; i < series.Count; i++)
                {
                    var one = series[i];
                    if (!IsListed(one, endLabelled)) continue;
                    int index = axes.SlotIndex(pair, firstIndexInPair + i);
                    foreach (var part in PartsOf(one))
                    {
                        if (string.IsNullOrEmpty(part.Label)) continue;
                        entries.Add(new LegendEntry(one, index, colours[index] + part.ColorSlotOffset,
                            AxesRenderer.BuildLegendDisplayLabel(part.Label, one, legendValues), part.Swatch));
                    }
                }
            }

            foreach (var seat in axes.YSlotSeats())
                Collect(seat.Pair, seat.FirstIndexInPair, seat.Slot.Series, directLabelsDrawn);
            Collect(SeriesAxisPair.SecondaryX, 0, axes.XSecondarySeries, endLabelled: false);
            return entries;
        }
    }

    /// <summary>The one rule for "this series is named in the legend" apart from its name: its name is not drawn somewhere
    /// else instead. Two kinds draw it elsewhere: a series that draws its own label as part of itself (a stat tile,
    /// <see cref="ChartSeries.DrawsOwnLabel"/>) never has an entry, and a series that asked for an end label leaves the legend
    /// only where an end label is drawn for it (<paramref name="endLabelled"/>), so on axes whose renderer draws none (polar,
    /// 3D), and on the top X axis, its name is never lost.</summary>
    private static bool IsListed(ISeries series, bool endLabelled)
    {
        if (series is ChartSeries { DrawsOwnLabel: true }) return false;
        return !(endLabelled && series is IHasLabelPlacement { LabelPlacement: SeriesLabelPlacement.LineEnd });
    }

    /// <summary>The entries a series has: what it says (<see cref="ChartSeries.LegendParts"/>), or for a series the library
    /// does not know, one entry under its label as a filled rectangle (an empty label draws no entry, whoever's it is).</summary>
    private static IReadOnlyList<LegendPart> PartsOf(ISeries series) =>
        series is ChartSeries chart
            ? chart.LegendParts
            : [new LegendPart(series.Label ?? string.Empty, LegendSwatch.Patch)];
}
