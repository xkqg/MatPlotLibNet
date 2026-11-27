// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The grid of a polar axes (its rings and spokes) and of a 3-D axes (the lines on its three back walls) is part of
/// the axes' shape, so it is drawn unless the author switched it off, and it is drawn in the style of the grid of the theme
/// the figure is drawn under with the author's change on top, the same grid <c>GridUnder</c> answers for a Cartesian axes.
/// Before, the polar rings were a fixed light grey half a pixel wide and the 3-D walls always took the theme's grid whole, so
/// <c>WithGrid</c> changed neither.</summary>
public class ShapeGridTests
{
    private static readonly Rect Plot = new(0, 0, 400, 400);

    private static Axes PolarAxes()
    {
        var axes = new Axes { CoordinateSystem = CoordinateSystem.Polar };
        axes.PolarPlot([2.0, 10, 6], [0.0, 1, 2]);
        return axes;
    }

    private static Axes ThreeDAxes()
    {
        var axes = new Axes { CoordinateSystem = CoordinateSystem.ThreeD };
        axes.Scatter3D([1.0, 2, 3], [1.0, 3, 2], [2.0, 1, 3]);
        return axes;
    }

    private static Axes Changed(Axes axes, Func<GridStyle, GridStyle> change)
    {
        new AxesBuilder(axes).WithGrid(change);
        return axes;
    }

    /// <summary>The ring circles of a polar axes: their stroke colour and thickness. A solid ring is the circle the spine is, so a
    /// ring is told from the spine by where it stands: an outline circle (a stroke, no fill) inside the edge, 200 px from the centre,
    /// where only the spine stands (the rings of this chart stand at 2.5, 5.0, 7.5 and 10.0 of an extent of 10.5).</summary>
    private static List<(Color Stroke, double Thickness)> Rings(RecordingRenderContext ctx) =>
        ctx.Of<DrawCircleCall>()
            .Where(a => a.Stroke is not null && a.Fill is null && a.Radius < 200 - 1e-9)
            .Select(a => (a.Stroke!.Value, a.StrokeThickness))
            .ToList();

    /// <summary>The rings of a polar axes that are drawn as dashed polylines (a closed ring of 181 points), among the other
    /// polylines of the chart, which are the series'.</summary>
    private static List<DrawLinesCall> DashedRings(RecordingRenderContext ctx) =>
        [.. ctx.Of<DrawLinesCall>().Where(c => c.Points.Length == 181)];

    /// <summary>The spokes of a polar axes: their colour and thickness.</summary>
    private static List<(Color Colour, double Thickness)> Spokes(RecordingRenderContext ctx) =>
        ctx.Of<DrawLineCall>()
            .Select(c => (c.Color, c.Thickness))
            .ToList();

    private static int LinesOfColor(RecordingRenderContext ctx, Color color) =>
        ctx.Of<StrokeCall>().Count(c => c.Color == color);

    // ── polar ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Measured before the change: under the default theme the rings and spokes were #CCCCCC and 0.5 px, under the
    /// dark theme too; the theme's grid is #B0B0B0 at 0.8 px, and #404040 at 0.5 px.</summary>
    [Theory]
    [InlineData("Default")]
    [InlineData("Dark")]
    [InlineData("MatplotlibV2")]
    public void APolarGrid_WithoutAChange_IsTheGridOfTheTheme(string name)
    {
        var theme = name.AsTheme();

        var ctx = PolarAxes().Drawn(Plot, theme);

        Assert.NotEmpty(Rings(ctx));
        Assert.All(Rings(ctx), r => Assert.Equal((theme.DefaultGrid.Color, theme.DefaultGrid.LineWidth), r));
        Assert.Equal(8, Spokes(ctx).Count);
        Assert.All(Spokes(ctx), s => Assert.Equal((theme.DefaultGrid.Color, theme.DefaultGrid.LineWidth), s));
    }

