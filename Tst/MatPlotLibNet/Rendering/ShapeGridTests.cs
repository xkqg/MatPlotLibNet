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

    private static RecordingRenderContext DrawPolar(Axes axes, Theme theme)
    {
        var ctx = new RecordingRenderContext();
        new PolarAxesRenderer(axes, Plot, ctx, theme).Render();
        return ctx;
    }

    private static RecordingRenderContext Draw3D(Axes axes, Theme theme)
    {
        var ctx = new RecordingRenderContext();
        new ThreeDAxesRenderer(axes, Plot, ctx, theme).Render();
        return ctx;
    }

    private static Axes Changed(Axes axes, Func<GridStyle, GridStyle> change)
    {
        new AxesBuilder(axes).WithGrid(change);
        return axes;
    }

    /// <summary>The ring circles of a polar axes: their stroke colour and thickness.</summary>
    private static List<(Color Stroke, double Thickness)> Rings(RecordingRenderContext ctx) =>
        ctx.OfKind("DrawEllipse")
            .Select(c => ((Color)((dynamic)c.Args!).stroke, (double)((dynamic)c.Args!).strokeThickness))
            .ToList();

    /// <summary>The rings of a polar axes that are drawn as dashed polylines (a closed ring of 181 points), among the other
    /// polylines of the chart, which are the series'.</summary>
    private static List<CallRecord> DashedRings(RecordingRenderContext ctx) =>
        [.. ctx.OfKind("DrawLines").Where(c => ((Point[])((dynamic)c.Args!).points).Length == 181)];

    /// <summary>The spokes of a polar axes: their colour and thickness.</summary>
    private static List<(Color Colour, double Thickness)> Spokes(RecordingRenderContext ctx) =>
        ctx.OfKind("DrawLine")
            .Select(c => ((Color)((dynamic)c.Args!).color, (double)((dynamic)c.Args!).thickness))
            .ToList();

    private static int LinesOfColor(RecordingRenderContext ctx, Color color) =>
        ctx.OfKind("DrawLine").Concat(ctx.OfKind("DrawLines")).Count(c => (Color)((dynamic)c.Args!).color == color);

    // ── polar ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Measured before the change: under the default theme the rings and spokes were #CCCCCC and 0.5 px, under the
    /// dark theme too; the theme's grid is #B0B0B0 at 0.8 px, and #404040 at 0.5 px.</summary>
    [Theory]
    [InlineData("Default")]
    [InlineData("Dark")]
    [InlineData("MatplotlibV2")]
    public void APolarGrid_WithoutAChange_IsTheGridOfTheTheme(string name)
    {
        var theme = (Theme)typeof(Theme).GetProperty(name)!.GetValue(null)!;

        var ctx = DrawPolar(PolarAxes(), theme);

        Assert.NotEmpty(Rings(ctx));
        Assert.All(Rings(ctx), r => Assert.Equal((theme.DefaultGrid.Color, theme.DefaultGrid.LineWidth), r));
        Assert.Equal(12, Spokes(ctx).Count);
        Assert.All(Spokes(ctx), s => Assert.Equal((theme.DefaultGrid.Color, theme.DefaultGrid.LineWidth), s));
    }

    /// <summary>The spokes are lines and draw in the style of the grid, as the lines of a Cartesian grid do: dotted under the
    /// classic theme, dashed when the author asks for it, solid otherwise.</summary>
    [Fact]
    public void ThePolarSpokes_DrawInTheLineStyleOfTheGrid()
    {
        var classic = DrawPolar(PolarAxes(), Theme.MatplotlibClassic);
        var dashed = DrawPolar(Changed(PolarAxes(), g => g with { LineStyle = LineStyle.Dashed }), Theme.MatplotlibV2);
        var plain = DrawPolar(PolarAxes(), Theme.MatplotlibV2);

        Assert.Equal(12, classic.OfKind("DrawLine").Count(c => (LineStyle)((dynamic)c.Args!).style == LineStyle.Dotted));
        Assert.Equal(12, dashed.OfKind("DrawLine").Count(c => (LineStyle)((dynamic)c.Args!).style == LineStyle.Dashed));
        Assert.All(plain.OfKind("DrawLine"), c => Assert.Equal(LineStyle.Solid, (LineStyle)((dynamic)c.Args!).style));
    }

    /// <summary>The rings draw in the style of the grid too. A circle has no dash, so a ring that is not solid is a closed
    /// polyline of many short segments in the grid's style, drawn through the one line drawing that dashes; a solid ring stays
    /// the circle it always was (no picture under a solid grid changes).</summary>
    [Fact]
    public void ThePolarRings_DrawInTheLineStyleOfTheGrid_ASolidOneStaysACircle()
    {
        var classic = DrawPolar(PolarAxes(), Theme.MatplotlibClassic);
        var dashed = DrawPolar(Changed(PolarAxes(), g => g with { LineStyle = LineStyle.Dashed }), Theme.MatplotlibV2);
        var plain = DrawPolar(PolarAxes(), Theme.MatplotlibV2);

        Assert.Empty(classic.OfKind("DrawEllipse"));
        var rings = DashedRings(classic);
        Assert.NotEmpty(rings);
        Assert.All(rings, c =>
        {
            Assert.Equal(LineStyle.Dotted, (LineStyle)((dynamic)c.Args!).style);
            Assert.Equal(Color.FromHex("#000000"), (Color)((dynamic)c.Args!).color);
            Assert.Equal(0.5, (double)((dynamic)c.Args!).thickness);
            var points = (Point[])((dynamic)c.Args!).points;
            Assert.True(points.Length > 100);
            Assert.Equal(points[0], points[^1]);
        });
        Assert.All(DashedRings(dashed), c => Assert.Equal(LineStyle.Dashed, (LineStyle)((dynamic)c.Args!).style));
        Assert.NotEmpty(DashedRings(dashed));
        Assert.Empty(DashedRings(plain));
        Assert.NotEmpty(plain.OfKind("DrawEllipse"));
    }

    /// <summary>A dotted ring stands where the circle of its radius would: every point of the polyline is that far from the
    /// centre.</summary>
    [Fact]
    public void ADottedRing_StandsOnTheCircleItReplaces()
    {
        var ctx = DrawPolar(PolarAxes(), Theme.MatplotlibClassic);

        Assert.NotEmpty(DashedRings(ctx));
        foreach (var call in DashedRings(ctx))
        {
            var points = (Point[])((dynamic)call.Args!).points;
            double radius = Math.Sqrt(Math.Pow(points[0].X - 200, 2) + Math.Pow(points[0].Y - 200, 2));
            Assert.All(points, p => Assert.Equal(radius, Math.Sqrt(Math.Pow(p.X - 200, 2) + Math.Pow(p.Y - 200, 2)), 6));
        }
    }

    [Fact]
    public void APolarGrid_FollowsAChangeOverTheThemesGrid()
    {
        var ctx = DrawPolar(Changed(PolarAxes(), g => g with { Color = Colors.Red, LineWidth = 2 }), Theme.Dark);

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

        Assert.NotEmpty(Rings(DrawPolar(PolarAxes(), Theme.MatplotlibV2)));
    }

    [Fact]
    public void APolarGrid_SwitchedOff_DrawsNoRingsAndNoSpokes_ButKeepsItsLabels()
    {
        var ctx = DrawPolar(Changed(PolarAxes(), g => g with { Visible = false }), Theme.Default);

        Assert.Empty(Rings(ctx));
        Assert.Empty(Spokes(ctx));
        Assert.Contains(ctx.OfKind("DrawText"), c => ((string)((dynamic)c.Args!).text).EndsWith('°'));
        Assert.Contains(ctx.OfKind("DrawText"), c => (string)((dynamic)c.Args!).text == "10");
    }

    // ── 3-D ──────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A3DGrid_WithoutAChange_IsTheGridOfTheTheme()
    {
        var ctx = Draw3D(ThreeDAxes(), Theme.Dark);

        Assert.True(LinesOfColor(ctx, Theme.Dark.DefaultGrid.Color) > 0);
    }

    /// <summary>Measured before the change: a change over the grid did not reach the walls, which always drew the theme's.</summary>
    [Fact]
    public void A3DGrid_FollowsAChangeOverTheThemesGrid()
    {
        var plain = Draw3D(ThreeDAxes(), Theme.Dark);
        var changed = Draw3D(Changed(ThreeDAxes(), g => g with { Color = Colors.Red }), Theme.Dark);

        Assert.Equal(0, LinesOfColor(plain, Colors.Red));
        Assert.True(LinesOfColor(changed, Colors.Red) > 0);
        Assert.True(LinesOfColor(changed, Theme.Dark.DefaultGrid.Color) < LinesOfColor(plain, Theme.Dark.DefaultGrid.Color));
    }

    [Fact]
    public void A3DGrid_SwitchedOff_DrawsNoWallLines()
    {
        var gridColor = Theme.Dark.DefaultGrid.Color;
        int drawn = LinesOfColor(Draw3D(ThreeDAxes(), Theme.Dark), gridColor);
        int switchedOff = LinesOfColor(Draw3D(Changed(ThreeDAxes(), g => g with { Visible = false }), Theme.Dark), gridColor);

        Assert.True(switchedOff < drawn, $"{switchedOff} lines of the grid colour with the grid off, {drawn} with it on");
    }

    /// <summary>A 3-D chart draws its wall grid under a theme whose grid is off, as matplotlib's <c>axes3d.grid</c> does.</summary>
    [Fact]
    public void A3DGrid_UnderAThemeWithoutOne_IsStillDrawn()
    {
        var gridColor = Theme.MatplotlibV2.DefaultGrid.Color;

        Assert.True(LinesOfColor(Draw3D(ThreeDAxes(), Theme.MatplotlibV2), gridColor) > 0);
    }
}
