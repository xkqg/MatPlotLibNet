// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// Ground-truth parity tests for <see cref="CubeFaceSelection"/> — which cube faces the camera
/// puts at the back, and which cube edges therefore carry the X/Y/Z ticks.
/// <para>
/// Every expectation in <see cref="MatplotlibTruth"/> was READ OUT OF matplotlib 3.11.1 itself
/// (<c>mpl_toolkits.mplot3d.axis3d.Axis._get_coord_info</c> and
/// <c>_get_axis_line_edge_points</c> queried directly for each camera), not derived by hand.
/// The rows deliberately include the edge-on cameras where matplotlib applies its special
/// tie handling (<c>axis3d.py</c> lines 288-297): elevation ±90 collapses the X/Y pair, and
/// elevation 0 collapses the Z pair with one of X/Y.
/// </para>
/// <para>
/// Bounds are deliberately unequal on all three axes — x[-10, 370], y[0, 50], z[0, 60] — so a
/// swapped-axis implementation cannot pass by coincidence.
/// </para>
/// </summary>
public class CubeFaceSelectionTests
{
    private const double XMin = -10, XMax = 370;
    private const double YMin = 0, YMax = 50;
    private const double ZMin = 0, ZMax = 60;

    private static readonly Box3D Box = new(XMin, XMax, YMin, YMax, ZMin, ZMax);

    private static Projection3D Projection(double elevation, double azimuth) =>
        new(elevation, azimuth, new Rect(0, 0, 600, 600), XMin, XMax, YMin, YMax, ZMin, ZMax);

    /// <summary>
    /// One row per camera: elevation; azimuth; back side of the X/Y/Z pane; then the sides of the
    /// coordinates each axis line is PINNED to — X line (y, z), Y line (x, z), Z line (x, y).
    /// </summary>
    public static TheoryData<double, double, string, string, string, string, string, string, string, string, string> MatplotlibTruth()
    {
        var data = new TheoryData<double, double, string, string, string, string, string, string, string, string, string>();
        foreach (var row in TruthRows)
        {
            var f = row.Split(';');
            data.Add(double.Parse(f[0], System.Globalization.CultureInfo.InvariantCulture),
                     double.Parse(f[1], System.Globalization.CultureInfo.InvariantCulture),
                     f[2], f[3], f[4], f[5], f[6], f[7], f[8], f[9], f[10]);
        }
        return data;
    }

