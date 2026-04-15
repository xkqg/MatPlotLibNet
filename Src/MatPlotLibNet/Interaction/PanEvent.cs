// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interaction;

/// <summary>A client-initiated pan that translates the target axes by a delta in data space.
/// Shape differs from <see cref="AxisRangeEvent"/> (delta vs absolute), so <see cref="PanEvent"/>
/// inherits directly from <see cref="FigureInteractionEvent"/> and supplies its own
/// <see cref="ApplyTo"/>. Null axis limits (unset / auto-ranged) result in a no-op: we cannot
/// translate a range that has not been materialised yet.</summary>
public sealed record PanEvent(
    string ChartId,
    int AxesIndex,
    double DxData,
    double DyData) : FigureInteractionEvent(ChartId, AxesIndex)
{
    /// <inheritdoc />
    public override void ApplyTo(Figure figure)
    {
        var axes = TargetAxes(figure);

        if (axes.XAxis.Min is double xMin && axes.XAxis.Max is double xMax)
        {
            axes.XAxis.Min = xMin + DxData;
            axes.XAxis.Max = xMax + DxData;
        }

        if (axes.YAxis.Min is double yMin && axes.YAxis.Max is double yMax)
        {
            axes.YAxis.Min = yMin + DyData;
            axes.YAxis.Max = yMax + DyData;
        }
    }
}
