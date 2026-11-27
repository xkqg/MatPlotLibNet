// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>The axis-aligned box a 3-D bar or a voxel is drawn as: its six faces (top, bottom, front at <see cref="Y0"/>, back at
/// <see cref="Y1"/>, left at <see cref="X0"/>, right at <see cref="X1"/>), the four corners of each in the order they are drawn, the
/// outward normal of each and the light on it. The one owner of a box's faces, so a bar and a voxel are built and shaded alike.</summary>
/// <param name="X0">The low end along X.</param>
/// <param name="X1">The high end along X.</param>
/// <param name="Y0">The low end along Y.</param>
/// <param name="Y1">The high end along Y.</param>
/// <param name="Z0">The low end along Z.</param>
/// <param name="Z1">The high end along Z.</param>
internal readonly record struct Cuboid(double X0, double X1, double Y0, double Y1, double Z0, double Z1)
{
    /// <summary>The outward normals of the six faces, in face order: top, bottom, front, back, left, right.</summary>
    private static readonly Vec3[] Normals = [new(0, 0, 1), new(0, 0, -1), new(0, -1, 0), new(0, 1, 0), new(-1, 0, 0), new(1, 0, 0)];

    /// <summary>How many faces a box has.</summary>
    internal const int FaceCount = 6;

    /// <summary>The colour each face of a box of colour <paramref name="color"/> is drawn in under <paramref name="light"/>, in face
    /// order: the colour shaded by the light on the face's outward normal (<see cref="ILightSource.Shade"/>), as matplotlib's
    /// <c>bar3d</c> and <c>voxels</c> shade their faces (<c>art3d._shade_colors</c>). matplotlib shades a box by default, so a box
    /// with no light of its own is lit by matplotlib's (<see cref="DirectionalLight.MatplotlibDefault"/>).</summary>
    /// <param name="color">The colour of the box when it is unlit.</param>
    /// <param name="light">The light the box is lit by; <see langword="null"/> for matplotlib's.</param>
    /// <returns>The six colours, in face order.</returns>
    internal static Color[] ShadesOf(Color color, ILightSource? light)
    {
        light ??= DirectionalLight.MatplotlibDefault;
        var shades = new Color[FaceCount];
        for (int face = 0; face < FaceCount; face++)
        {
            var n = Normals[face];
            shades[face] = light.Shade(color, n.X, n.Y, n.Z);
        }

        return shades;
    }

    /// <summary>Adds the faces of the box named by <paramref name="shown"/> (all six by default) to <paramref name="faces"/>, in face
    /// order, projected by <paramref name="projection"/>, face <c>k</c> filled with <c>fills[k]</c>, each at the depth of its centre; a
    /// box with a corner of those faces that cannot be placed is one mark and adds none of them.</summary>
    /// <param name="faces">The faces of the series, which the box's are added to.</param>
    /// <param name="projection">The projection of the scene.</param>
    /// <param name="fills">The colour of each face, in face order.</param>
    /// <param name="shown">The faces drawn; every face by default.</param>
    /// <returns><see langword="true"/> when the faces were added.</returns>
    internal bool AddFacesTo(List<FilledFace> faces, Projection3D projection, Color[] fills, BoxFaces shown = BoxFaces.All)
    {
        int start = faces.Count;
        for (int face = 0; face < FaceCount; face++)
        {
            if ((shown & (BoxFaces)(1 << face)) != 0) faces.Add(FaceOf(projection, face, fills[face]));
        }

        for (int i = start; i < faces.Count; i++)
        {
            if (faces[i].Vertices.IsDrawable()) continue;
            faces.RemoveRange(start, faces.Count - start);
            return false;
        }

        return true;
    }

    /// <summary>Face <paramref name="face"/> of the box (in face order), its corners in the order they are drawn.</summary>
    private FilledFace FaceOf(Projection3D projection, int face, Color fill) => face switch
    {
        0 => FaceOf(projection, fill, new(X0, Y0, Z1), new(X1, Y0, Z1), new(X1, Y1, Z1), new(X0, Y1, Z1)),
        1 => FaceOf(projection, fill, new(X0, Y0, Z0), new(X0, Y1, Z0), new(X1, Y1, Z0), new(X1, Y0, Z0)),
        2 => FaceOf(projection, fill, new(X0, Y0, Z0), new(X1, Y0, Z0), new(X1, Y0, Z1), new(X0, Y0, Z1)),
        3 => FaceOf(projection, fill, new(X1, Y1, Z0), new(X0, Y1, Z0), new(X0, Y1, Z1), new(X1, Y1, Z1)),
        4 => FaceOf(projection, fill, new(X0, Y1, Z0), new(X0, Y0, Z0), new(X0, Y0, Z1), new(X0, Y1, Z1)),
        _ => FaceOf(projection, fill, new(X1, Y0, Z0), new(X1, Y1, Z0), new(X1, Y1, Z1), new(X1, Y0, Z1)),
    };

    /// <summary>One face: its corners projected, at the depth of the mean of its corners.</summary>
    private static FilledFace FaceOf(Projection3D projection, Color fill, Vec3 c0, Vec3 c1, Vec3 c2, Vec3 c3)
    {
        var vertices = new[]
        {
            projection.Project(c0.X, c0.Y, c0.Z),
            projection.Project(c1.X, c1.Y, c1.Z),
            projection.Project(c2.X, c2.Y, c2.Z),
            projection.Project(c3.X, c3.Y, c3.Z),
        };
        double cx = (c0.X + c1.X + c2.X + c3.X) / 4.0;
        double cy = (c0.Y + c1.Y + c2.Y + c3.Y) / 4.0;
        double cz = (c0.Z + c1.Z + c2.Z + c3.Z) / 4.0;
        return new(projection.Depth(cx, cy, cz), vertices, fill);
    }
}

/// <summary>Which faces of a box (<see cref="Cuboid"/>) are drawn, one bit per face in face order.</summary>
[Flags]
internal enum BoxFaces
{
    /// <summary>No face.</summary>
    None = 0,

    /// <summary>The face at the high end along Z.</summary>
    Top = 1,

    /// <summary>The face at the low end along Z.</summary>
    Bottom = 2,

    /// <summary>The face at the low end along Y.</summary>
    Front = 4,

    /// <summary>The face at the high end along Y.</summary>
    Back = 8,

    /// <summary>The face at the low end along X.</summary>
    Left = 16,

    /// <summary>The face at the high end along X.</summary>
    Right = 32,

    /// <summary>Every face.</summary>
    All = Top | Bottom | Front | Back | Left | Right,
}

/// <summary>A filled face of a 3-D scene (a face of a box, a planar bar): its corners on the canvas, its fill, and its depth.</summary>
/// <param name="Depth">Where the face stands in depth: lower is farther from the camera.</param>
/// <param name="Vertices">The corners on the canvas, in drawing order.</param>
/// <param name="Fill">The colour it is filled with.</param>
internal readonly record struct FilledFace(double Depth, Point[] Vertices, Color Fill) : IDepthMark;
