// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>Transforms data coordinates to pixel coordinates and vice versa within a plot area.</summary>
public sealed class DataTransform
{
    private readonly double _xScale, _xOffset, _yScale, _yOffset;
    private readonly double _dataXMin, _dataXMax, _dataYMin, _dataYMax;
    private readonly Rect _plotBounds;

    /// <summary>Gets the minimum X value in data space.</summary>
    public double DataXMin => _dataXMin;

    /// <summary>Gets the maximum X value in data space.</summary>
    public double DataXMax => _dataXMax;

    /// <summary>Gets the minimum Y value in data space.</summary>
    public double DataYMin => _dataYMin;

    /// <summary>Gets the maximum Y value in data space.</summary>
    public double DataYMax => _dataYMax;

    /// <summary>Initializes a new transform mapping the specified data range onto the given pixel bounds.</summary>
    /// <param name="dataXMin">Minimum X value in data space.</param>
    /// <param name="dataXMax">Maximum X value in data space.</param>
    /// <param name="dataYMin">Minimum Y value in data space.</param>
    /// <param name="dataYMax">Maximum Y value in data space.</param>
    /// <param name="plotBounds">The pixel-space rectangle to map data onto.</param>
    public DataTransform(double dataXMin, double dataXMax, double dataYMin, double dataYMax, Rect plotBounds)
    {
        _dataXMin = dataXMin;
        _dataXMax = dataXMax;
        _dataYMin = dataYMin;
        _dataYMax = dataYMax;
        _plotBounds = plotBounds;

        double xRange = dataXMax - dataXMin;
        double yRange = dataYMax - dataYMin;

        _xScale = xRange == 0 ? 0 : plotBounds.Width / xRange;
        _xOffset = plotBounds.X - dataXMin * _xScale;

        _yScale = yRange == 0 ? 0 : plotBounds.Height / yRange;
        _yOffset = plotBounds.Y + plotBounds.Height + dataYMin * _yScale;
    }

    /// <summary>Converts data-space coordinates to pixel-space coordinates.</summary>
    /// <param name="x">The X value in data space.</param>
    /// <param name="y">The Y value in data space.</param>
    /// <returns>The corresponding pixel position.</returns>
    public Point DataToPixel(double x, double y)
    {
        double px = _xScale == 0
            ? _plotBounds.X + _plotBounds.Width / 2
            : x * _xScale + _xOffset;

        // Y is inverted: data-max maps to top, data-min maps to bottom
        double py = _yScale == 0
            ? _plotBounds.Y + _plotBounds.Height / 2
            : _yOffset - y * _yScale;

        return new Point(px, py);
    }

    /// <summary>Converts pixel-space coordinates back to data-space coordinates.</summary>
    /// <param name="pixel">The pixel-space point to convert.</param>
    /// <returns>The corresponding data-space point.</returns>
    public Point PixelToData(Point pixel)
    {
        double xRange = _dataXMax - _dataXMin;
        double yRange = _dataYMax - _dataYMin;

        double x = _dataXMin + (pixel.X - _plotBounds.X) / _plotBounds.Width * xRange;
        double y = _dataYMin + (1 - (pixel.Y - _plotBounds.Y) / _plotBounds.Height) * yRange;

        return new Point(x, y);
    }
}