    // elev;azim;backX;backY;backZ;xLine(y,z);yLine(x,z);zLine(x,y)
    private static readonly string[] TruthRows =
    [
        "90;-180;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "90;-145;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "90;-135;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "90;-120;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "90;-90;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "90;-60;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "90;-45;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "90;-30;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "90;-15;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "90;0;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "90;15;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "90;30;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "90;45;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "90;90;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "90;135;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "90;180;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "60;-180;Max;Min;Min;Max;Min;Min;Min;Min;Min",
        "60;-145;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "60;-135;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "60;-120;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "60;-90;Min;Max;Min;Min;Min;Max;Min;Max;Max",
        "60;-60;Min;Max;Min;Min;Min;Max;Min;Max;Max",
        "60;-45;Min;Max;Min;Min;Min;Max;Min;Max;Max",
        "60;-30;Min;Max;Min;Min;Min;Max;Min;Max;Max",
        "60;-15;Min;Max;Min;Min;Min;Max;Min;Max;Max",
        "60;0;Min;Min;Min;Max;Min;Max;Min;Max;Min",
        "60;15;Min;Min;Min;Max;Min;Max;Min;Max;Min",
        "60;30;Min;Min;Min;Max;Min;Max;Min;Max;Min",
        "60;45;Min;Min;Min;Max;Min;Max;Min;Max;Min",
        "60;90;Min;Min;Min;Max;Min;Max;Min;Max;Min",
        "60;135;Max;Min;Min;Max;Min;Min;Min;Min;Min",
        "60;180;Max;Min;Min;Max;Min;Min;Min;Min;Min",
        "30;-180;Max;Min;Min;Max;Min;Min;Min;Min;Min",
        "30;-145;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "30;-135;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "30;-120;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "30;-90;Min;Max;Min;Min;Min;Max;Min;Max;Max",
        "30;-60;Min;Max;Min;Min;Min;Max;Min;Max;Max",
        "30;-45;Min;Max;Min;Min;Min;Max;Min;Max;Max",
        "30;-30;Min;Max;Min;Min;Min;Max;Min;Max;Max",
        "30;-15;Min;Max;Min;Min;Min;Max;Min;Max;Max",
        "30;0;Min;Min;Min;Max;Min;Max;Min;Max;Min",
        "30;15;Min;Min;Min;Max;Min;Max;Min;Max;Min",
        "30;30;Min;Min;Min;Max;Min;Max;Min;Max;Min",
        "30;45;Min;Min;Min;Max;Min;Max;Min;Max;Min",
        "30;90;Min;Min;Min;Max;Min;Max;Min;Max;Min",
        "30;135;Max;Min;Min;Max;Min;Min;Min;Min;Min",
        "30;180;Max;Min;Min;Max;Min;Min;Min;Min;Min",
        "15;-180;Max;Min;Min;Max;Min;Min;Min;Min;Min",
        "15;-145;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "15;-135;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "15;-120;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "15;-90;Min;Max;Min;Min;Min;Max;Min;Max;Max",
        "15;-60;Min;Max;Min;Min;Min;Max;Min;Max;Max",
        "15;-45;Min;Max;Min;Min;Min;Max;Min;Max;Max",
        "15;-30;Min;Max;Min;Min;Min;Max;Min;Max;Max",
        "15;-15;Min;Max;Min;Min;Min;Max;Min;Max;Max",
        "15;0;Min;Min;Min;Max;Min;Max;Min;Max;Min",
        "15;15;Min;Min;Min;Max;Min;Max;Min;Max;Min",
        "15;30;Min;Min;Min;Max;Min;Max;Min;Max;Min",
        "15;45;Min;Min;Min;Max;Min;Max;Min;Max;Min",
        "15;90;Min;Min;Min;Max;Min;Max;Min;Max;Min",
        "15;135;Max;Min;Min;Max;Min;Min;Min;Min;Min",
        "15;180;Max;Min;Min;Max;Min;Min;Min;Min;Min",
        "0;-180;Max;Min;Min;Max;Min;Min;Min;Min;Min",
        "0;-145;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "0;-135;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "0;-120;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "0;-90;Max;Max;Min;Min;Min;Min;Min;Min;Max",
        "0;-60;Min;Max;Min;Min;Min;Max;Min;Max;Max",
        "0;-45;Min;Max;Min;Min;Min;Max;Min;Max;Max",
        "0;-30;Min;Max;Min;Min;Min;Max;Min;Max;Max",
        "0;-15;Min;Max;Min;Min;Min;Max;Min;Max;Max",
        "0;0;Min;Min;Min;Max;Min;Max;Min;Max;Min",
        "0;15;Min;Min;Min;Max;Min;Max;Min;Max;Min",
        "0;30;Min;Min;Min;Max;Min;Max;Min;Max;Min",
        "0;45;Min;Min;Min;Max;Min;Max;Min;Max;Min",
        "0;90;Max;Min;Min;Max;Min;Min;Min;Min;Min",
        "0;135;Max;Min;Min;Max;Min;Min;Min;Min;Min",
        "0;180;Max;Min;Min;Max;Min;Min;Min;Min;Min",
        "-15;-180;Max;Min;Max;Max;Max;Min;Max;Min;Min",
        "-15;-145;Max;Max;Max;Min;Max;Min;Max;Min;Max",
        "-15;-135;Max;Max;Max;Min;Max;Min;Max;Min;Max",
        "-15;-120;Max;Max;Max;Min;Max;Min;Max;Min;Max",
        "-15;-90;Min;Max;Max;Min;Max;Max;Max;Max;Max",
        "-15;-60;Min;Max;Max;Min;Max;Max;Max;Max;Max",
        "-15;-45;Min;Max;Max;Min;Max;Max;Max;Max;Max",
        "-15;-30;Min;Max;Max;Min;Max;Max;Max;Max;Max",
        "-15;-15;Min;Max;Max;Min;Max;Max;Max;Max;Max",
        "-15;0;Min;Min;Max;Max;Max;Max;Max;Max;Min",
        "-15;15;Min;Min;Max;Max;Max;Max;Max;Max;Min",
        "-15;30;Min;Min;Max;Max;Max;Max;Max;Max;Min",
        "-15;45;Min;Min;Max;Max;Max;Max;Max;Max;Min",
        "-15;90;Min;Min;Max;Max;Max;Max;Max;Max;Min",
        "-15;135;Max;Min;Max;Max;Max;Min;Max;Min;Min",
        "-15;180;Max;Min;Max;Max;Max;Min;Max;Min;Min",
        "-30;-180;Max;Min;Max;Max;Max;Min;Max;Min;Min",
        "-30;-145;Max;Max;Max;Min;Max;Min;Max;Min;Max",
        "-30;-135;Max;Max;Max;Min;Max;Min;Max;Min;Max",
        "-30;-120;Max;Max;Max;Min;Max;Min;Max;Min;Max",
        "-30;-90;Min;Max;Max;Min;Max;Max;Max;Max;Max",
        "-30;-60;Min;Max;Max;Min;Max;Max;Max;Max;Max",
        "-30;-45;Min;Max;Max;Min;Max;Max;Max;Max;Max",
        "-30;-30;Min;Max;Max;Min;Max;Max;Max;Max;Max",
        "-30;-15;Min;Max;Max;Min;Max;Max;Max;Max;Max",
        "-30;0;Min;Min;Max;Max;Max;Max;Max;Max;Min",
        "-30;15;Min;Min;Max;Max;Max;Max;Max;Max;Min",
        "-30;30;Min;Min;Max;Max;Max;Max;Max;Max;Min",
        "-30;45;Min;Min;Max;Max;Max;Max;Max;Max;Min",
        "-30;90;Min;Min;Max;Max;Max;Max;Max;Max;Min",
        "-30;135;Max;Min;Max;Max;Max;Min;Max;Min;Min",
        "-30;180;Max;Min;Max;Max;Max;Min;Max;Min;Min",
        "-60;-180;Max;Min;Max;Max;Max;Min;Max;Min;Min",
        "-60;-145;Max;Max;Max;Min;Max;Min;Max;Min;Max",
        "-60;-135;Max;Max;Max;Min;Max;Min;Max;Min;Max",
        "-60;-120;Max;Max;Max;Min;Max;Min;Max;Min;Max",
        "-60;-90;Min;Max;Max;Min;Max;Max;Max;Max;Max",
        "-60;-60;Min;Max;Max;Min;Max;Max;Max;Max;Max",
        "-60;-45;Min;Max;Max;Min;Max;Max;Max;Max;Max",
        "-60;-30;Min;Max;Max;Min;Max;Max;Max;Max;Max",
        "-60;-15;Min;Max;Max;Min;Max;Max;Max;Max;Max",
        "-60;0;Min;Min;Max;Max;Max;Max;Max;Max;Min",
        "-60;15;Min;Min;Max;Max;Max;Max;Max;Max;Min",
        "-60;30;Min;Min;Max;Max;Max;Max;Max;Max;Min",
        "-60;45;Min;Min;Max;Max;Max;Max;Max;Max;Min",
        "-60;90;Min;Min;Max;Max;Max;Max;Max;Max;Min",
        "-60;135;Max;Min;Max;Max;Max;Min;Max;Min;Min",
        "-60;180;Max;Min;Max;Max;Max;Min;Max;Min;Min",
        "-90;-180;Max;Max;Max;Min;Max;Min;Max;Min;Max",
        "-90;-145;Max;Max;Max;Min;Max;Min;Max;Min;Max",
        "-90;-135;Max;Max;Max;Min;Max;Min;Max;Min;Max",
        "-90;-120;Max;Max;Max;Min;Max;Min;Max;Min;Max",
        "-90;-90;Max;Max;Max;Min;Max;Min;Max;Min;Max",
        "-90;-60;Max;Max;Max;Min;Max;Min;Max;Min;Max",
        "-90;-45;Max;Max;Max;Min;Max;Min;Max;Min;Max",
        "-90;-30;Max;Max;Max;Min;Max;Min;Max;Min;Max",
        "-90;-15;Max;Max;Max;Min;Max;Min;Max;Min;Max",
        "-90;0;Max;Max;Max;Min;Max;Min;Max;Min;Max",
        "-90;15;Max;Max;Max;Min;Max;Min;Max;Min;Max",
        "-90;30;Max;Max;Max;Min;Max;Min;Max;Min;Max",
        "-90;45;Max;Max;Max;Min;Max;Min;Max;Min;Max",
        "-90;90;Max;Max;Max;Min;Max;Min;Max;Min;Max",
        "-90;135;Max;Max;Max;Min;Max;Min;Max;Min;Max",
        "-90;180;Max;Max;Max;Min;Max;Min;Max;Min;Max",
    ];

