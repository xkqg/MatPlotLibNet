// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interaction;

/// <summary>Rotates a 3D axes camera by the specified azimuth and elevation deltas: the camera's turn
/// (<see cref="Models.Axes.Azimuth"/> moves, <see cref="Models.Axes.Elevation"/> moves and is held between the poles, and
/// <see cref="Models.Axes.Projection"/> is dropped so the next render builds a fresh <see cref="Rendering.Projection3D"/>).</summary>
public sealed record Rotate3DEvent(
    string ChartId,
    int AxesIndex,
    double DeltaAzimuth,
    double DeltaElevation) : FigureInteractionEvent(ChartId, AxesIndex)
{
    /// <inheritdoc />
    internal override bool ChangesView => true;

    /// <inheritdoc />
    public override void ApplyTo(Figure figure) => TryApplyTo(figure);

    /// <inheritdoc />
    internal override bool TryApplyTo(Figure figure) => OnTargetAxes(figure, axes =>
    {
        figure.Home.Remember(axes);
        axes.Camera.Turn(DeltaAzimuth, DeltaElevation);
    });
}
