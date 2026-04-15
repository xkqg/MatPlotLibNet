// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Interaction;

/// <summary>A click or keyboard activation on a legend entry that flips the target series'
/// <see cref="ChartSeries.Visible"/> flag. No-op if the index is out of range or the series
/// is not a <see cref="ChartSeries"/>.</summary>
public sealed record LegendToggleEvent(
    string ChartId,
    int AxesIndex,
    int SeriesIndex) : FigureInteractionEvent(ChartId, AxesIndex)
{
    /// <inheritdoc />
    public override void ApplyTo(Figure figure)
    {
        var axes = TargetAxes(figure);
        if (SeriesIndex < 0 || SeriesIndex >= axes.Series.Count)
            return;

        if (axes.Series[SeriesIndex] is ChartSeries series)
            series.Visible = !series.Visible;
    }
}
