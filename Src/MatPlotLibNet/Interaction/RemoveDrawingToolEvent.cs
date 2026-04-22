// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;

namespace MatPlotLibNet.Interaction;

/// <summary>Removes a drawing tool (trendline, level, or Fibonacci retracement) from the target axes.
/// Unknown tool types are silently ignored.</summary>
public sealed record RemoveDrawingToolEvent(
    string ChartId,
    int AxesIndex,
    object Tool) : FigureInteractionEvent(ChartId, AxesIndex)
{
    /// <inheritdoc />
    public override void ApplyTo(Figure figure)
    {
        var axes = TargetAxes(figure);
        switch (Tool)
        {
            case Trendline t:          axes.RemoveTrendline(t); break;
            case HorizontalLevel l:    axes.RemoveLevel(l); break;
            case FibonacciRetracement f: axes.RemoveFibonacci(f); break;
        }
    }
}
