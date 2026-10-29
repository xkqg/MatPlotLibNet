// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;

namespace MatPlotLibNet.Interaction;

/// <summary>Adds a <see cref="Trendline"/> to the target axes.</summary>
public sealed record AddTrendlineEvent(
    string ChartId,
    int AxesIndex,
    Trendline Tool) : FigureInteractionEvent(ChartId, AxesIndex)
{
    /// <inheritdoc />
    public override void ApplyTo(Figure figure) => TryApplyTo(figure);

    /// <inheritdoc />
    internal override bool TryApplyTo(Figure figure)
    {
        if (!TryTargetAxes(figure, out var axes)) return false;
        axes.AddTrendline(Tool);
        return true;
    }
}
