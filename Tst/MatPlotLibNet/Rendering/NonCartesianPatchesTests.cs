// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A patch on a polar, ternary or 3-D axes is drawn through that axes' own placement of its data, as matplotlib draws a
/// patch through the axes' <c>transData</c>: a polar axes maps (x, y) as (angle, radius), a ternary axes places it in the
/// triangle's frame, and a 3-D axes stands it on a plane (<see cref="Patch.Z"/>, <see cref="Patch.ZDir"/>), matplotlib's
/// <c>pathpatch_2d_to_3d</c>. Each draws and lists its patches, in their z-order among the series.</summary>
public class NonCartesianPatchesTests
{
    private static readonly Rect Area = new(100, 100, 400, 400);

    /// <summary>The polygon drawn in <paramref name="color"/>: the patch, not a pane of the cube.</summary>
    private static DrawPolygonCall PolygonIn(RecordingRenderContext ctx, Color color) =>
        Assert.Single(ctx.Of<DrawPolygonCall>(), c => c.Paints(color));

    // ---- polar ------------------------------------------------------------------------------------------------------

    /// <summary>matplotlib 3.11.2 draws a rectangle on a polar axes through its four corners mapped by angle (x) and radius (y),
    /// joined straight, not bent round the centre: a patch's path has one interpolation step, and
    /// <c>PolarTransform.transform_path_non_affine</c> reads <c>if not len(path) or path._interpolation_steps == 1: return
    /// Path(self.transform_non_affine(path.vertices), path.codes)</c> (printed for Rectangle((0, 2), 3, 1): codes [1, 2, 2, 2, 79],
    /// five vertices).</summary>
    [Fact]
    public void APolarRectangle_IsDrawnThroughItsFourCornersMappedByAngleAndRadius()
    {
        var axes = new Axes { CoordinateSystem = CoordinateSystem.Polar };
        axes.YAxis.Max = 5;
        axes.AddPatch(new Rectangle(new(0.0, 2), 3.0, 1));
        var polar = new PolarTransform(Area, 5);

        var path = Assert.Single(axes.Drawn(Area).Of<DrawPathCall>()).Segments;

        Assert.Equal(5, path.Length);
        polar.PolarToPixel(2, 0.0).AssertCloseTo(Assert.IsType<MoveToSegment>(path[0]).Point, 1e-9);
        polar.PolarToPixel(2, 3.0).AssertCloseTo(Assert.IsType<LineToSegment>(path[1]).Point, 1e-9);
        polar.PolarToPixel(3, 3.0).AssertCloseTo(Assert.IsType<LineToSegment>(path[2]).Point, 1e-9);
        polar.PolarToPixel(3, 0.0).AssertCloseTo(Assert.IsType<LineToSegment>(path[3]).Point, 1e-9);
        Assert.IsType<CloseSegment>(path[4]);
    }

    /// <summary>A curve keeps its Bézier segments and each control point is mapped as a vertex (matplotlib's path codes for a
    /// circle on a polar axes stay 24 curve points between the move and the close).</summary>
    [Fact]
    public void APolarCircle_KeepsItsCurves_EveryControlPointMappedByAngleAndRadius()
    {
        var axes = new Axes { CoordinateSystem = CoordinateSystem.Polar };
        axes.YAxis.Max = 5;
        var circle = axes.AddPatch(new Circle(new(1.0, 3), 0.5));
        var polar = new PolarTransform(Area, 5);
        var data = circle.Outline();

        var path = Assert.Single(axes.Drawn(Area).Of<DrawPathCall>()).Segments;

        Assert.Equal(data.Count, path.Length);
        for (int i = 0; i < data.Count; i++)
        {
            if (data[i] is BezierSegment curve)
            {
                var drawn = Assert.IsType<BezierSegment>(path[i]);
                polar.PolarToPixel(curve.Control1.Y, curve.Control1.X).AssertCloseTo(drawn.Control1, 1e-9);
                polar.PolarToPixel(curve.Control2.Y, curve.Control2.X).AssertCloseTo(drawn.Control2, 1e-9);
                polar.PolarToPixel(curve.End.Y, curve.End.X).AssertCloseTo(drawn.End, 1e-9);
            }
        }
    }