    private static CubeSide Side(string s) => s == "Min" ? CubeSide.Min : CubeSide.Max;

    private static double Coord(CubeAxis axis, CubeSide side) => (axis, side) switch
    {
        (CubeAxis.X, CubeSide.Min) => XMin,
        (CubeAxis.X, CubeSide.Max) => XMax,
        (CubeAxis.Y, CubeSide.Min) => YMin,
        (CubeAxis.Y, CubeSide.Max) => YMax,
        (CubeAxis.Z, CubeSide.Min) => ZMin,
        _ => ZMax,
    };

    /// <summary>Verifies the three back panes match matplotlib's own choice for every sampled camera.</summary>
    [Theory]
    [MemberData(nameof(MatplotlibTruth))]
    public void Faces_BackPanes_MatchMatplotlib(
        double elevation, double azimuth,
        string backX, string backY, string backZ,
        string xLineY, string xLineZ, string yLineX, string yLineZ, string zLineX, string zLineY)
    {
        _ = xLineY; _ = xLineZ; _ = yLineX; _ = yLineZ; _ = zLineX; _ = zLineY;
        var faces = Projection(elevation, azimuth).Faces;

        Assert.Equal(Side(backX), faces.X.Back);
        Assert.Equal(Side(backY), faces.Y.Back);
        Assert.Equal(Side(backZ), faces.Z.Back);
    }

