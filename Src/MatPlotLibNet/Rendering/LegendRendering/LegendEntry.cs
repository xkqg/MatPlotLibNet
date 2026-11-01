// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.LegendRendering;

/// <summary>One row of a legend: the text it prints and the handle drawn before it. A row stands for a series
/// (<see cref="SeriesLegendEntry"/>) or for a patch (<see cref="PatchLegendEntry"/>). What differs between the two (the handle, the
/// index a click toggles, what makes two rows look alike) each kind says for itself, so the drawing, the hit-test, the margin
/// reservation and the legend of a figure read every row alike and never ask which kind it is.</summary>
/// <param name="Label">The text the legend prints for the row.</param>
internal abstract record LegendEntry(string Label)
{
    /// <summary>The series index a click on the row toggles: the number its drawn group carries (<c>data-legend-index</c>) and its
    /// hit-test bounds carry. <see langword="null"/> for a row a click toggles nothing with, which has neither.</summary>
    internal abstract int? ToggleIndex { get; }

    /// <summary>Draws the row's handle in <paramref name="swatch"/> through <paramref name="legend"/>, the one drawer of handles,
    /// which draws each kind of row its own way.</summary>
    /// <param name="legend">The legend being drawn.</param>
    /// <param name="swatch">The handle's box: matplotlib's handle length wide and handle height high.</param>
    internal abstract void DrawSwatch(LegendRenderer legend, Rect swatch);

    /// <summary>What the row looks like once drawn under <paramref name="theme"/>, compared by value: two rows whose looks are equal
    /// are the same row to the eye, which the legend of a figure lists once.</summary>
    /// <param name="theme">The theme the row is drawn under.</param>
    /// <returns>A value whose equality is the row's look.</returns>
    internal abstract object LookUnder(Theme theme);
}

/// <summary>The single place that decides which series and which patches of an axes have a row in its legend.</summary>
/// <remarks>The renderer of the axes joins the two (<see cref="AxesRenderer.LegendEntries(bool)"/>, series rows first) into the
/// one list the drawing (<c>AxesRenderer.RenderLegend</c>), the hit-test (<c>AxesRenderer.ComputeLegendBounds</c>), the margin
/// reservation (<c>LegendMeasurer.MeasureBox</c>) and the legend of the figure all read, so they cannot disagree about how many
/// rows there are, in which order, or what they say.</remarks>
internal static class AxesLegendEntryExtensions
{
    extension(Axes axes)
    {
        /// <summary>Lists the series rows of <paramref name="axes"/> in identity order (<see cref="Axes.AllSeries"/>):
        /// the series of every Y axis of the container (the primary's, then every right slot's, then every left slot's),
        /// then the series of the top X axis, a row for each part of each that has one
        /// (<see cref="ChartSeries.LegendParts"/>, unless <c>IsListed</c> says its name is drawn elsewhere). The parts of one
        /// series carry the same index.</summary>
        /// <param name="directLabelsDrawn">Whether the renderer of these axes draws end-of-line labels
        /// (<c>AxesRenderer.DrawsDirectLabels</c>). Only then does a series on a Y axis that asked for one leave the
        /// legend; a top X axis series keeps its row, since end labels are drawn for the lines of the Y axes only.</param>
        /// <returns>The rows in drawing order; empty when no series has a row.</returns>
        internal IReadOnlyList<SeriesLegendEntry> LegendEntries(bool directLabelsDrawn) =>
            axes.LegendEntries(directLabelsDrawn, axes.Legend.LegendValues);

        /// <summary>The series rows of <paramref name="axes"/> by the rule of <c>LegendEntries(bool)</c>, with the text of the
        /// rows (<see cref="Legend.LegendValues"/>) asked for by the caller instead of by the axes' own legend: the legend of
        /// a figure lists the rows of several axes under its own say.</summary>
        /// <param name="directLabelsDrawn">Whether the renderer of these axes draws end-of-line labels.</param>
        /// <param name="legendValues">Whether each label is suffixed with its series' last value.</param>
        /// <returns>The rows in drawing order; empty when no series has a row.</returns>
        internal IReadOnlyList<SeriesLegendEntry> LegendEntries(bool directLabelsDrawn, bool legendValues)
        {
            var entries = new List<SeriesLegendEntry>();
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
                        entries.Add(new SeriesLegendEntry(one, index, colours[index] + part.ColorSlotOffset,
                            AxesRenderer.BuildLegendDisplayLabel(part.Label, one, legendValues), part.Swatch));
                    }
                }
            }

            foreach (var seat in axes.YSlotSeats())
                Collect(seat.Pair, seat.FirstIndexInPair, seat.Slot.Series, directLabelsDrawn);
            Collect(SeriesAxisPair.SecondaryX, 0, axes.XSecondarySeries, endLabelled: false);
            return entries;
        }

        /// <summary>The patch rows of <paramref name="axes"/>, in the order the patches were added: one for each patch whose
        /// <see cref="Patch.Label"/> names it, matplotlib's rule (<c>_get_legend_handles_labels</c>): the label is neither missing
        /// nor empty and does not start with an underscore. Neither a patch's opacity nor its shape decides. A row prints the
        /// label as it is, with no legend value.</summary>
        /// <returns>The rows; empty when no patch has a row.</returns>
        internal IReadOnlyList<PatchLegendEntry> PatchLegendEntries()
        {
            List<PatchLegendEntry> entries = [];
            foreach (var patch in axes.Patches)
            {
                if (patch.Label is { Length: > 0 } label && label[0] != '_')
                {
                    entries.Add(new PatchLegendEntry(patch, label));
                }
            }

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