    /// <summary>On a polar axes the patches range the radius as on a Cartesian axes: under the library's own themes always; under
    /// the matplotlib themes beside a series, as <c>add_patch</c> asks for no range (printed: a line up to r 5 beside a rectangle up
    /// to r 8 gives rlim (0, 8.35); the rectangle alone leaves (0, 1)).</summary>
    [Theory]
    [InlineData("default", false, true)]
    [InlineData("default", true, true)]
    [InlineData("v2", true, true)]
    [InlineData("v2", false, false)]
    public void APolarPatch_RangesTheRadius_WhereTheThemeLetsIt(string theme, bool withSeries, bool ranged)
    {
        var axes = new Axes { CoordinateSystem = CoordinateSystem.Polar };
        if (withSeries) axes.PolarPlot([1.0, 3.0, 5.0], [0.0, 1.0, 2.0]);
        axes.AddPatch(new Rectangle(new(0.5, 6), 0.5, 2));
        var centre = new Point(Area.X + Area.Width / 2, Area.Y + Area.Height / 2);
        double rim = Area.Width / 2;
        double FromCentre(Point p) => Math.Sqrt((p.X - centre.X) * (p.X - centre.X) + (p.Y - centre.Y) * (p.Y - centre.Y));

        var path = (Assert.Single(axes.Drawn(Area, theme.AsTheme()).Of<DrawPathCall>())).Segments;
        double near = FromCentre(Assert.IsType<MoveToSegment>(path[0]).Point);
        double far = FromCentre(Assert.IsType<LineToSegment>(path[2]).Point);

        // Ranged, the corner at r 8 stands inside the rim and farther out than the one at r 6; not ranged (the radius stays 0 to
        // 1), the corner at r 6 stands six times the rim out, past the circle the axes clips it at (PolarClipTests).
        Assert.Equal(ranged, far < rim - 1e-6 && far > near + 1);
        if (!ranged) Assert.Equal(6 * rim, near, 1e-9);
    }

    // ---- ternary ----------------------------------------------------------------------------------------------------

    /// <summary>A ternary axes draws a patch in the triangle's own frame, one unit wide (<see cref="TernaryPointExtensions.InFrame"/> gives a
    /// composition's place), through the transform its series and its zoom indicator are placed with.</summary>
    [Fact]
    public void ATernaryPatch_StandsInTheFrameOfTheTriangle()
    {
        var axes = new Axes { CoordinateSystem = CoordinateSystem.Ternary };
        axes.AddPatch(new Rectangle(new(0.25, 0.1), 0.5, 0.3));
        var frame = new DataTransform(0, 1, 0, TernaryFrame.Height, Area);

        var path = Assert.Single(axes.Drawn(Area).Of<DrawPathCall>()).Segments;

        frame.DataToPixel(0.25, 0.1).AssertCloseTo(Assert.IsType<MoveToSegment>(path[0]).Point, 1e-9);
        frame.DataToPixel(0.75, 0.1).AssertCloseTo(Assert.IsType<LineToSegment>(path[1]).Point, 1e-9);
        frame.DataToPixel(0.75, 0.4).AssertCloseTo(Assert.IsType<LineToSegment>(path[2]).Point, 1e-9);
    }

    // ---- 3-D --------------------------------------------------------------------------------------------------------

    /// <summary>The projection the 3-D renderer draws <paramref name="axes"/> through.</summary>
    private static Projection3D ProjectionOf(Axes axes)
    {
        var box = axes.ComputeCubeRange(Theme.Default);
        return new Projection3D(axes.Elevation, axes.Azimuth, Area, box.X.Lo, box.X.Hi, box.Y.Lo, box.Y.Hi, box.Z.Lo, box.Z.Hi,
            axes.CameraDistance);
    }

    private static Axes ThreeD()
    {
        var axes = new Axes { CoordinateSystem = CoordinateSystem.ThreeD };
        axes.Plot3D([0.0, 4.0], [0.0, 4.0], [0.0, 4.0]);
        return axes;
    }

