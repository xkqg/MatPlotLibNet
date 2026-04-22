// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Lighting;

/// <summary>Three-dimensional vector/point used by the lighting pipeline. Deconstructs into
/// <c>(X, Y, Z)</c>.</summary>
public readonly record struct Vec3(double X, double Y, double Z)
{
    /// <summary>Returns the unnormalized face normal of the triangle formed by vertices
    /// <paramref name="v0"/>, <paramref name="v1"/>, <paramref name="v2"/> (cross product
    /// <c>(v1 − v0) × (v2 − v0)</c>). Reversing vertex order flips the normal's direction.</summary>
    public static Vec3 FaceNormal(Vec3 v0, Vec3 v1, Vec3 v2)
    {
        double ax = v1.X - v0.X, ay = v1.Y - v0.Y, az = v1.Z - v0.Z;
        double bx = v2.X - v0.X, by = v2.Y - v0.Y, bz = v2.Z - v0.Z;
        return new Vec3(ay * bz - az * by, az * bx - ax * bz, ax * by - ay * bx);
    }
}
