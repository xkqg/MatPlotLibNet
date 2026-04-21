// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Extension methods for color lighting operations on <see cref="Color"/>.</summary>
public static class ColorExtensions
{
    /// <summary>Shades a color using matplotlib's exact <c>_shade_colors</c> formula:
    /// <c>k = clamp(0.65 + 0.35·dot(n̂, l̂), 0.3, 1.0)</c>. Returns <paramref name="color"/>
    /// unchanged when either normal or light vector has near-zero length.</summary>
    public static Color Shade(this Color color,
        double nx, double ny, double nz,
        double lx, double ly, double lz)
    {
        double nLen = Math.Sqrt(nx * nx + ny * ny + nz * nz);
        double lLen = Math.Sqrt(lx * lx + ly * ly + lz * lz);
        if (nLen < 1e-10 || lLen < 1e-10) return color;
        double dot = (nx * lx + ny * ly + nz * lz) / (nLen * lLen);
        double k = Math.Clamp(0.65 + 0.35 * dot, 0.3, 1.0);
        return new Color(
            (byte)Math.Round(color.R * k),
            (byte)Math.Round(color.G * k),
            (byte)Math.Round(color.B * k),
            color.A);
    }

    /// <summary>Scales a color's RGB channels by <paramref name="intensity"/> (clamped to [0,1]),
    /// preserving alpha.</summary>
    public static Color Modulate(this Color color, double intensity)
    {
        intensity = Math.Clamp(intensity, 0, 1);
        return new Color(
            (byte)(color.R * intensity),
            (byte)(color.G * intensity),
            (byte)(color.B * intensity),
            color.A);
    }
}
