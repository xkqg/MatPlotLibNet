// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The verbs of the axes builder that add a series of the 3-D family (<see cref="AxesThreeDVerbs"/>), and the view of
/// a 3-D axes: its camera, its light and its panes.</summary>
public static class AxesBuilderThreeDVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Configures the 3D axes pane colors (floor, left wall, right wall).
        /// Set <c>Visible = false</c> to hide all panes for a transparent 3D background.</summary>
        public AxesBuilder WithPane3D(Func<Pane3DConfig, Pane3DConfig> configure) { axes.Axes.Pane3D = configure(axes.Axes.Pane3D); return axes; }

        /// <summary>Adds a 3D surface series to the axes.</summary>
        public AxesBuilder Surface(double[] x, double[] y, double[,] z, Action<SurfaceSeries>? configure = null)
            => axes.Configured(axes.Axes.Surface(x, y, z), configure);

        /// <summary>Adds a 3D wireframe series to the axes.</summary>
        public AxesBuilder Wireframe(double[] x, double[] y, double[,] z, Action<WireframeSeries>? configure = null)
            => axes.Configured(axes.Axes.Wireframe(x, y, z), configure);

        /// <summary>Adds a 3D scatter series to the axes.</summary>
        public AxesBuilder Scatter3D(double[] x, double[] y, double[] z, Action<Scatter3DSeries>? configure = null)
            => axes.Configured(axes.Axes.Scatter3D(x, y, z), configure);

        /// <summary>Adds a 3D stem series to the axes.</summary>
        public AxesBuilder Stem3D(double[] x, double[] y, double[] z, Action<Stem3DSeries>? configure = null)
            => axes.Configured(axes.Axes.Stem3D(x, y, z), configure);

        /// <summary>Adds a 3D bar series to the axes.</summary>
        public AxesBuilder Bar3D(double[] x, double[] y, double[] z, Action<Bar3DSeries>? configure = null)
            => axes.Configured(axes.Axes.Bar3D(x, y, z), configure);

        /// <summary>Adds a planar 3D bar series — flat translucent rectangles placed on Y planes.
        /// Reproduces matplotlib's <c>ax.bar(xs, heights, zs=y, zdir='y')</c> pattern
        /// (also known as "2D bars in different planes" or a "skyscraper plot").</summary>
        public AxesBuilder PlanarBar3D(double[] x, double[] y, double[] z, Action<PlanarBar3DSeries>? configure = null)
            => axes.Configured(axes.Axes.PlanarBar3D(x, y, z), configure);

        /// <summary>Adds a 3D line (polyline) series to the axes.</summary>
        public AxesBuilder Plot3D(double[] x, double[] y, double[] z, Action<Line3DSeries>? configure = null)
            => axes.Configured(axes.Axes.Plot3D(x, y, z), configure);

        /// <summary>Adds a triangulated surface series to the axes.</summary>
        public AxesBuilder Trisurf(double[] x, double[] y, double[] z, Action<Trisurf3DSeries>? configure = null)
            => axes.Configured(axes.Axes.Trisurf(x, y, z), configure);

        /// <summary>Adds a 3D contour series to the axes.</summary>
        public AxesBuilder Contour3D(double[] x, double[] y, double[,] z, Action<Contour3DSeries>? configure = null)
            => axes.Configured(axes.Axes.Contour3D(x, y, z), configure);

        /// <summary>Adds a 3D quiver (vector field) series to the axes.</summary>
        public AxesBuilder Quiver3D(double[] x, double[] y, double[] z, double[] u, double[] v, double[] w, Action<Quiver3DSeries>? configure = null)
            => axes.Configured(axes.Axes.Quiver3D(x, y, z, u, v, w), configure);

        /// <summary>Adds a voxel series to the axes.</summary>
        public AxesBuilder Voxels(bool[,,] filled, Action<VoxelSeries>? configure = null)
            => axes.Configured(axes.Axes.Voxels(filled), configure);

        /// <summary>Adds a 3D text annotation series to the axes.</summary>
        public AxesBuilder Text3D(double x, double y, double z, string text, Action<Text3DSeries>? configure = null)
            => axes.Configured(axes.Axes.Text3D(x, y, z, text), configure);

        /// <summary>Sets the 3D projection angles for ThreeD coordinate system axes.</summary>
        public AxesBuilder WithProjection(double elevation = Camera3D.RestingElevation, double azimuth = Camera3D.RestingAzimuth)
        {
            axes.Axes.Elevation = elevation;
            axes.Axes.Azimuth = azimuth;
            return axes;
        }

        /// <summary>Sets camera elevation, azimuth, and optional perspective distance for 3D axes.</summary>
        /// <param name="elevation">Camera elevation above the XY plane in degrees.</param>
        /// <param name="azimuth">Camera azimuth rotation around the Z axis in degrees.</param>
        /// <param name="distance">The camera's distance from the centre of the box, at least 2. Null keeps matplotlib's default
        /// camera, at <see cref="Camera3D.RestingDistance"/>. The projection is a perspective one either way; a closer camera bends the box more and keeps its size,
        /// as matplotlib's focal length does (a distance d draws as focal length d / 10).</param>
        public AxesBuilder WithCamera(double elevation = Camera3D.RestingElevation, double azimuth = Camera3D.RestingAzimuth, double? distance = null)
        {
            axes.Axes.CameraDistance = distance;
            return axes.WithProjection(elevation, azimuth);
        }

        /// <summary>Attaches a directional light source for per-face shading on 3D surfaces and bars.</summary>
        /// <param name="dx">X component of the light direction.</param>
        /// <param name="dy">Y component of the light direction.</param>
        /// <param name="dz">Z component of the light direction.</param>
        /// <param name="ambient">Ambient light intensity [0, 1]. Default <see cref="Rendering.Lighting.DirectionalLight.RestingAmbient"/>.</param>
        /// <param name="diffuse">Diffuse light intensity [0, 1]. Default <see cref="Rendering.Lighting.DirectionalLight.RestingDiffuse"/>.</param>
        public AxesBuilder WithLighting(double dx, double dy, double dz,
            double ambient = Rendering.Lighting.DirectionalLight.RestingAmbient, double diffuse = Rendering.Lighting.DirectionalLight.RestingDiffuse)
        {
            axes.Axes.LightSource = new Rendering.Lighting.DirectionalLight(dx, dy, dz, ambient, diffuse);
            return axes;
        }
    }
}