    /// <summary>The spokes are lines and draw in the style of the grid, as the lines of a Cartesian grid do: dotted under the
    /// classic theme, dashed when the author asks for it, solid otherwise.</summary>
    [Fact]
    public void ThePolarSpokes_DrawInTheLineStyleOfTheGrid()
    {
        var classic = PolarAxes().Drawn(Plot, Theme.MatplotlibClassic);
        var dashed = Changed(PolarAxes(), g => g with { LineStyle = LineStyle.Dashed }).Drawn(Plot, Theme.MatplotlibV2);
        var plain = PolarAxes().Drawn(Plot, Theme.MatplotlibV2);

        Assert.Equal(8, classic.Of<DrawLineCall>().Count(c => c.Style == LineStyle.Dotted));
        Assert.Equal(8, dashed.Of<DrawLineCall>().Count(c => c.Style == LineStyle.Dashed));
        Assert.All(plain.Of<DrawLineCall>(), c => Assert.Equal(LineStyle.Solid, c.Style));
    }

    /// <summary>The rings draw in the style of the grid too. A circle has no dash, so a ring that is not solid is a closed
    /// polyline of many short segments in the grid's style, drawn through the one line drawing that dashes; a solid ring stays
    /// a circle, drawn as the spine's circle is.</summary>
    [Fact]
    public void ThePolarRings_DrawInTheLineStyleOfTheGrid_ASolidOneStaysACircle()
    {
        var classic = PolarAxes().Drawn(Plot, Theme.MatplotlibClassic);
        var dashed = Changed(PolarAxes(), g => g with { LineStyle = LineStyle.Dashed }).Drawn(Plot, Theme.MatplotlibV2);
        var plain = PolarAxes().Drawn(Plot, Theme.MatplotlibV2);

        Assert.Empty(Rings(classic));
        Assert.Empty(classic.Of<DrawEllipseCall>());
        var rings = DashedRings(classic);
        Assert.NotEmpty(rings);
        Assert.All(rings, c =>
        {
            Assert.Equal(LineStyle.Dotted, c.Style);
            Assert.Equal(Color.FromHex("#000000"), c.Color);
            Assert.Equal(0.5.PointsInPixels, c.Thickness);
            var points = c.Points;
            Assert.True(points.Length > 100);
            Assert.Equal(points[0], points[^1]);
        });
        Assert.All(DashedRings(dashed), c => Assert.Equal(LineStyle.Dashed, c.Style));
        Assert.NotEmpty(DashedRings(dashed));
        Assert.Empty(DashedRings(plain));
        Assert.NotEmpty(Rings(plain));
        Assert.Empty(plain.Of<DrawEllipseCall>());
    }

    /// <summary>A dotted ring stands where the circle of its radius would: every point of the polyline is that far from the
    /// centre.</summary>
    [Fact]
    public void ADottedRing_StandsOnTheCircleItReplaces()
    {
        var ctx = PolarAxes().Drawn(Plot, Theme.MatplotlibClassic);

        Assert.NotEmpty(DashedRings(ctx));
        foreach (var call in DashedRings(ctx))
        {
            var points = call.Points;
            double radius = Math.Sqrt(Math.Pow(points[0].X - 200, 2) + Math.Pow(points[0].Y - 200, 2));
            Assert.All(points, p => Assert.Equal(radius, Math.Sqrt(Math.Pow(p.X - 200, 2) + Math.Pow(p.Y - 200, 2)), 6));
        }
    }

    /// <summary>A dotted or dashed ring is walked as matplotlib walks it: its path (the gridline over the polar transform, and the arc
    /// spine, <c>Path.arc(0, 360)</c>) starts at theta 0 and runs with theta, which is counter-clockwise on the screen, so the dots fall at
    /// the same places. matplotlib 3.11.2 (python, classic style, ring of 80 px, dotted [1, 3] points unscaled, 2 points wide): the dots stand
    /// 3.98 degrees apart (4 points of arc), counter-clockwise from the east, the first at 1.1 degrees. Walked clockwise the polyline put them
    /// where the ring of the matplotlib picture has gaps.</summary>
    [Fact]
    public void ADottedRing_RunsFromTheEastCounterClockwiseOnTheScreen()
    {
        var ctx = PolarAxes().Drawn(Plot, Theme.MatplotlibClassic);

        Assert.NotEmpty(DashedRings(ctx));
        foreach (var call in DashedRings(ctx))
        {
            var points = call.Points;
            double radius = points[0].X - 200;
            Assert.Equal(200, points[0].Y, 6);
            Assert.True(points[1].Y < 200, "the second point of the ring lies above the centre: theta grows upward on the screen");
            Assert.Equal(200 - radius, points[points.Length / 4].Y, 6);   // a quarter of the way round: north
            Assert.Equal(200, points[points.Length / 4].X, 6);
        }
    }

