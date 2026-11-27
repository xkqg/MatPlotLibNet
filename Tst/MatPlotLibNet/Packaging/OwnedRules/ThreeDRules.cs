// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging.OwnedRules;

/// <summary>The rules of a 3-D scene: where its camera stands at rest, the one depth queue its marks are put back to front by, the
/// faces of a box, the edge of a box under a theme and the world box its values are scaled into.</summary>
internal static class ThreeDRules
{
/// <summary>Where a 3-D camera stands when it is told nothing (matplotlib's elevation 30, azimuth -60 and distance 10) is written once,
    /// in Camera3D: the axes, the verbs, the wire form, the Home key, the projection, a series drawn off a 3-D axes and the scene's
    /// markup read it there, so no site types the angles or the distance, or builds a projection from numbers of its own.</summary>
    private static readonly OwnedRule CameraAtRest = new(
        "where a 3-D camera stands at rest (elevation 30, azimuth -60, distance 10) is written once, in Camera3D: no site types the angles or the distance, or builds a projection from angles of its own",
        @"(?:[Ee]levation|[Aa]zimuth|[Dd]ist(?:ance)?)\w*\s*(?:=|!=|==|\?\?)\s*-?(?:30|60|10)(?:\.0+)?\b|\b(?:elevation|azimuth|distance):\s*-?(?:30|60|10)\b|\bnew Projection3D\(\s*-?\d",
        "Src/MatPlotLibNet/Models/Camera3D.cs") { Under = "Src/MatPlotLibNet/", Finds = [
        "public AxesBuilder WithCamera(double elevation = 30, double azimuth = -60, double? distance = null)",
        "    private const double DefaultElevation = 30.0;",
        "    private const double DefaultDist = 10.0;",
        "        Context.Projection3D ?? new Projection3D(30, -60, Area.PlotBounds,",
        "        _sb.Append(\" data-distance=\\\"\").AppendExactSvgNumber(distance ?? 10.0).Append('\"');",
        "    internal const double RestingAzimuth = -60;"] };

/// <summary>The prose of a camera setting names where the camera stands at rest by a cref to Camera3D's constant: no sentence types
    /// the distance 10, the elevation 30 or the azimuth -60 as a default again.</summary>
    private static readonly OwnedRule CameraAtRestProse = OwnedRule.InDocumentation(
        "the prose of a camera setting names where the camera stands at rest by a cref to Camera3D's constants: no sentence types 10, 30 or -60 as the camera's default",
        @"\b(?:[Cc]amera|[Ee]levation|[Aa]zimuth)\b.*\b(?:at 10\b|[Dd]efault(?:s)?(?: is| to| of)?\s+-?(?:30|60|10)\b)|^\s*///\s*camera, at 10\b",
        under: "Src/") with { Finds = [
        "    /// camera, at 10. The projection is a perspective one either way; a closer camera bends the box more and keeps its size,",
        "    /// <summary>Camera elevation above the XY plane in degrees. Default 30.</summary>",
        "    /// <summary>Camera azimuth rotation around the Z axis in degrees. Default -60.</summary>"] };

/// <summary>The marks of a 3-D scene are put back to front in one place, the scene's depth queue: a renderer hands its marks to it
    /// (SceneSeriesRenderer.DrawBackToFront) and never sorts them itself.</summary>
    private static readonly OwnedRule DepthOrder = OwnedRule.Nowhere(
        "the marks of a 3-D scene are put back to front by the one depth queue (DepthQueue3D): no 3-D renderer sorts its marks or orders them by a comparison of its own",
        @"\.Sort\(|\bIComparable<|\bOrderBy(?:Descending)?\(|\bCompareTo\(",
        under: "Src/MatPlotLibNet/Rendering/SeriesRenderers/ThreeD/") with { Finds = [
        "        segments.Sort((a, b) => a.Depth.CompareTo(b.Depth));",
        "        quads.Sort();",
        "    private readonly record struct ProjectedQuad(double Depth, Point[] Vertices, int Face) : IComparable<ProjectedQuad>"] };

/// <summary>A 3-D scene has one depth queue, made by the 3-D axes for every series it draws; a 3-D series drawn on its own (off a
    /// 3-D axes) is one scene of its own, whose queue the renderer's base makes.</summary>
    private static readonly OwnedRule DepthQueueMade = new(
        "a 3-D scene has one depth queue: the 3-D axes makes it for all its series and patches, and a 3-D series drawn off a 3-D axes is a scene of its own whose queue the renderer's base makes",
        @"\bnew DepthQueue3D\(",
        "Src/MatPlotLibNet/Rendering/ThreeDAxesRenderer.cs", "Src/MatPlotLibNet/Rendering/SeriesRenderers/ThreeD/SceneSeriesRenderer.cs") { Under = "Src/", Finds = [
        "        var depthQueue = new DepthQueue3D();"] };

/// <summary>The six faces of a box (a 3-D bar, a voxel), their corners, their outward normals and the light on each are Cuboid's: no
    /// renderer lists the faces of a box or shades one by a normal of its own.</summary>
    private static readonly OwnedRule BoxFaces = new(
        "the six faces of a box (a 3-D bar, a voxel), their corners, their outward normals and the light on each are Cuboid's: no renderer lists the faces of a box or shades a face by a normal it types",
        @"(?:\(|,)\s*-?[01](?:d|\.0)?\s*,\s*-?[01](?:d|\.0)?\s*,\s*-?[01](?:d|\.0)?\s*\)|\bAddFace\(|\brecord struct DepthFace\b",
        "Src/MatPlotLibNet/Rendering/SeriesRenderers/ThreeD/Cuboid.cs") { Under = "Src/MatPlotLibNet/Rendering/SeriesRenderers/ThreeD/", Finds = [
        "        var topColor    = light.Shade(baseColor,  0,  0,  1);",
        "    private readonly record struct DepthFace(double Depth, Point[] Vertices, Color Fill);",
        "            AddFace(prism, proj, topColor,"] };

    /// <summary>The rules of the area, in the order the scan reads them.</summary>
    internal static OwnedRule[] All { get; } =
    [
        OwnedRule.Nowhere("the edge of a 3-D box or bar is its theme's patch rule (Theme.PatchEdgesOf: none under v2, black at 1 pt under classic, the series' own hairline under the library's themes): no 3-D renderer types a black edge",
            @"\bColors\.Black\b", under: "Src/MatPlotLibNet/Rendering/SeriesRenderers/ThreeD/") with { Finds = [
            "        var strokeColor = Colors.Black;"] },
        OwnedRule.Nowhere("a value along X, Y or Z of a 3-D projection is measured and scaled into the world box by its WorldAxis (From, Scale, Share, Middle): no projection holds the six ends of the box field by field",
            @"\b_[xyz]M(?:in|ax)\b", under: "Src/MatPlotLibNet/Rendering/") with { Finds = [
            "    private readonly double _xMin, _xMax, _yMin, _yMax, _zMin, _zMax;"] },
        CameraAtRest,
        CameraAtRestProse,
        DepthOrder,
        DepthQueueMade,
        BoxFaces,
    ];
}
