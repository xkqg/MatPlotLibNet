// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo;

/// <summary>Degrees/radians conversion extensions on <see cref="double"/>, the one conversion every projection of this package reads:
/// one multiply by a precomputed factor. It is the package's own rule, not the core's two-operation <c>degrees * Math.PI / 180</c>: the
/// two differ in the last bit for about one input in four, and every place a map's JSON carries is held to this one.</summary>
public static class AngleExtensions
{
    private const double DegToRadFactor = Math.PI / 180.0;
    private const double RadToDegFactor = 180.0 / Math.PI;

    extension(double degrees)
    {
        /// <summary>Converts an angle expressed in degrees to radians.</summary>
        /// <returns>The equivalent angle in radians.</returns>
        public double ToRadians() => degrees * DegToRadFactor;
    }

    extension(double radians)
    {
        /// <summary>Converts an angle expressed in radians to degrees.</summary>
        /// <returns>The equivalent angle in degrees.</returns>
        public double ToDegrees() => radians * RadToDegFactor;
    }
}
