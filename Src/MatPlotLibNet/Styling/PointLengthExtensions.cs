// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>The one rule that turns a length in points into pixels. matplotlib states its widths and sizes in points (a line
/// width, a tick, a font size); this library draws in pixels, and takes a point as 100 / 72 of a pixel, the 100 dots per inch
/// matplotlib draws its pictures at.</summary>
internal static class PointLengthExtensions
{
    /// <summary>The pixels in a point: 100 / 72.</summary>
    internal const double PixelsPerPoint = 100.0 / 72.0;

    extension(double points)
    {
        /// <summary>This length, given in points, in pixels.</summary>
        internal double PointsInPixels => points * PixelsPerPoint;

        /// <summary>This length, given in pixels, in points: the inverse, for a value handed back in matplotlib's unit.</summary>
        internal double PixelsInPoints => points / PixelsPerPoint;
    }

    extension(double area)
    {
        /// <summary>This marker size, given as an area in points squared (matplotlib's scatter <c>s</c>), in points across: its
        /// square root. matplotlib scales the unit marker by that, so a circle is that many points across and a square that many
        /// points on a side. A negative or missing area gives no number, and no marker.</summary>
        internal double AreaAcrossInPoints => Math.Sqrt(area);
    }
}