    /// <summary>Verifies the three tick-bearing axis edges match matplotlib's own choice for every sampled camera.</summary>
    [Theory]
    [MemberData(nameof(MatplotlibTruth))]
    public void Faces_AxisEdges_MatchMatplotlib(
        double elevation, double azimuth,
        string backX, string backY, string backZ,
        string xLineY, string xLineZ, string yLineX, string yLineZ, string zLineX, string zLineY)
    {
        _ = backX; _ = backY; _ = backZ;
        var faces = Projection(elevation, azimuth).Faces;

        var xEdge = faces.AxisEdge(CubeAxis.X, Box);
        Assert.Equal(XMin, Math.Min(xEdge.From.X, xEdge.To.X));
        Assert.Equal(XMax, Math.Max(xEdge.From.X, xEdge.To.X));
        Assert.Equal(Coord(CubeAxis.Y, Side(xLineY)), xEdge.From.Y);
        Assert.Equal(Coord(CubeAxis.Y, Side(xLineY)), xEdge.To.Y);
        Assert.Equal(Coord(CubeAxis.Z, Side(xLineZ)), xEdge.From.Z);
        Assert.Equal(Coord(CubeAxis.Z, Side(xLineZ)), xEdge.To.Z);

        var yEdge = faces.AxisEdge(CubeAxis.Y, Box);
        Assert.Equal(YMin, Math.Min(yEdge.From.Y, yEdge.To.Y));
        Assert.Equal(YMax, Math.Max(yEdge.From.Y, yEdge.To.Y));
        Assert.Equal(Coord(CubeAxis.X, Side(yLineX)), yEdge.From.X);
        Assert.Equal(Coord(CubeAxis.X, Side(yLineX)), yEdge.To.X);
        Assert.Equal(Coord(CubeAxis.Z, Side(yLineZ)), yEdge.From.Z);
        Assert.Equal(Coord(CubeAxis.Z, Side(yLineZ)), yEdge.To.Z);

        var zEdge = faces.AxisEdge(CubeAxis.Z, Box);
        Assert.Equal(ZMin, Math.Min(zEdge.From.Z, zEdge.To.Z));
        Assert.Equal(ZMax, Math.Max(zEdge.From.Z, zEdge.To.Z));
        Assert.Equal(Coord(CubeAxis.X, Side(zLineX)), zEdge.From.X);
        Assert.Equal(Coord(CubeAxis.X, Side(zLineX)), zEdge.To.X);
        Assert.Equal(Coord(CubeAxis.Y, Side(zLineY)), zEdge.From.Y);
        Assert.Equal(Coord(CubeAxis.Y, Side(zLineY)), zEdge.To.Y);
    }