    /// <summary>matplotlib's <c>pathpatch_2d_to_3d(patch, z, zdir)</c> places the 2-D vertices on a plane by <c>juggle_axes</c>:
    /// <c>if zdir == 'x': return zs, xs, ys; elif zdir == 'y': return xs, zs, ys; else: return xs, ys, zs</c>. A patch on a 3-D axes
    /// stands on the plane <see cref="Patch.Z"/> across <see cref="Patch.ZDir"/> (z = 0 across Z by default) and each corner is
    /// projected there.</summary>
    [Theory]
    [InlineData(ZDirection.Z, 0.0)]
    [InlineData(ZDirection.Z, 2.0)]
    [InlineData(ZDirection.X, 1.5)]
    [InlineData(ZDirection.Y, 3.0)]
    public void A3DRectangle_StandsOnThePlaneItsZAndZDirName(ZDirection direction, double z)
    {
        var axes = ThreeD();
        var rectangle = axes.AddPatch(new Rectangle(new(1, 1), 2, 1) { ZDir = direction, FaceColor = Colors.Red });
        if (z != 0) rectangle.Z = z;
        var proj = ProjectionOf(axes);
        Point Corner(double x, double y) => direction switch
        {
            ZDirection.X => proj.Project(z, x, y),
            ZDirection.Y => proj.Project(x, z, y),
            _ => proj.Project(x, y, z),
        };

        var points = PolygonIn(axes.Drawn(Area), Colors.Red).Points;

        Assert.Equal(4, points.Length);
        Corner(1, 1).AssertCloseTo(points[0], 1e-9);
        Corner(3, 1).AssertCloseTo(points[1], 1e-9);
        Corner(3, 2).AssertCloseTo(points[2], 1e-9);
        Corner(1, 2).AssertCloseTo(points[3], 1e-9);
    }

    /// <summary>A curve on a 3-D axes is drawn through points along it, each projected, so the scene the browser turns can move
    /// every one of them (<c>data-v3d</c>): an ellipse's path is many points on its curve, all on the ellipse.</summary>
    [Fact]
    public void A3DCircle_IsDrawnThroughPointsOnItsCurve_EachProjected()
    {
        var axes = ThreeD();
        axes.AddPatch(new Circle(new(2, 2), 1) { FaceColor = Colors.Red });
        var proj = ProjectionOf(axes);

        var points = PolygonIn(axes.Drawn(Area), Colors.Red).Points;

        Assert.True(points.Length >= 32, $"{points.Length} points");
        // Every point lies on the circle of radius 1 about (2, 2) in the plane z = 0: some angle projects to it.
        foreach (var point in points)
        {
            double best = Enumerable.Range(0, 3600).Select(i => i * Math.PI / 1800)
                .Select(a => proj.Project(2 + Math.Cos(a), 2 + Math.Sin(a), 0))
                .Min(p => Math.Abs(p.X - point.X) + Math.Abs(p.Y - point.Y));
            Assert.True(best < 0.5, $"a point stands {best} px off the circle");
        }
    }

    /// <summary>An open outline (an arc) is a line, not a filled shape.</summary>
    [Fact]
    public void A3DArc_IsDrawnAsALine()
    {
        var axes = ThreeD();
        axes.AddPatch(new Arc(new(2, 2), 2, 2) { Theta1 = 0, Theta2 = 90, EdgeColor = Colors.Red });

        var ctx = axes.Drawn(Area);

        Assert.DoesNotContain(ctx.Of<DrawPolygonCall>(), c => c.Paints(Colors.Red));
        Assert.Contains(ctx.Of<DrawLinesCall>(), c => c.Paints(Colors.Red) && c.Points.Length > 4);
    }

