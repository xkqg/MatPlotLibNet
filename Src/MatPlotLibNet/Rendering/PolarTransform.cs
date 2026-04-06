// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

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
        _maxRadius = Math.Min(plotBounds.Width, plotBounds.Height) / 2 * 0.85;
        _rMax = rMax > 0 ? rMax : 1;
    }

    /// <summary>Gets the center X coordinate in pixels.</summary>
    public double CenterX => _cx;

    /// <summary>Gets the center Y coordinate in pixels.</summary>
    public double CenterY => _cy;

    /// <summary>Gets the maximum pixel radius.</summary>
    public double MaxRadius => _maxRadius;

    /// <summary>Converts polar coordinates to pixel coordinates.</summary>
    /// <param name="r">Radial distance (data units).</param>
    /// <param name="theta">Angle in radians (0 = right, counter-clockwise).</param>
    public Point PolarToPixel(double r, double theta)
    {
        double norm = Math.Min(r / _rMax, 1.0);
        double px = _cx + _maxRadius * norm * Math.Cos(theta);
        double py = _cy - _maxRadius * norm * Math.Sin(theta);
        return new Point(px, py);
    }
}
