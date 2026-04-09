// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>Projects 3D data coordinates to 2D pixel coordinates using elevation and azimuth rotation.</summary>
public sealed class Projection3D
{
    private readonly double _cosEl, _sinEl, _cosAz, _sinAz;
    private readonly Rect _plotBounds;
    private readonly double _xMin, _xMax, _yMin, _yMax, _zMin, _zMax;

    /// <summary>Gets the elevation angle in degrees.</summary>
    public double Elevation { get; }

    /// <summary>Gets the azimuth angle in degrees.</summary>
    public double Azimuth { get; }

    public Projection3D(double elevation, double azimuth, Rect plotBounds,
        double xMin, double xMax, double yMin, double yMax, double zMin, double zMax)
    {
        Elevation = elevation;
        Azimuth = azimuth;
        _plotBounds = plotBounds;
        _xMin = xMin; _xMax = xMax; _yMin = yMin; _yMax = yMax; _zMin = zMin; _zMax = zMax;

        double elRad = elevation * Math.PI / 180;
        double azRad = azimuth * Math.PI / 180;
        _cosEl = Math.Cos(elRad); _sinEl = Math.Sin(elRad);
        _cosAz = Math.Cos(azRad); _sinAz = Math.Sin(azRad);
    }

    /// <summary>Projects a 3D point to 2D pixel coordinates.</summary>
    public Point Project(double x, double y, double z)
    {
        // Normalize to [-1, 1]
        double nx = _xMax > _xMin ? 2 * (x - _xMin) / (_xMax - _xMin) - 1 : 0;
        double ny = _yMax > _yMin ? 2 * (y - _yMin) / (_yMax - _yMin) - 1 : 0;
        double nz = _zMax > _zMin ? 2 * (z - _zMin) / (_zMax - _zMin) - 1 : 0;

        // Rotate by azimuth (around Z axis)
        double rx = nx * _cosAz - ny * _sinAz;
        double ry = nx * _sinAz + ny * _cosAz;
        double rz = nz;

        // Rotate by elevation (around X axis)
        double py = ry * _cosEl - rz * _sinEl;
        double pz = ry * _sinEl + rz * _cosEl;

        // Orthographic projection to 2D
        double px = _plotBounds.X + _plotBounds.Width * (rx + 1) / 2;
        double pyScreen = _plotBounds.Y + _plotBounds.Height * (1 - (pz + 1) / 2);

        return new Point(px, pyScreen);
    }

    /// <summary>Returns a depth value for sorting (higher = further from viewer).</summary>
    public double Depth(double x, double y, double z)
    {
        double nx = _xMax > _xMin ? 2 * (x - _xMin) / (_xMax - _xMin) - 1 : 0;
        double ny = _yMax > _yMin ? 2 * (y - _yMin) / (_yMax - _yMin) - 1 : 0;
        double nz = _zMax > _zMin ? 2 * (z - _zMin) / (_zMax - _zMin) - 1 : 0;
        double rx = nx * _cosAz - ny * _sinAz;
        double ry = nx * _sinAz + ny * _cosAz;
        return ry * _cosEl - nz * _sinEl; // view-space Y = depth
    }
}
