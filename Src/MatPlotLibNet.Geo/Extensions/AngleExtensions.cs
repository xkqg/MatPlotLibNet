// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo;

/// <summary>Degrees/radians conversion extensions on <see cref="double"/>. Introduced to DRY out
/// the <c>DegToRad</c>/<c>RadToDeg</c> private consts that were duplicated across the projection
/// files in <c>MatPlotLibNet.Geo.Projections</c> — pure formatting change, no behavioural difference.</summary>
public static class AngleExtensions
{
    // Precomputed single-constant factors (not an inline `x * Math.PI / 180.0` two-op
    // expression) so every call site folds to the same one-multiply IL the former
    // per-file `DegToRad`/`RadToDeg` consts produced — bit-identical output, pure DRY.
    private const double DegToRadFactor = Math.PI / 180.0;
    private const double RadToDegFactor = 180.0 / Math.PI;

    /// <summary>Converts an angle expressed in degrees to radians.</summary>
    /// <param name="degrees">The angle in degrees.</param>
    /// <returns>The equivalent angle in radians.</returns>
    public static double ToRadians(this double degrees) => degrees * DegToRadFactor;

    /// <summary>Converts an angle expressed in radians to degrees.</summary>
    /// <param name="radians">The angle in radians.</param>
    /// <returns>The equivalent angle in degrees.</returns>
    public static double ToDegrees(this double radians) => radians * RadToDegFactor;
}