    /// <summary>An open outline with a face (a polygon that is not closed) is filled as a polygon with no edge, under its edge drawn
    /// as a line, as matplotlib fills the area a path encloses and strokes only the segments it has.</summary>
    [Fact]
    public void A3DOpenPolygonWithAFace_IsFilledUnderItsLine()
    {
        var axes = ThreeD();
        axes.AddPatch(new Polygon([new(1, 1), new(3, 1), new(2, 3)]) { Closed = false, FaceColor = Colors.Red, EdgeColor = Colors.Blue });

        var ctx = axes.Drawn(Area);

        var fill = PolygonIn(ctx, Colors.Red);
        Assert.Null(fill.Stroke);
        var line = Assert.Single(ctx.Of<DrawLinesCall>(), c => c.Paints(Colors.Blue));
        Assert.Equal(fill.Points, line.Points);
        Assert.True(ctx.Calls.IndexOf(fill) < ctx.Calls.IndexOf(line));
    }

    /// <summary>A patch whose outline has no point (a coordinate that is not a number) or whose plane cannot be projected (a
    /// <see cref="Patch.Z"/> that is not a number) is not drawn on a 3-D axes, as a patch that cannot be drawn is not on any.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A3DPatchThatCannotBePlaced_IsNotDrawn(bool badVertex)
    {
        var axes = ThreeD();
        axes.AddPatch(badVertex
            ? new Rectangle(new(double.NaN, 1), 2, 1) { FaceColor = Colors.Red }
            : new Rectangle(new(1, 1), 2, 1) { FaceColor = Colors.Red, Z = double.NaN });

        Assert.DoesNotContain(axes.Drawn(Area).Of<DrawPolygonCall>(), c => c.Paints(Colors.Red));
    }

    /// <summary>A polar patch that cannot be drawn (a coordinate that is not a number) draws nothing, and the axes' other marks stay.</summary>
    [Fact]
    public void APolarPatchThatCannotBeDrawn_DrawsNothing()
    {
        var axes = new Axes { CoordinateSystem = CoordinateSystem.Polar };
        axes.AddPatch(new Circle(new(double.NaN, 3), 0.5) { FaceColor = Colors.Red });

        Assert.DoesNotContain(axes.Drawn(Area).Of<DrawPathCall>(), c => c.Paints(Colors.Red));
    }

    /// <summary>In a scene the browser turns, a patch carries its points in data (<c>data-v3d</c>), so it turns with the scene.</summary>
    [Fact]
    public void A3DPatch_CarriesItsPoints_InASceneTheBrowserTurns()
    {
        var figure = Plt.Create().With3DRotation().AddSubPlot(1, 1, 1, ax => ax.Plot3D([0.0, 4.0], [0.0, 4.0], [0.0, 4.0])
            .AddPatch(new Rectangle(new(1, 1), 2, 1) { FaceColor = Colors.Red })).Build();
        figure.SubPlots[0].CoordinateSystem = CoordinateSystem.ThreeD;

        string svg = figure.ToSvg();
        int polygon = svg.IndexOf("fill=\"#FF0000\"", StringComparison.Ordinal);

        Assert.True(polygon > 0, "the patch was not drawn");
        int start = svg.LastIndexOf('<', polygon);
        Assert.Contains("data-v3d=", svg[start..svg.IndexOf('>', polygon)]);
    }

    /// <summary>matplotlib leaves the limits of a 3-D axes where its data puts them: a patch placed in 3-D asks for no range
    /// (printed: a line from 0 to 1 beside a rectangle on the plane x = 4 keeps xlim (-0.073, 1.073)).</summary>
    [Fact]
    public void A3DPatch_TakesNoPartInTheCube()
    {
        var axes = new Axes { CoordinateSystem = CoordinateSystem.ThreeD };
        axes.Plot3D([0.0, 1.0], [0.0, 1.0], [0.0, 1.0]);
        var before = axes.ComputeCubeRange(Theme.Default);
        axes.AddPatch(new Rectangle(new(2, 2), 3, 3) { Z = 4, ZDir = ZDirection.X });

        Assert.Equal(before, axes.ComputeCubeRange(Theme.Default));
    }

    [Fact]
    public void APatchsPlane_RestsAtZeroAcrossZ_AndTravelsThroughJson()
    {
        var resting = new Circle(new(0, 0), 1);
        var placed = new Circle(new(0, 0), 1) { Z = 2.5, ZDir = ZDirection.Y };
        var serializer = new ChartSerializer();
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.AddPatch(resting).AddPatch(placed)).Build();

