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
}
