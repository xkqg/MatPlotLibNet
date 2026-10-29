// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>Transforms polar coordinates (r, theta) to pixel coordinates within a circular plot area.</summary>
public sealed class PolarTransform
{
    private readonly double _cx, _cy, _maxRadius, _rMax;

    /// <summary>Creates a polar transform for the given plot bounds and data range.</summary>
    public PolarTransform(Rect plotBounds, double rMax)
    {
        _cx = plotBounds.X + plotBounds.Width / 2;
        _cy = plotBounds.Y + plotBounds.Height / 2;
        _maxRadius = RadiusOf(plotBounds);
        _rMax = rMax > 0 ? rMax : 1;
    }

    /// <summary>The pixel radius of the outermost ring of a polar plot in <paramref name="plotBounds"/>.</summary>
    /// <param name="plotBounds">The plot area.</param>
    internal static double RadiusOf(Rect plotBounds) => Math.Min(plotBounds.Width, plotBounds.Height) / 2 * 0.85;

    /// <summary>Gets the center X coordinate in pixels.</summary>
    public double CenterX => _cx;

    /// <summary>Gets the center Y coordinate in pixels.</summary>
    public double CenterY => _cy;

    /// <summary>Gets the maximum pixel radius.</summary>
    public double MaxRadius => _maxRadius;

    /// <summary>Converts polar coordinates to pixel coordinates. A finite radius beyond rMax is clamped to the edge; a
    /// radius or an angle that cannot be placed (missing, or infinite) and a radius below zero, which reaches nowhere from
    /// the centre, map to a non-finite pixel, which no renderer draws, as a cartesian coordinate that cannot be placed does.</summary>
    /// <param name="r">Radial distance (data units).</param>
    /// <param name="theta">Angle in radians (0 = right, counter-clockwise).</param>
    public Point PolarToPixel(double r, double theta)
    {
        double norm = r.IsDrawable() && r >= 0 ? Math.Min(r / _rMax, 1.0) : double.NaN;
        double px = _cx + _maxRadius * norm * Math.Cos(theta);
        double py = _cy - _maxRadius * norm * Math.Sin(theta);
        return new Point(px, py);
    }
}
