// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.LegendRendering;

/// <summary>The row of a series in a legend: the series it names, the index that series has in the whole legend list, the
/// colour slot its handle wears and the text the legend prints for it. A click on the row hides or shows the series.</summary>
/// <param name="Series">The series the row stands for.</param>
/// <param name="OriginalIndex">The series' IDENTITY slot (<see cref="SeriesSlotExtensions.SlotIndex"/>): its place in
/// <see cref="Axes.AllSeries"/>. It is the one number the drawn group (<c>data-legend-index</c>) and the hit-test bounds
/// use, so a primary series keeps its own index and a series on any other axis continues where the series before it
/// stopped — unaffected by
/// <see cref="ChartSeries.ColorGroup"/>, so a click never crosses from one member of a group to
/// another.</param>
/// <param name="ColorSlotIndex">The series' COLOUR slot (<see cref="SeriesSlotExtensions.ColorSlots"/>): the
/// swatch reads this, not <paramref name="OriginalIndex"/>, so a grouped series' swatch wears the colour its group
/// is actually drawn in.</param>
/// <param name="Label">The display label: the series label plus the legend-values suffix when
/// <see cref="Legend.LegendValues"/> is on.</param>
/// <param name="Swatch">How the row's handle is drawn.</param>
/// <param name="PartColor">The colour the series' part this row stands for is drawn in, when its caller named one
/// (<see cref="LegendPart.Color"/>); <see langword="null"/> draws the handle in the colour its slot gives the series.</param>
internal sealed record SeriesLegendEntry(ISeries Series, int OriginalIndex, int ColorSlotIndex, string Label, LegendSwatch Swatch,
    Color? PartColor = null)
    : LegendEntry(Label)
{
    /// <inheritdoc />
    /// <remarks>The series' identity slot (<see cref="OriginalIndex"/>).</remarks>
    internal override int ToggleIndex => OriginalIndex;

    /// <inheritdoc />
    internal override void DrawSwatch(LegendRenderer legend, Rect swatch, double fontSize) => legend.DrawSwatchOf(this, swatch, fontSize);

    /// <inheritdoc />
    /// <remarks>Another series row with the same label, the same handle, the same colour and stroke from its colour slot, the same
    /// colour of its handle (the part's own where it was named) and, for an edged handle, the same edge its series gives.</remarks>
    internal override bool LooksLike(LegendEntry other, Theme theme)
    {
        if (other is not SeriesLegendEntry row || row.Label != Label || row.Swatch != Swatch) return false;
        var slot = SlotUnder(theme);
        var rowSlot = row.SlotUnder(theme);
        return rowSlot == slot && row.HandleColorIn(rowSlot) == HandleColorIn(slot) && row.GivenEdge == GivenEdge;
    }

    /// <summary>The edge an edged handle reads from its series; none for another handle or a series that is no patch.</summary>
    private GivenPatchEdge GivenEdge => Swatch.Kind == LegendSwatchKind.EdgedPatch && Series is IEdgedPatch patch ? patch.GivenEdge : default;

    /// <summary>The slot the row's handle wears under <paramref name="theme"/>: that of its COLOUR slot (<see cref="ColorSlotIndex"/>), so a
    /// series sharing a colour group shows the colour it is drawn in.</summary>
    /// <param name="theme">The theme the legend is drawn under.</param>
    /// <returns>The slot.</returns>
    internal SeriesSlot SlotUnder(Theme theme) => theme.SeriesSlot(ColorSlotIndex);

    /// <summary>The colour the row's handle is drawn in within <paramref name="slot"/>: the colour of the part the row stands for when its
    /// caller named one (<see cref="PartColor"/>), else the colour the slot gives the series. The one rule the drawn handle and the row's
    /// look read.</summary>
    /// <param name="slot">The slot the handle wears (<see cref="SlotUnder"/>).</param>
    /// <returns>The colour of the handle.</returns>
    internal Color HandleColorIn(SeriesSlot slot) => PartColor ?? slot.ColorOf(Series);

    /// <summary>Lists the series rows of <paramref name="axes"/> in identity order (<see cref="Axes.AllSeries"/>):
    /// the series of every Y axis of the container (the primary's, then every right slot's, then every left slot's),
    /// then the series of the top X axis, a row for each part of each that has one
    /// (<see cref="ChartSeries.LegendPartsUnder"/>, unless <c>IsListed</c> says its name is drawn elsewhere). The parts of one
    /// series carry the same index.</summary>
    /// <param name="theme">The theme the axes is drawn under, which decides the colour slot of each series.</param>
    /// <param name="directLabelsDrawn">Whether the renderer of these axes draws end-of-line labels
    /// (<c>AxesRenderer.DrawsDirectLabels</c>). Only then does a series on a Y axis that asked for one leave the
    /// legend; a top X axis series keeps its row, since end labels are drawn for the lines of the Y axes only.</param>
    /// <param name="legendValues">Whether each label is suffixed with its series' last value: the axes' own legend asks for
    /// it (<see cref="Legend.LegendValues"/>), and the legend of a figure lists the rows of several axes under its own say.</param>
    /// <param name="colorSlots">The colour slot of each series of the axes (<see cref="SeriesSlotExtensions.ColorSlots"/>), made once
    /// by the renderer that draws the axes (<see cref="AxesRenderer.ColorSlots"/>) and read here, never made again for the rows.</param>
    /// <returns>The rows in drawing order; empty when no series has a row.</returns>
    /// <param name="axes">The <see cref="Axes"/> it works on.</param>
    internal static IReadOnlyList<SeriesLegendEntry> RowsOf(Axes axes, Theme theme, bool directLabelsDrawn, bool legendValues, int[] colorSlots)
    {
        var entries = new List<SeriesLegendEntry>();

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
                foreach (var part in PartsOf(one, theme))
                {
                    if (string.IsNullOrEmpty(part.Label)) continue;
                    entries.Add(new SeriesLegendEntry(one, index, colorSlots[index] + part.ColorSlotOffset,
                        one.LegendLabel(part.Label, legendValues), part.Swatch, part.Color));
                }
            }
        }

        foreach (var seat in axes.YSlotSeats())
            Collect(seat.Pair, seat.FirstIndexInPair, seat.Slot.Series, directLabelsDrawn);
        Collect(SeriesAxisPair.SecondaryX, 0, axes.XSecondarySeries, endLabelled: false);
        return entries;
    }

    /// <summary>The one rule for "this series is named in the legend" apart from its name: its name is not drawn somewhere
    /// else instead. Two kinds draw it elsewhere: a series that draws its own label as part of itself (a stat tile,
    /// <see cref="ChartSeries.DrawsOwnLabel"/>) never has an entry, and a series that asked for an end label leaves the legend
    /// only where an end label is drawn for it (<paramref name="endLabelled"/>, and a line the library can read,
    /// <see cref="IHasLinePoints"/>), so on axes whose renderer draws none (polar,
    /// 3D), and on the top X axis, its name is never lost.</summary>
    private static bool IsListed(ISeries series, bool endLabelled)
    {
        if (series is ChartSeries { DrawsOwnLabel: true }) return false;
        return !(endLabelled && series is IHasLinePoints { LabelPlacement: SeriesLabelPlacement.LineEnd });
    }

    /// <summary>The entries a series has under <paramref name="theme"/>: what it says (<see cref="ChartSeries.LegendPartsUnder"/>), or
    /// for a series the library does not know, one entry under its label as a filled rectangle (an empty label draws no entry,
    /// whoever's it is).</summary>
    private static IReadOnlyList<LegendPart> PartsOf(ISeries series, Theme theme) =>
        series is ChartSeries chart
            ? chart.LegendPartsUnder(theme)
            : [new LegendPart(series.Label ?? string.Empty, LegendSwatch.Patch)];
}
