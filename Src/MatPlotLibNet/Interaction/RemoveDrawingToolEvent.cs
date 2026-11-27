// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;

namespace MatPlotLibNet.Interaction;

/// <summary>Removes a drawing tool (trendline, level, or Fibonacci retracement) from the target axes.
/// Anything else, a reference line or a span drawn as a tool is included, is silently ignored.</summary>
public sealed record RemoveDrawingToolEvent(
    string ChartId,
    int AxesIndex,
    object Tool) : FigureInteractionEvent(ChartId, AxesIndex)
{
    /// <inheritdoc />
    public override void ApplyTo(Figure figure) => TryApplyTo(figure);

    /// <inheritdoc />
    internal override bool TryApplyTo(Figure figure) => OnTargetAxes(figure, axes => (Tool as IDrawingTool)?.RemoveFrom(axes.Overlays));
}