    [Fact]
    public void ADashedSpine_RunsFromTheEastCounterClockwiseToo()
    {
        var axes = PolarAxes();
        axes.Spines = axes.Spines with { Top = axes.Spines.Top with { LineStyle = LineStyle.Dashed } };

        var ctx = axes.Drawn(Plot, Theme.MatplotlibV2);

        var spine = ctx.Polylines().Single(p => p.Length == 181);
        Assert.Equal(200, spine[0].Y, 6);
        Assert.True(spine[1].Y < 200);
    }

    [Fact]
    public void APolarGrid_FollowsAChangeOverTheThemesGrid()
    {
        var ctx = Changed(PolarAxes(), g => g with { Color = Colors.Red, LineWidth = 2 }).Drawn(Plot, Theme.Dark);

        Assert.All(Rings(ctx), r => Assert.Equal((Colors.Red, 2.0), r));
        Assert.All(Spokes(ctx), s => Assert.Equal((Colors.Red, 2.0), s));
        Assert.NotEmpty(Rings(ctx));
    }

    /// <summary>A polar chart draws its grid under a theme whose grid is off (the default theme), because the rings are what
    /// the chart is read by; only an explicit "no grid" removes them.</summary>
    [Fact]
    public void APolarGrid_UnderAThemeWithoutOne_IsStillDrawn()
    {
        Assert.False(Theme.MatplotlibV2.DefaultGrid.Visible);

        Assert.NotEmpty(Rings(PolarAxes().Drawn(Plot, Theme.MatplotlibV2)));
    }

    [Fact]
    public void APolarGrid_SwitchedOff_DrawsNoRingsAndNoSpokes_ButKeepsItsLabels()
    {
        var ctx = Changed(PolarAxes(), g => g with { Visible = false }).Drawn(Plot, Theme.Default);

        Assert.Empty(Rings(ctx));
        Assert.Empty(Spokes(ctx));
        Assert.Contains(ctx.Of<DrawTextCall>(), c => c.Text.EndsWith('°'));
        Assert.Contains(ctx.Of<DrawTextCall>(), c => c.Text == "10");
    }

    // ── 3-D ──────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A3DGrid_WithoutAChange_IsTheGridOfTheTheme()
    {
        var ctx = ThreeDAxes().Drawn(Plot, Theme.Dark);

        Assert.True(LinesOfColor(ctx, Theme.Dark.DefaultGrid.Color) > 0);
    }

    /// <summary>Measured before the change: a change over the grid did not reach the walls, which always drew the theme's.</summary>
    [Fact]
    public void A3DGrid_FollowsAChangeOverTheThemesGrid()
    {
        var plain = ThreeDAxes().Drawn(Plot, Theme.Dark);
        var changed = Changed(ThreeDAxes(), g => g with { Color = Colors.Red }).Drawn(Plot, Theme.Dark);

        Assert.Equal(0, LinesOfColor(plain, Colors.Red));
        Assert.True(LinesOfColor(changed, Colors.Red) > 0);
        Assert.True(LinesOfColor(changed, Theme.Dark.DefaultGrid.Color) < LinesOfColor(plain, Theme.Dark.DefaultGrid.Color));
    }