        string json = serializer.ToJson(figure);
        var read = serializer.FromJson(json).SubPlots[0].Patches;

        Assert.Equal((0.0, ZDirection.Z), (resting.Z, resting.ZDir));
        Assert.Equal((2.5, ZDirection.Y), (read[1].Z, read[1].ZDir));
        Assert.Equal((0.0, ZDirection.Z), (read[0].Z, read[0].ZDir));
        Assert.Contains("\"zDir\": \"y\"", json.Replace("\":\"", "\": \""), StringComparison.Ordinal);
    }

    // ---- order, legend, interaction ---------------------------------------------------------------------------------

    /// <summary>A patch stands among the series of a polar or ternary axes by its z-order: under them when it is given none (a
    /// patch is matplotlib's 1, a line 2), over a series whose z-order is lower than the one it is given.</summary>
    [Theory]
    [InlineData(CoordinateSystem.Polar)]
    [InlineData(CoordinateSystem.Ternary)]
    public void APatch_StandsAmongTheSeriesByItsZOrder(CoordinateSystem system)
    {
        Axes Make(int? zOrder)
        {
            var axes = new Axes { CoordinateSystem = system };
            if (system == CoordinateSystem.Polar) axes.PolarPlot([1.0, 2.0, 3.0], [0.0, 1.0, 2.0]).Color = Colors.Green;
            else axes.TernaryScatter([0.2, 0.3], [0.3, 0.3], [0.5, 0.4]).Color = Colors.Green;
            var patch = axes.AddPatch(new Rectangle(new(0.1, 0.1), 0.2, 0.2) { FaceColor = Colors.Red });
            if (zOrder is { } z) patch.ZOrder = z;
            return axes;
        }

        static int PatchAt(RecordingRenderContext ctx) => ctx.Calls.FindIndex(c => c is DrawPathCall && c.Paints(Colors.Red));
        static int SeriesAt(RecordingRenderContext ctx) => ctx.Calls.FindIndex(c => c.Paints(Colors.Green));

        var under = Make(null).Drawn(Area);
        var over = Make(5).Drawn(Area);

        Assert.True(PatchAt(under) < SeriesAt(under), "a patch given no z-order is drawn over the series");
        Assert.True(PatchAt(over) > SeriesAt(over), "a patch given z-order 5 is drawn under the series");
    }

    /// <summary>A hidden patch is not drawn, on any axes.</summary>
    [Theory]
    [InlineData(CoordinateSystem.Polar)]
    [InlineData(CoordinateSystem.Ternary)]
    [InlineData(CoordinateSystem.ThreeD)]
    public void AHiddenPatch_IsNotDrawn(CoordinateSystem system)
    {
        var axes = system == CoordinateSystem.ThreeD ? ThreeD() : new Axes { CoordinateSystem = system };
        axes.AddPatch(new Rectangle(new(0.1, 0.1), 0.2, 0.2) { FaceColor = Colors.Red, Visible = false });

        var ctx = axes.Drawn(Area);

        Assert.DoesNotContain(ctx.Calls.OfType<ShapeCall>(), c => c is DrawPathCall or DrawPolygonCall && c.Fill == Colors.Red);
    }

    /// <summary>In an interactive picture the patch of a polar, ternary or 3-D axes is drawn in the group its legend row toggles.</summary>
    [Theory]
    [InlineData(CoordinateSystem.Polar)]
    [InlineData(CoordinateSystem.Ternary)]
    [InlineData(CoordinateSystem.ThreeD)]
    public void APatchWithALegendRow_IsDrawnInItsToggleGroup_InAnInteractivePicture(CoordinateSystem system)
    {
        var figure = Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax.AddPatch(new Rectangle(new(0.1, 0.1), 0.2, 0.2) { Label = "zone" })).Build();
        figure.SubPlots[0].CoordinateSystem = system;

        string svg = figure.ToSvg();

        Assert.Contains("<g class=\"patch\" data-series-index=\"0\" aria-label=\"zone\">", svg);
        Assert.Contains("data-legend-index=\"0\"", svg);
    }
}
