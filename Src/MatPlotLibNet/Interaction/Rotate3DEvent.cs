// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interaction;

/// <summary>Rotates a 3D axes camera by the specified azimuth and elevation deltas.
/// Mutates <see cref="Models.Axes.Azimuth"/> and <see cref="Models.Axes.Elevation"/>,
/// and nulls <see cref="Models.Axes.Projection"/> to force a fresh <see cref="Rendering.Projection3D"/>
/// rebuild on the next render pass.</summary>
public sealed record Rotate3DEvent(
    string ChartId,
    int AxesIndex,
    double DeltaAzimuth,
    double DeltaElevation) : FigureInteractionEvent(ChartId, AxesIndex)
{
    /// <inheritdoc />
    public override void ApplyTo(Figure figure)
    {
        var axes = TargetAxes(figure);
        axes.Azimuth += DeltaAzimuth;
        axes.Elevation = Math.Clamp(axes.Elevation + DeltaElevation, -90, 90);
        axes.Projection = null; // force rebuild
    }
}
