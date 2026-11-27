// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet;

/// <summary>The one answer to "this angle in the other unit": degrees to radians and back. A pie's start angle, a polar label's turn, a
/// marker's rotation, the elevation of a 3-D view, the phase of an indicator and the direction of a wind barb are each named in one unit and
/// drawn in the other, and each reads it here rather than typing the multiply by pi over 180.
/// <para>The conversion is the standard library's <c>double.DegreesToRadians</c> and <c>double.RadiansToDegrees</c>: <c>degrees * Math.PI /
/// 180</c>, two operations, what every site that used to type it computed, so no picture moves (held bit for bit). A multiply by a
/// precomputed <c>Math.PI / 180</c> differs from it in the last bit for about one input in four.</para></summary>
internal static class AngleExtensions
{
    extension(double degrees)
    {
        /// <summary>This angle, named in degrees, in radians.</summary>
        /// <returns>The angle in radians.</returns>
        internal double ToRadians() => double.DegreesToRadians(degrees);
    }

    extension(double radians)
    {
        /// <summary>This angle, named in radians, in degrees.</summary>
        /// <returns>The angle in degrees.</returns>
        internal double ToDegrees() => double.RadiansToDegrees(radians);
    }
}
