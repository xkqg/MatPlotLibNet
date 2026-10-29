// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Lighting;

/// <summary>A directional light source: a constant ambient term plus a diffuse term that grows as the face turns towards the
/// light. The intensity is <c>Ambient + Diffuse · (dot + 1) / 2</c>, held to [0, 1], where <c>dot</c> is the cosine of the angle
/// between the face's normal and the direction of the light: a face towards the light gets <c>Ambient + Diffuse</c>, one facing
/// away only <c>Ambient</c>, one edge-on the middle of the two. At the defaults (0.3 and 0.7) that is matplotlib's
/// <c>0.65 + 0.35 · dot</c>, with its two constants named, so a surface and a 3-D bar are shaded as matplotlib shades them and
/// the ambient and the diffuse change the picture when they are set.</summary>
/// <param name="Dx">X component of the light direction vector.</param>
/// <param name="Dy">Y component of the light direction vector.</param>
/// <param name="Dz">Z component of the light direction vector.</param>
/// <param name="Ambient">Ambient light intensity [0, 1]. Default 0.3.</param>
/// <param name="Diffuse">Diffuse light intensity [0, 1]. Default 0.7.</param>
public sealed record DirectionalLight(
    double Dx, double Dy, double Dz,
    double Ambient = 0.3, double Diffuse = 0.7) : ILightSource
{
    /// <inheritdoc />
    /// <remarks>A light or a face with no direction cannot shade anything, and has an intensity of 1: the colour as it is.</remarks>
    public double ComputeIntensity(double nx, double ny, double nz)
    {
        double lLen = Math.Sqrt(Dx * Dx + Dy * Dy + Dz * Dz);
        double nLen = Math.Sqrt(nx * nx + ny * ny + nz * nz);
        if (lLen < 1e-10 || nLen < 1e-10) return 1;
        double dot = (nx / nLen) * (Dx / lLen) + (ny / nLen) * (Dy / lLen) + (nz / nLen) * (Dz / lLen);
        return Math.Clamp(Ambient + Diffuse * (dot + 1) / 2, 0, 1);
    }

    /// <inheritdoc />
    /// <remarks>Each channel is the colour's times the intensity, rounded to the nearest step, as matplotlib's
    /// <c>_shade_colors</c> rounds; alpha is kept.</remarks>
    public Color Shade(Color color, double nx, double ny, double nz)
    {
        double k = ComputeIntensity(nx, ny, nz);
        return new Color((byte)Math.Round(color.R * k), (byte)Math.Round(color.G * k), (byte)Math.Round(color.B * k), color.A);
    }
}
