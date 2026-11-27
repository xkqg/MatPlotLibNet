// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="VoxelSeries"/> as unit boxes (<see cref="Cuboid"/>) projected from 3D to 2D, their faces drawn back to
/// front with the scene.</summary>
internal sealed class VoxelSeriesRenderer : SceneSeriesRenderer<VoxelSeries>
{
    /// <inheritdoc />
    public VoxelSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(VoxelSeries series)
    {
        int xDim = series.Filled.GetLength(0);
        int yDim = series.Filled.GetLength(1);
        int zDim = series.Filled.GetLength(2);
        if (xDim == 0 || yDim == 0 || zDim == 0) return;

        var baseColor = ColorOf(series);
        var proj = ProjectionFor(new Range1D(0, xDim), new Range1D(0, yDim), new Range1D(0, zDim));

        // Every face of a voxel is shaded by the light on its outward normal, as a 3-D bar's is: matplotlib's voxels shade with the
        // same art3d._shade_colors as its bar3d.
        var fills = Cuboid.ShadesOf(baseColor, Context.LightSource);

        // Only the faces between a filled cell and an empty one (or the edge of the grid), as matplotlib draws a voxel's faces: a face
        // between two filled cells is inside the solid.
        var faces = new List<FilledFace>();
        var filled = series.Filled;
        for (int x = 0; x < xDim; x++)
            for (int y = 0; y < yDim; y++)
                for (int z = 0; z < zDim; z++)
                {
                    if (filled[x, y, z]) new Cuboid(x, x + 1, y, y + 1, z, z + 1).AddFacesTo(faces, proj, fills, OuterFacesAt(filled, x, y, z));
                }

        // Every face edged as the theme decides for a patch (black at 1 pt under classic, none under v2), the voxel's own faint hairline
        // under the library's themes.
        var theme = Context.Theme;
        var edges = theme.PatchEdgesOf(series);
        var edge = edges.Of();
        DrawBackToFront(faces, face => Ctx.DrawPolygon(face.Vertices, edges.StyleOf(face.Fill, edge)), series.Alpha);
    }

    /// <summary>The faces of the filled cell (<paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>) that border an empty cell
    /// or the edge of the grid.</summary>
    private static BoxFaces OuterFacesAt(bool[,,] filled, int x, int y, int z)
    {
        var faces = BoxFaces.None;
        if (!IsFilled(filled, x, y, z + 1)) faces |= BoxFaces.Top;
        if (!IsFilled(filled, x, y, z - 1)) faces |= BoxFaces.Bottom;
        if (!IsFilled(filled, x, y - 1, z)) faces |= BoxFaces.Front;
        if (!IsFilled(filled, x, y + 1, z)) faces |= BoxFaces.Back;
        if (!IsFilled(filled, x - 1, y, z)) faces |= BoxFaces.Left;
        if (!IsFilled(filled, x + 1, y, z)) faces |= BoxFaces.Right;
        return faces;
    }

    /// <summary>Whether the cell (<paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>) lies in the grid and is filled.</summary>
    private static bool IsFilled(bool[,,] filled, int x, int y, int z) =>
        x >= 0 && y >= 0 && z >= 0 && x < filled.GetLength(0) && y < filled.GetLength(1) && z < filled.GetLength(2) && filled[x, y, z];
}
