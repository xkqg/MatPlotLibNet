// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Interaction;

/// <summary>A click or keyboard activation on a legend entry that flips the target series'
/// <see cref="ChartSeries.Visible"/> flag. <see cref="SeriesIndex"/> is the index the legend entry carries: the
/// series' position in <see cref="Axes.AllSeries"/> (the primary series, then the secondary-Y series, then the
/// secondary-X series), so an entry of a secondary line toggles that line. No-op if the index is out of range or the
/// series is not a <see cref="ChartSeries"/>; an event for a subplot the figure does not have is dropped.</summary>
public sealed record LegendToggleEvent(
    string ChartId,
    int AxesIndex,
    int SeriesIndex) : FigureInteractionEvent(ChartId, AxesIndex)
{
    /// <inheritdoc />
    public override void ApplyTo(Figure figure) => TryApplyTo(figure);

    /// <inheritdoc />
    internal override bool TryApplyTo(Figure figure)
    {
        if (!TryTargetAxes(figure, out var axes)) return false;
        if (SeriesIndex >= 0 && axes.AllSeries.ElementAtOrDefault(SeriesIndex) is ChartSeries series)
            series.Visible = !series.Visible;
        return true;
    }
}
