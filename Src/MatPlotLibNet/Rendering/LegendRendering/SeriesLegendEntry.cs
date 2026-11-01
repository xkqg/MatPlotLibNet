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
/// <param name="ColorSlotIndex">The series' COLOUR slot (<see cref="SeriesSlotExtensions.ColorSlotIndex"/>): the
/// swatch reads this, not <paramref name="OriginalIndex"/>, so a grouped series' swatch wears the colour its group
/// is actually drawn in.</param>
/// <param name="Label">The display label: the series label plus the legend-values suffix when
/// <see cref="Legend.LegendValues"/> is on.</param>
/// <param name="Swatch">How the row's handle is drawn.</param>
internal sealed record SeriesLegendEntry(ISeries Series, int OriginalIndex, int ColorSlotIndex, string Label, LegendSwatch Swatch)
    : LegendEntry(Label)
{
    /// <inheritdoc />
    /// <remarks>The series' identity slot (<see cref="OriginalIndex"/>).</remarks>
    internal override int? ToggleIndex => OriginalIndex;

    /// <inheritdoc />
    internal override void DrawSwatch(LegendRenderer legend, Rect swatch) => legend.DrawSwatchOf(this, swatch);

    /// <inheritdoc />
    /// <remarks>The label, the handle and the colour and stroke the colour slot gives the series.</remarks>
    internal override object LookUnder(Theme theme)
    {
        var slot = theme.SeriesSlot(ColorSlotIndex);
        return new Look(Label, Swatch, slot, slot.ColorOf(Series));
    }

    /// <summary>What a series row looks like once drawn.</summary>
    private readonly record struct Look(string Label, LegendSwatch Swatch, SeriesSlot Slot, Color Color);
}
