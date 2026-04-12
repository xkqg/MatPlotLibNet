// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>Projects 3D data coordinates to 2D pixel coordinates using elevation and azimuth rotation.</summary>
public sealed class Projection3D
{
    private readonly double _cosEl, _sinEl, _cosAz, _sinAz;
    private readonly Rect _plotBounds;
    private readonly double _xMin, _xMax, _yMin, _yMax, _zMin, _zMax;
    private readonly double? _distance;

    public double Elevation { get; }

    public double Azimuth { get; }

    /// <summary>Camera distance for perspective projection. Null = orthographic. Clamped to minimum 2.0.</summary>
    public double? Distance => _distance;

    /// <summary>Initializes a new <see cref="Projection3D"/> with the given view angles and data bounds.</summary>
    /// <param name="elevation">Camera elevation above the XY plane in degrees.</param>
    /// <param name="azimuth">Camera azimuth rotation around the Z axis in degrees.</param>
    /// <param name="plotBounds">Pixel rectangle into which the projection maps.</param>
    /// <param name="xMin">Minimum X data value.</param>
    /// <param name="xMax">Maximum X data value.</param>
    /// <param name="yMin">Minimum Y data value.</param>
    /// <param name="yMax">Maximum Y data value.</param>
    /// <param name="zMin">Minimum Z data value.</param>
    /// <param name="zMax">Maximum Z data value.</param>
    /// <param name="distance">Camera distance for perspective. Null = orthographic. Values below 2.0 are clamped.</param>
    public Projection3D(double elevation, double azimuth, Rect plotBounds,
        double xMin, double xMax, double yMin, double yMax, double zMin, double zMax,
        double? distance = null)
    {
        Elevation = elevation;
        Azimuth = azimuth;
        _plotBounds = plotBounds;
        _xMin = xMin; _xMax = xMax; _yMin = yMin; _yMax = yMax; _zMin = zMin; _zMax = zMax;
        _distance = distance.HasValue ? Math.Max(2.0, distance.Value) : null;

        double elRad = elevation * Math.PI / 180;
        double azRad = azimuth * Math.PI / 180;
        _cosEl = Math.Cos(elRad); _sinEl = Math.Sin(elRad);
        _cosAz = Math.Cos(azRad); _sinAz = Math.Sin(azRad);
    }

    /// <summary>Normalizes data coordinates to the [−1, 1]³ cube.</summary>
    /// <remarks>Primarily called by 3-D series renderers; consumer code should prefer the renderer
    /// pipeline rather than calling this method directly.</remarks>
    public Normalized3DPoint Normalize(double x, double y, double z)
    {
        double nx = _xMax > _xMin ? 2 * (x - _xMin) / (_xMax - _xMin) - 1 : 0;
        double ny = _yMax > _yMin ? 2 * (y - _yMin) / (_yMax - _yMin) - 1 : 0;
        double nz = _zMax > _zMin ? 2 * (z - _zMin) / (_zMax - _zMin) - 1 : 0;
        return new(nx, ny, nz);
    }

    /// <summary>Projects a 3D point to 2D pixel coordinates.</summary>
    public Point Project(double x, double y, double z)
    {
        var n = Normalize(x, y, z);
        double nx = n.Nx, ny = n.Ny, nz = n.Nz;

        // Rotate by azimuth (around Z axis)
        double rx = nx * _cosAz - ny * _sinAz;
        double ry = nx * _sinAz + ny * _cosAz;
        double rz = nz;

        // Rotate by elevation (around X axis)
        double pz = ry * _sinEl + rz * _cosEl;

        // Perspective scale (if distance set)
        if (_distance.HasValue)
        {
            double viewDepth = ry * _cosEl - rz * _sinEl;
            double scale = _distance.Value / (_distance.Value - viewDepth);
            rx *= scale;
            pz *= scale;
        }

        // Map to screen
        double px = _plotBounds.X + _plotBounds.Width * (rx + 1) / 2;
        double pyScreen = _plotBounds.Y + _plotBounds.Height * (1 - (pz + 1) / 2);

        return new Point(px, pyScreen);
    }

    /// <summary>Returns a depth value for sorting (higher = further from viewer).</summary>
    public double Depth(double x, double y, double z)
    {
        var n = Normalize(x, y, z);
        double rx = n.Nx * _cosAz - n.Ny * _sinAz;
        double ry = n.Nx * _sinAz + n.Ny * _cosAz;
        return ry * _cosEl - n.Nz * _sinEl; // view-space Y = depth
    }
}
