// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Lighting;

namespace MatPlotLibNet.Models;

/// <summary>Where a 3-D axes is seen from and what it is lit by: the camera's elevation, azimuth and distance, a projection handed in
/// that overrides the angles until the camera moves, and the light on its faces. The one owner of where a 3-D camera stands at rest
/// (<see cref="RestingElevation"/>, <see cref="RestingAzimuth"/>, <see cref="RestingDistance"/>, matplotlib's default view) and of how
/// it moves: a turn (<see cref="Turn"/>), a return to a view kept before (<see cref="LookFrom"/>), and the view a draw is made from
/// (<see cref="DrawnView"/>). The camera members of <see cref="Axes"/> forward to it.</summary>
internal sealed class Camera3D : IChangeTracked
{
    private long _stamp;

    /// <summary>Stores <paramref name="value"/> and marks the camera changed, unless it already holds an equal value.</summary>
    private void Change<T>(ref T slot, T value) => ChangeClock.Change(ref slot, value, ref _stamp);

    /// <inheritdoc />
    public long LatestChange => _stamp;

    /// <summary>The camera's elevation above the XY plane, in degrees, when it is told none: matplotlib's <c>elev</c>.</summary>
    internal const double RestingElevation = 30;

    /// <summary>The camera's azimuth round the Z axis, in degrees, when it is told none: matplotlib's <c>azim</c>.</summary>
    internal const double RestingAzimuth = -60;

    /// <summary>The camera's distance from the centre of the box when it is told none: matplotlib's <c>Axes3D._dist</c>.</summary>
    internal const double RestingDistance = 10;

    /// <summary>The camera's elevation above the XY plane, in degrees.</summary>
    internal double Elevation { get; set => Change(ref field, value); } = RestingElevation;

    /// <summary>The camera's azimuth round the Z axis, in degrees.</summary>
    internal double Azimuth { get; set => Change(ref field, value); } = RestingAzimuth;

    /// <summary>The camera's distance from the centre of the box, at least 2 when drawn; <see langword="null"/> stands it at
    /// <see cref="RestingDistance"/>.</summary>
    internal double? Distance { get; set => Change(ref field, value); }

    /// <summary>A projection handed in, whose angles win over <see cref="Elevation"/> and <see cref="Azimuth"/> until the camera moves;
    /// <see langword="null"/> builds the projection from the camera's own angles.</summary>
    internal Projection3D? Projection { get; set => Change(ref field, value); }

    /// <summary>The light that shades the faces of the scene; <see langword="null"/> for the light each kind is lit by at rest.</summary>
    internal ILightSource? Light { get; set => Change(ref field, value); }

    /// <summary>Where the camera stands: its own angles and distance.</summary>
    internal CameraView View => new(Elevation, Azimuth, Distance);

    /// <summary>The view a draw is made from: the angles of a projection handed in when there is one, the camera's own otherwise, at
    /// the camera's distance.</summary>
    internal CameraView DrawnView => new(Projection?.Elevation ?? Elevation, Projection?.Azimuth ?? Azimuth, Distance);

    /// <summary>Turns the camera by <paramref name="azimuth"/> round the Z axis and <paramref name="elevation"/> above the plane, the
    /// elevation held between the poles (-90 to 90), and drops a projection handed in, so the next draw is made from the turned camera.</summary>
    /// <param name="azimuth">The turn round the Z axis, in degrees.</param>
    /// <param name="elevation">The turn above the XY plane, in degrees.</param>
    internal void Turn(double azimuth, double elevation)
    {
        Azimuth += azimuth;
        Elevation = Math.Clamp(Elevation + elevation, -90, 90);
        Projection = null;
    }

    /// <summary>Puts the camera at <paramref name="view"/> (a view kept before a gesture moved it) and drops a projection handed in.</summary>
    /// <param name="view">The view to look from.</param>
    internal void LookFrom(CameraView view)
    {
        Azimuth = view.Azimuth;
        Elevation = view.Elevation;
        Distance = view.Distance;
        Projection = null;
    }
}

/// <summary>Where a 3-D camera stands: its elevation and azimuth in degrees and its distance from the centre of the box
/// (<see langword="null"/> for <see cref="Camera3D.RestingDistance"/>).</summary>
/// <param name="Elevation">The elevation above the XY plane, in degrees.</param>
/// <param name="Azimuth">The azimuth round the Z axis, in degrees.</param>
/// <param name="Distance">The distance from the centre of the box; <see langword="null"/> for the resting one.</param>
internal readonly record struct CameraView(double Elevation, double Azimuth, double? Distance)
{
    /// <summary>The view of a camera told nothing: matplotlib's default view.</summary>
    internal static CameraView Resting => new(Camera3D.RestingElevation, Camera3D.RestingAzimuth, null);

    /// <summary>The projection from this view of <paramref name="box"/> into <paramref name="plot"/>: the one place a projection of a
    /// scene is built from a camera.</summary>
    /// <param name="plot">The pixel rectangle the scene is drawn in.</param>
    /// <param name="box">The data box of the scene.</param>
    /// <returns>The projection.</returns>
    internal Projection3D ProjectionOver(Rect plot, Box3D box) =>
        new(Elevation, Azimuth, plot, box.X.Lo, box.X.Hi, box.Y.Lo, box.Y.Hi, box.Z.Lo, box.Z.Hi, Distance);
}
