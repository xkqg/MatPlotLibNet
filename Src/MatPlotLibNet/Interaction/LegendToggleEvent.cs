// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>A click or keyboard activation on a legend entry that flips the <see cref="ChartSeries.Visible"/> flag of the series
/// it stands for, or the <see cref="Models.Patches.Patch.Visible"/> flag of the patch. <see cref="SeriesIndex"/> is the index the
/// legend entry carries: the series' position in <see cref="Axes.AllSeries"/> (the primary series, then the secondary-Y series,
/// then the secondary-X series), so an entry of a secondary line toggles that line; past the series, the number of series plus the
/// patch's position in <see cref="Axes.Patches"/>. No-op if the index is past both or the series is not a
/// <see cref="ChartSeries"/>; an event for a subplot the figure does not have is dropped.</summary>
/// <param name="ChartId">The chart the click came from.</param>
/// <param name="AxesIndex">The place of the axes in the figure.</param>
/// <param name="SeriesIndex">The entry's series, as <see cref="Axes.AllSeries"/> numbers them.</param>
public sealed record LegendToggleEvent(
    string ChartId,
    int AxesIndex,
    int SeriesIndex) : FigureInteractionEvent(ChartId, AxesIndex)
{
    /// <inheritdoc />
    public override void ApplyTo(Figure figure) => TryApplyTo(figure);

    /// <inheritdoc />
    internal override bool TryApplyTo(Figure figure) => OnTargetAxes(figure, axes =>
    {
        if (SeriesIndex < 0) return;
        int firstPatch = axes.FirstPatchSlot;
        if (SeriesIndex < firstPatch)
        {
            if (axes.AllSeries.ElementAtOrDefault(SeriesIndex) is ChartSeries series)
                series.Visible = !series.Visible;
        }
        else if (axes.Patches.ElementAtOrDefault(SeriesIndex - firstPatch) is { } patch)
        {
            patch.Visible = !patch.Visible;
        }
    });
}