    /// <summary>
    /// Verifies the default matplotlib view keeps the historical hard-coded choice — floor z=zMin,
    /// the x=xMin wall and the y=yMax wall — so every fixture rendered at that camera is unchanged.
    /// </summary>
    [Fact]
    public void Faces_DefaultView_KeepsTheHistoricalPaneSet()
    {
        var faces = Projection(30, -60).Faces;

        Assert.Equal(CubeSide.Min, faces.X.Back);
        Assert.Equal(CubeSide.Max, faces.Y.Back);
        Assert.Equal(CubeSide.Min, faces.Z.Back);
    }

    /// <summary>
    /// Verifies the reported bug camera selects the OPPOSITE x wall and moves the Y and Z axis
    /// lines to the x=xMin edge (GitHub issue #18).
    /// </summary>
    [Fact]
    public void Faces_Azimuth145_SelectsTheOppositeXWall()
    {
        var faces = Projection(30, -145).Faces;

        Assert.Equal(CubeSide.Max, faces.X.Back);
        Assert.Equal(XMin, faces.AxisEdge(CubeAxis.Y, Box).From.X);
        Assert.Equal(XMin, faces.AxisEdge(CubeAxis.Z, Box).From.X);
    }

    /// <summary>
    /// Verifies the three back planes meet in exactly one corner and that corner is the farthest of
    /// the eight from the camera — the invariant that makes a swapped or duplicated side impossible.
    /// </summary>
    [Theory]
    [MemberData(nameof(MatplotlibTruth))]
    public void Faces_FarCorner_IsTheFarthestOfTheEightCorners(
        double elevation, double azimuth,
        string backX, string backY, string backZ,
        string xLineY, string xLineZ, string yLineX, string yLineZ, string zLineX, string zLineY)
    {
        _ = backX; _ = backY; _ = backZ; _ = xLineY; _ = xLineZ; _ = yLineX; _ = yLineZ; _ = zLineX; _ = zLineY;
        var proj = Projection(elevation, azimuth);
        var corner = proj.Faces.FarCorner(Box);

        double cornerDepth = proj.Depth(corner.X, corner.Y, corner.Z);
        foreach (double x in new[] { XMin, XMax })
            foreach (double y in new[] { YMin, YMax })
                foreach (double z in new[] { ZMin, ZMax })
                    Assert.True(proj.Depth(x, y, z) >= cornerDepth - 1e-9,
                        $"corner ({x},{y},{z}) is farther than the selected far corner at elev {elevation}, azim {azimuth}");
    }

    /// <summary>Verifies a degenerate axis range still yields a finite, usable selection.</summary>
    [Fact]
    public void Faces_DegenerateRange_StaysFinite()
    {
        var proj = new Projection3D(30, -60, new Rect(0, 0, 600, 600), 5, 5, 0, 50, 0, 60);
        var faces = proj.Faces;
        var edge = faces.AxisEdge(CubeAxis.X, new Box3D(5, 5, 0, 50, 0, 60));

        Assert.True(double.IsFinite(edge.From.X));
        Assert.True(double.IsFinite(edge.To.X));
        Assert.Equal(5, edge.From.X);
    }

    /// <summary>Verifies every plane reports the side opposite its back side as its front side.</summary>
    [Fact]
    public void CubePlane_Front_IsTheOppositeSide()
    {
        Assert.Equal(CubeSide.Max, new CubePlane(CubeAxis.X, CubeSide.Min).Front);
        Assert.Equal(CubeSide.Min, new CubePlane(CubeAxis.Z, CubeSide.Max).Front);
    }

    /// <summary>Verifies the indexer returns the plane belonging to the requested axis.</summary>
    [Fact]
    public void Faces_Indexer_ReturnsThePlaneForTheAxis()
    {
        var faces = Projection(30, -60).Faces;

        Assert.Equal(CubeAxis.X, faces[CubeAxis.X].Axis);
        Assert.Equal(CubeAxis.Y, faces[CubeAxis.Y].Axis);
        Assert.Equal(CubeAxis.Z, faces[CubeAxis.Z].Axis);
        Assert.Equal(3, faces.Planes.Count);
    }

    /// <summary>Verifies the plane resolves both of its coordinates against a caller-supplied box.</summary>
    [Fact]
    public void CubePlane_Coordinate_ResolvesAgainstTheCallerBox()
    {
        var plane = new CubePlane(CubeAxis.Y, CubeSide.Max);

        Assert.Equal(YMax, plane.Coordinate(Box, plane.Back));
        Assert.Equal(YMin, plane.Coordinate(Box, plane.Front));
    }
}
