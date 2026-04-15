// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Lighting;

/// <summary>Static helpers for computing face normals and modulating colors by light intensity.</summary>
public static class LightingHelper
{
    /// <summary>Computes the cross-product face normal for a triangle defined by three 3D vertices.</summary>
    public static (double Nx, double Ny, double Nz) ComputeFaceNormal(
        (double X, double Y, double Z) v0,
        (double X, double Y, double Z) v1,
        (double X, double Y, double Z) v2)
    {
        double ax = v1.X - v0.X, ay = v1.Y - v0.Y, az = v1.Z - v0.Z;
        double bx = v2.X - v0.X, by = v2.Y - v0.Y, bz = v2.Z - v0.Z;
        return (ay * bz - az * by, az * bx - ax * bz, ax * by - ay * bx);
    }

    /// <summary>Scales a color's RGB channels by <paramref name="intensity"/>, preserving alpha.</summary>
    public static Color ModulateColor(Color color, double intensity)
    {
        intensity = Math.Clamp(intensity, 0, 1);
        return new Color(
            (byte)(color.R * intensity),
            (byte)(color.G * intensity),
            (byte)(color.B * intensity),
            color.A);
    }

    /// <summary>
    /// Shades a base colour using matplotlib's exact <c>mpl_toolkits.mplot3d.art3d._shade_colors</c>
    /// formula: <c>k = 0.65 + 0.35·dot(n̂, l̂)</c>, mapping the raw dot product from [−1, 1] to
    /// [0.3, 1.0]. No Lambertian <c>max(0, dot)</c> clamp — back-facing faces get 0.3× brightness
    /// (matplotlib's ambient floor) while front-facing faces get 1.0×. Hue is preserved.
    /// Both <paramref name="nx"/>/<paramref name="ny"/>/<paramref name="nz"/> and the light
    /// direction are normalised internally.
    /// </summary>
    public static Color ShadeColor(Color baseColor,
        double nx, double ny, double nz,
        double lx, double ly, double lz)
    {
        double nLen = Math.Sqrt(nx * nx + ny * ny + nz * nz);
        double lLen = Math.Sqrt(lx * lx + ly * ly + lz * lz);
        if (nLen < 1e-10 || lLen < 1e-10) return baseColor;
        double dot = (nx * lx + ny * ly + nz * lz) / (nLen * lLen);
        double k = Math.Clamp(0.65 + 0.35 * dot, 0.3, 1.0);
        return new Color(
            (byte)Math.Round(baseColor.R * k),
            (byte)Math.Round(baseColor.G * k),
            (byte)Math.Round(baseColor.B * k),
            baseColor.A);
    }
}