    [Fact]
    public void A3DGrid_SwitchedOff_DrawsNoWallLines()
    {
        var gridColor = Theme.Dark.DefaultGrid.Color;
        int drawn = LinesOfColor(ThreeDAxes().Drawn(Plot, Theme.Dark), gridColor);
        int switchedOff = LinesOfColor(Changed(ThreeDAxes(), g => g with { Visible = false }).Drawn(Plot, Theme.Dark), gridColor);

        Assert.True(switchedOff < drawn, $"{switchedOff} lines of the grid colour with the grid off, {drawn} with it on");
    }

    /// <summary>A 3-D chart draws its wall grid under a theme whose grid is off, as matplotlib's <c>axes3d.grid</c> does.</summary>
    [Fact]
    public void A3DGrid_UnderAThemeWithoutOne_IsStillDrawn()
    {
        var gridColor = Theme.MatplotlibV2.DefaultGrid.Color;

        Assert.True(LinesOfColor(ThreeDAxes().Drawn(Plot, Theme.MatplotlibV2), gridColor) > 0);
    }

    /// <summary>matplotlib draws the walls of a 3-D box under its classic style in a grid of their own, not the style's grid
    /// (black, dotted, half a point). axis3d.py, <c>Axis.__init__</c>:
    /// <code>
    /// if mpl.rcParams['_internal.classic_mode']:
    ///     self._axinfo.update({
    ///         'axisline': {'linewidth': 0.75, 'color': (0, 0, 0, 1)},
    ///         'grid': {
    ///             'color': (0.9, 0.9, 0.9, 1),
    ///             'linewidth': 1.0,
    ///             'linestyle': '-',
    ///         },
    ///     })
    /// </code>
    /// Over the same limits the lines stand where the v2 theme draws its own, one for one (the two themes pad and round an
    /// unset range differently, so the limits are set).</summary>
    [Fact]
    public void A3DGrid_UnderTheClassicTheme_IsMatplotlibsClassicWallGrid()
    {
        static Axes Limited()
        {
            var axes = ThreeDAxes();
            foreach (var axis in new[] { axes.XAxis, axes.YAxis, axes.ZAxis }) { axis.Min = 0; axis.Max = 4; }
            return axes;
        }

        var lightGrey = Color.FromHex("#E6E6E6");
        var classic = Limited().Drawn(Plot, Theme.MatplotlibClassic);
        int v2Lines = LinesOfColor(Limited().Drawn(Plot, Theme.MatplotlibV2), Theme.MatplotlibV2.DefaultGrid.Color);

        var walls = classic.Of<DrawLineCall>().Where(c => c.Color == lightGrey).ToList();
        Assert.True(v2Lines > 0);
        Assert.Equal(v2Lines, walls.Count);
        Assert.All(walls, c => Assert.Equal(1.0.PointsInPixels, c.Thickness));
        Assert.All(walls, c => Assert.Equal(LineStyle.Solid, c.Style));
    }

    /// <summary>An author's change applies over the classic wall grid as it does over any theme's.</summary>
    [Fact]
    public void A3DGrid_UnderTheClassicTheme_FollowsAChange()
    {
        var changed = Changed(ThreeDAxes(), g => g with { Color = Colors.Red }).Drawn(Plot, Theme.MatplotlibClassic);

        Assert.True(LinesOfColor(changed, Colors.Red) > 0);
        Assert.Equal(0, LinesOfColor(changed, Color.FromHex("#E6E6E6")));
    }

    /// <summary>The classic wall grid is the theme's, so a figure under it comes back from JSON with it, and a theme derived
    /// from it keeps it.</summary>
    [Fact]
    public void TheClassicWallGrid_SurvivesJsonAndADerivedTheme()
    {
        var wall = Theme.MatplotlibClassic.Looks.Cube.WallGrid;

        Assert.NotNull(wall);
        Assert.Equal(wall, Theme.MatplotlibClassic.ThroughDto().Looks.Cube.WallGrid);
        Assert.Equal(wall, Theme.MatplotlibClassic.Cloned().Looks.Cube.WallGrid);
        Assert.Null(MatPlotLibNet.Serialization.ThemeDto.From(Theme.MatplotlibV2).WallGrid);
    }
}
