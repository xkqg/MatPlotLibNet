// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Lighting;

/// <summary>A directional light source using Lambertian (diffuse) shading with a constant ambient term.</summary>
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
    public double ComputeIntensity(double nx, double ny, double nz)
    {
        double lLen = Math.Sqrt(Dx * Dx + Dy * Dy + Dz * Dz);
        double nLen = Math.Sqrt(nx * nx + ny * ny + nz * nz);
        if (lLen < 1e-10 || nLen < 1e-10) return Ambient + Diffuse;
        double dot = (nx / nLen) * (Dx / lLen) + (ny / nLen) * (Dy / lLen) + (nz / nLen) * (Dz / lLen);
        return Math.Clamp(Ambient + Diffuse * Math.Max(0, dot), 0, 1);
    }
}
