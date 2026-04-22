// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;

namespace MatPlotLibNet.Interaction;

/// <summary>Adds a <see cref="FibonacciRetracement"/> overlay to the target axes.</summary>
public sealed record AddFibonacciRetracementEvent(
    string ChartId,
    int AxesIndex,
    FibonacciRetracement Tool) : FigureInteractionEvent(ChartId, AxesIndex)
{
    /// <inheritdoc />
    public override void ApplyTo(Figure figure) => TargetAxes(figure).AddFibonacci(Tool);
}
