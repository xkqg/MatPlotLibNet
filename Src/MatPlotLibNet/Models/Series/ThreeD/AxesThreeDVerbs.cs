// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The 3D-family verbs of <see cref="Axes"/> (<c>Surface</c>, <c>Scatter3D</c>, <c>Bar3D</c>, …), each
/// constructing one series, setting <see cref="Axes.CoordinateSystem"/> to <see cref="CoordinateSystem.ThreeD"/>,
/// and handing the series to <see cref="Axes.AddSeries{T}"/>.</summary>
public static class AxesThreeDVerbs
{
    extension(Axes axes)
    {
        /// <summary>Adds a 3D surface series and sets coordinate system to ThreeD.</summary>
        public SurfaceSeries Surface(double[] x, double[] y, double[,] z)
        {
            z.ThrowIfMisaligned(y.Length, x.Length);
            axes.CoordinateSystem = CoordinateSystem.ThreeD;
            return axes.AddSeries(new SurfaceSeries(x, y, z));
        }

        /// <summary>Adds a 3D wireframe series and sets coordinate system to ThreeD.</summary>
        public WireframeSeries Wireframe(double[] x, double[] y, double[,] z)
        {
            z.ThrowIfMisaligned(y.Length, x.Length);
            axes.CoordinateSystem = CoordinateSystem.ThreeD;
            return axes.AddSeries(new WireframeSeries(x, y, z));
        }

        /// <summary>Adds a 3D scatter series and sets coordinate system to ThreeD.</summary>
        public Scatter3DSeries Scatter3D(double[] x, double[] y, double[] z)
        {
            x.Length.ThrowIfMisaligned(y.Length);
            x.Length.ThrowIfMisaligned(z.Length);
            axes.CoordinateSystem = CoordinateSystem.ThreeD;
            return axes.AddSeries(new Scatter3DSeries(x, y, z));
        }

        /// <summary>Adds a 3D stem series and sets coordinate system to ThreeD.</summary>
        public Stem3DSeries Stem3D(Numerics.Vec x, Numerics.Vec y, Numerics.Vec z)
        {
            x.Length.ThrowIfMisaligned(y.Length);
            x.Length.ThrowIfMisaligned(z.Length);
            axes.CoordinateSystem = CoordinateSystem.ThreeD;
            return axes.AddSeries(new Stem3DSeries(x, y, z));
        }

        /// <summary>Adds a 3D bar series and sets coordinate system to ThreeD.</summary>
        public Bar3DSeries Bar3D(Numerics.Vec x, Numerics.Vec y, Numerics.Vec z)
        {
            x.Length.ThrowIfMisaligned(y.Length);
            x.Length.ThrowIfMisaligned(z.Length);
            axes.CoordinateSystem = CoordinateSystem.ThreeD;
            return axes.AddSeries(new Bar3DSeries(x, y, z));
        }

        /// <summary>Adds a planar 3D bar series (flat translucent rectangles in Y-planes —
        /// matplotlib's "2D bars in different planes" pattern) and sets coordinate system to ThreeD.</summary>
        public PlanarBar3DSeries PlanarBar3D(Numerics.Vec x, Numerics.Vec y, Numerics.Vec z)
        {
            x.Length.ThrowIfMisaligned(y.Length);
            x.Length.ThrowIfMisaligned(z.Length);
            axes.CoordinateSystem = CoordinateSystem.ThreeD;
            return axes.AddSeries(new PlanarBar3DSeries(x, y, z));
        }

        /// <summary>Adds a 3D line (polyline) series and sets coordinate system to ThreeD.</summary>
        public Line3DSeries Plot3D(double[] x, double[] y, double[] z)
        {
            x.Length.ThrowIfMisaligned(y.Length);
            x.Length.ThrowIfMisaligned(z.Length);
            axes.CoordinateSystem = CoordinateSystem.ThreeD;
            return axes.AddSeries(new Line3DSeries(x, y, z));
        }

        /// <summary>Adds a triangulated surface series and sets coordinate system to ThreeD.</summary>
        public Trisurf3DSeries Trisurf(double[] x, double[] y, double[] z)
        {
            x.Length.ThrowIfMisaligned(y.Length);
            x.Length.ThrowIfMisaligned(z.Length);
            axes.CoordinateSystem = CoordinateSystem.ThreeD;
            return axes.AddSeries(new Trisurf3DSeries(x, y, z));
        }

        /// <summary>Adds a 3D contour series and sets coordinate system to ThreeD.</summary>
        public Contour3DSeries Contour3D(double[] x, double[] y, double[,] z)
        {
            z.ThrowIfMisaligned(y.Length, x.Length);
            axes.CoordinateSystem = CoordinateSystem.ThreeD;
            return axes.AddSeries(new Contour3DSeries(x, y, z));
        }

        /// <summary>Adds a 3D quiver (vector field) series and sets coordinate system to ThreeD.</summary>
        public Quiver3DSeries Quiver3D(double[] x, double[] y, double[] z, double[] u, double[] v, double[] w)
        {
            x.Length.ThrowIfMisaligned(y.Length);
            x.Length.ThrowIfMisaligned(z.Length);
            x.Length.ThrowIfMisaligned(u.Length);
            x.Length.ThrowIfMisaligned(v.Length);
            x.Length.ThrowIfMisaligned(w.Length);
            axes.CoordinateSystem = CoordinateSystem.ThreeD;
            return axes.AddSeries(new Quiver3DSeries(x, y, z, u, v, w));
        }

        /// <summary>Adds a voxel series and sets coordinate system to ThreeD.</summary>
        public VoxelSeries Voxels(bool[,,] filled)
        {
            axes.CoordinateSystem = CoordinateSystem.ThreeD;
            return axes.AddSeries(new VoxelSeries(filled));
        }

        /// <summary>Adds a single 3D text annotation and sets coordinate system to ThreeD.</summary>
        public Text3DSeries Text3D(double x, double y, double z, string text)
        {
            axes.CoordinateSystem = CoordinateSystem.ThreeD;
            return axes.AddSeries(new Text3DSeries([new Text3DAnnotation(x, y, z, text)]));
        }
    }
}
