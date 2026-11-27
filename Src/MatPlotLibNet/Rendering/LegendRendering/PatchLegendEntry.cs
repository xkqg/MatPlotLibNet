// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.LegendRendering;

/// <summary>The row of a labelled patch in a legend, as matplotlib lists one: its label and a rectangle the full handle long and
/// high in the patch's own style (<see cref="Patch.DrawnStyleUnder"/>: face, edge, width, dash and hatch, the opacity inside each
/// colour), whatever the shape. A click on it hides or shows the patch (<see cref="Patch.Visible"/>), as a click on a series' row
/// does the series.</summary>
/// <param name="Patch">The patch the row stands for.</param>
/// <param name="Label">The patch's label, which names it in the legend: never empty (<see cref="Patch.HasLegendRow"/>).</param>
/// <param name="Index">The index the row toggles: the number of series of its axes plus the patch's place in its axes'
/// patches, which the patch's drawn group carries in an interactive picture.</param>
internal sealed record PatchLegendEntry(Patch Patch, string Label, int Index) : LegendEntry(Label)
{
    /// <inheritdoc />
    /// <remarks>The patch's <see cref="Index"/>.</remarks>
    internal override int ToggleIndex => Index;

    /// <inheritdoc />
    internal override void DrawSwatch(LegendRenderer legend, Rect swatch, double fontSize) => legend.DrawSwatchOf(this, swatch);

    /// <inheritdoc />
    /// <remarks>Another patch row with the same label whose patch is drawn in the same style.</remarks>
    internal override bool LooksLike(LegendEntry other, Theme theme) =>
        other is PatchLegendEntry row && row.Label == Label && row.Patch.DrawnStyleUnder(theme) == Patch.DrawnStyleUnder(theme);

    /// <summary><paramref name="seriesRows"/> with a row for each patch of <paramref name="axes"/> whose label names it
    /// (<see cref="Patch.HasLegendRow"/>; neither its opacity nor its shape decides), each among the rows of the axes' own series
    /// where it was added (<see cref="AxesOverlays.SeriesBeforePatch"/>), as matplotlib lists the artists of an axes in the order they
    /// were added: before the row of the first series added after it, and before the rows of the series of a second Y axis and
    /// of the top X axis, which stand after the axes' own. A patch row prints its label as it is, with no legend value, and
    /// toggles the slot after every series of the axes (<c>FirstPatchSlot</c>) plus its place in
    /// <see cref="Axes.Patches"/>.</summary>
    /// <param name="seriesRows">The series rows (<see cref="SeriesLegendEntry.RowsOf"/>), in identity order.</param>
    /// <returns>The rows in the order they are listed; <paramref name="seriesRows"/> itself when the axes has no patch.</returns>
    /// <param name="axes">The <see cref="Axes"/> it works on.</param>
    internal static IReadOnlyList<LegendEntry> Among(Axes axes, IReadOnlyList<SeriesLegendEntry> seriesRows)
    {
        var patches = axes.Patches;
        int first = 0;
        while (first < patches.Count && !patches[first].HasLegendRow) first++;
        if (first == patches.Count) return seriesRows;

        int firstIndex = axes.FirstPatchSlot;
        var rows = new List<LegendEntry>(seriesRows.Count + patches.Count - first);
        int next = 0;
        for (int i = first; i < patches.Count; i++)
        {
            var patch = patches[i];
            if (!patch.HasLegendRow) continue;
            int seriesBefore = axes.Overlays.SeriesBeforePatch(i);
            while (next < seriesRows.Count && seriesRows[next].OriginalIndex < seriesBefore) rows.Add(seriesRows[next++]);
            rows.Add(new PatchLegendEntry(patch, patch.Label!, firstIndex + i));
        }

        while (next < seriesRows.Count) rows.Add(seriesRows[next++]);
        return rows;
    }
}
