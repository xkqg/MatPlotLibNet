// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Lighting;

/// <summary>A directional light source: a constant ambient term plus a diffuse term that grows as the face turns towards the
/// light. The intensity is <c>Ambient + Diffuse · (dot + 1) / 2</c>, held to [0, 1], where <c>dot</c> is the cosine of the angle
/// between the face's normal and the direction of the light: a face towards the light gets <c>Ambient + Diffuse</c>, one facing
/// away only <c>Ambient</c>, one edge-on the middle of the two. At the defaults (<see cref="RestingAmbient"/> and <see cref="RestingDiffuse"/>) that is matplotlib's
/// <c>0.65 + 0.35 · dot</c>, with its two constants named, so a surface and a 3-D bar are shaded as matplotlib shades them and
/// the ambient and the diffuse change the picture when they are set.</summary>
/// <param name="Dx">X component of the light direction vector.</param>
/// <param name="Dy">Y component of the light direction vector.</param>
/// <param name="Dz">Z component of the light direction vector.</param>
/// <param name="Ambient">Ambient light intensity [0, 1]. Default <see cref="RestingAmbient"/>.</param>
/// <param name="Diffuse">Diffuse light intensity [0, 1]. Default <see cref="RestingDiffuse"/>.</param>
public sealed record DirectionalLight(
    double Dx, double Dy, double Dz,
    double Ambient = DirectionalLight.RestingAmbient, double Diffuse = DirectionalLight.RestingDiffuse) : ILightSource
{
    /// <summary>The ambient intensity a light is made with when none is given; with <see cref="RestingDiffuse"/> it shades as
    /// matplotlib's <c>0.65 + 0.35 · dot</c>.</summary>
    internal const double RestingAmbient = 0.3;

    /// <summary>The diffuse intensity a light is made with when none is given.</summary>
    internal const double RestingDiffuse = 0.7;

    /// <summary>The light a 3-D bar is shaded by when none is given: matplotlib's <c>bar3d</c> shades by default, and
    /// <c>art3d._shade_colors</c> then lights it from <c>LightSource(azdeg=225, altdeg=19.4712)</c>, an azimuth clockwise from
    /// north and an altitude above the horizon, turned into the unit vector towards the light as
    /// <c>LightSource.direction</c> turns it.</summary>
    internal static DirectionalLight MatplotlibDefault { get; } = FromAzimuthAltitude(225, 19.4712);

    private static DirectionalLight FromAzimuthAltitude(double azimuthDegrees, double altitudeDegrees)
    {
        double az = (90 - azimuthDegrees).ToRadians(), alt = altitudeDegrees.ToRadians();
        return new(Math.Cos(az) * Math.Cos(alt), Math.Sin(az) * Math.Cos(alt), Math.Sin(alt));
    }

    /// <inheritdoc />
    /// <remarks>A light or a face with no direction cannot shade anything, and has an intensity of 1: the colour as it is.</remarks>
    public double ComputeIntensity(double nx, double ny, double nz)
    {
        double lLen = new Vec3(Dx, Dy, Dz).Length;
        double nLen = new Vec3(nx, ny, nz).Length;
        if (lLen < 1e-10 || nLen < 1e-10) return 1;
        double dot = (nx / nLen) * (Dx / lLen) + (ny / nLen) * (Dy / lLen) + (nz / nLen) * (Dz / lLen);
        return (Ambient + Diffuse * (dot + 1) / 2).Saturate();
    }

    /// <inheritdoc />
    /// <remarks>The one rule of every light: each channel times the intensity at the nearest step,
    /// as matplotlib's <c>_shade_colors</c> and its writers round; alpha is kept.</remarks>
    public Color Shade(Color color, double nx, double ny, double nz) => this.ShadeAtIntensity(color, nx, ny, nz);
}
