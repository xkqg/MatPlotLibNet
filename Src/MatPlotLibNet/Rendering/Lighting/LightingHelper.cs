// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Lighting;

/// <summary>Static helpers for computing face normals. Color lighting operations are on
/// <see cref="MatPlotLibNet.Styling.ColorExtensions"/>.</summary>
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
}
